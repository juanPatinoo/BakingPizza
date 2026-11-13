using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace BreakingPizza.Audio
{
    // Música por etapa, sintetizada por código, con fundido cruzado entre temas.
    public enum MusicTrack
    {
        Desert,
        Lab,
        Twist,
        Cooking,
        Credits
    }

    public class Music : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const string UserFolder = "Music";

        // Las canciones "de verdad" vienen masterizadas mucho más fuerte que
        // las sintetizadas: se bajan un poco para que no tapen los efectos.
        private const float UserClipGain = 0.55f;

        private static Music instance;
        private static readonly Dictionary<MusicTrack, AudioClip> clips = new Dictionary<MusicTrack, AudioClip>();
        private static readonly Dictionary<MusicTrack, string> credits = new Dictionary<MusicTrack, string>();
        private static readonly HashSet<MusicTrack> userTracks = new HashSet<MusicTrack>();
        private static AudioClip[] userClips;

        // Temas que se están sintetizando en segundo plano (ver Preload).
        private static readonly Dictionary<MusicTrack, Task<float[]>> pending = new Dictionary<MusicTrack, Task<float[]>>();

        private readonly AudioSource[] sources = new AudioSource[2];
        private readonly float[] targets = new float[2];
        private readonly float[] speeds = new float[2];
        private int active;
        private bool ducked;

        private static Music Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[Music]");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<Music>();
                }
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            clips.Clear();
            credits.Clear();
            userTracks.Clear();
            userClips = null;
            pending.Clear();
        }

        private void Awake()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.loop = true;
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.volume = 0f;
                sources[i] = s;
            }
        }

        private void Update()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                var s = sources[i];
                float goal = targets[i] * (ducked && i == active ? 0.7f : 1f);
                s.volume = Mathf.MoveTowards(s.volume, goal, speeds[i] * Time.unscaledDeltaTime);
                if (s.isPlaying && s.volume <= 0.0001f && targets[i] <= 0f) s.Stop();
            }
        }

        // ── API ──

        // Pasa al tema pedido con un fundido cruzado. Si ya está sonando,
        // solo ajusta el volumen.
        public static void Play(MusicTrack track, float volume, float fadeSeconds = 3f)
        {
            var m = Instance;
            AudioClip clip = GetClip(track);
            if (userTracks.Contains(track)) volume *= UserClipGain;
            fadeSeconds = Mathf.Max(0.1f, fadeSeconds);

            var current = m.sources[m.active];
            if (current.clip == clip && current.isPlaying)
            {
                m.targets[m.active] = volume;
                m.speeds[m.active] = Mathf.Max(0.01f, Mathf.Abs(volume - current.volume)) / fadeSeconds;
                return;
            }

            // El que sonaba se apaga despacio...
            m.targets[m.active] = 0f;
            m.speeds[m.active] = Mathf.Max(0.01f, current.volume) / fadeSeconds;

            // ...y entra el nuevo en la otra fuente.
            m.active = 1 - m.active;
            var next = m.sources[m.active];
            next.clip = clip;
            next.volume = 0f;
            next.time = 0f;
            next.Play();
            m.targets[m.active] = volume;
            m.speeds[m.active] = volume / fadeSeconds;
        }

        // Compatibilidad con el código anterior.
        public static void PlayLab(float volume = 0.12f, float fadeSeconds = 4f) => Play(MusicTrack.Lab, volume, fadeSeconds);

        public static void Stop(float fadeSeconds = 2f)
        {
            if (instance == null) return;
            for (int i = 0; i < instance.sources.Length; i++)
            {
                instance.targets[i] = 0f;
                instance.speeds[i] = Mathf.Max(0.01f, instance.sources[i].volume) / Mathf.Max(0.1f, fadeSeconds);
            }
        }

        // Baja un poco durante los minijuegos para que se escuchen sus efectos.
        public static void SetDucked(bool ducked)
        {
            if (instance != null) instance.ducked = ducked;
        }

        // Empieza a generar el tema EN SEGUNDO PLANO (otro hilo), para que
        // cuando haga falta ya esté listo y no haya un tirón en pantalla.
        public static void Preload(MusicTrack track)
        {
            if (clips.ContainsKey(track) || pending.ContainsKey(track)) return;

            var user = FindUserClip(track);
            if (user != null)
            {
                userTracks.Add(track);
                clips[track] = user;
                return;
            }
            pending[track] = Task.Run(() => Synthesize(track));
        }

        // Texto para los créditos: "Nombre del tema - Artista" si es una
        // canción propia, o null si es la música sintetizada.
        public static string SongCredit(MusicTrack track)
        {
            GetClip(track);
            return credits.TryGetValue(track, out string c) ? c : null;
        }

        public static bool IsUserTrack(MusicTrack track)
        {
            GetClip(track);
            return userTracks.Contains(track);
        }

        // ── Carga ──

        private static string Prefix(MusicTrack track)
        {
            switch (track)
            {
                case MusicTrack.Desert: return "desierto";
                case MusicTrack.Lab: return "laboratorio";
                case MusicTrack.Twist: return "giro";
                case MusicTrack.Cooking: return "cocina";
                default: return "creditos";
            }
        }

        private static AudioClip GetClip(MusicTrack track)
        {
            if (clips.TryGetValue(track, out var c) && c != null) return c;

            c = FindUserClip(track);
            if (c != null)
            {
                userTracks.Add(track);
            }
            else
            {
                // Si ya se estaba generando en segundo plano se espera ese
                // resultado; si no, se genera acá mismo.
                float[] data = pending.TryGetValue(track, out var task) ? task.Result : Synthesize(track);
                pending.Remove(track);
                c = AudioClip.Create("Music" + track, data.Length, 1, SampleRate, false);
                c.SetData(data, 0);
            }

            clips[track] = c;
            return c;
        }

        // Solo matemática (sin API de Unity), así puede correr en otro hilo.
        private static float[] Synthesize(MusicTrack track)
        {
            switch (track)
            {
                case MusicTrack.Desert: return BuildDesert();
                case MusicTrack.Lab: return BuildLab();
                case MusicTrack.Twist: return BuildTwist();
                case MusicTrack.Cooking: return BuildCooking();
                default: return BuildCredits();
            }
        }

        private static AudioClip FindUserClip(MusicTrack track)
        {
            if (userClips == null) userClips = Resources.LoadAll<AudioClip>(UserFolder);

            string prefix = Prefix(track);
            foreach (var clip in userClips)
            {
                if (clip == null) continue;
                string name = clip.name.Trim();
                string lower = name.ToLowerInvariant().Replace("é", "e").Replace("í", "i");
                if (!lower.StartsWith(prefix)) continue;

                // "Creditos - Baby Blue - Badfinger" -> "Baby Blue - Badfinger"
                int dash = name.IndexOf('-');
                if (dash >= 0 && dash + 1 < name.Length)
                {
                    string rest = name.Substring(dash + 1).Trim();
                    if (rest.Length > 0) credits[track] = rest;
                }
                return clip;
            }
            return null;
        }

        // ── Composición de cada tema ──

        private static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        // DESIERTO: Mi menor, 4 acordes de 12 s. Rasgueo lento al empezar
        // cada acorde y notas sueltas que "responden", como un western.
        private static float[] BuildDesert()
        {
            const float loop = 48f;
            var buf = new float[Mathf.RoundToInt(loop * SampleRate)];
            var rng = new System.Random(7);

            int[][] chords =
            {
                new[] { 40, 47, 52, 55, 59, 64 }, // Em
                new[] { 38, 45, 50, 54, 57, 62 }, // D
                new[] { 36, 43, 48, 52, 55, 60 }, // C
                new[] { 35, 42, 47, 51, 57, 63 }, // B7
            };
            const float chordSeconds = loop / 4f;

            for (int c = 0; c < chords.Length; c++)
            {
                float start = c * chordSeconds;
                int[] ch = chords[c];

                // Bordón grave sostenido.
                AddPad(buf, start, chordSeconds + 3f, Midi(ch[0]), 0.09f, c);
                AddPad(buf, start, chordSeconds + 3f, Midi(ch[1]), 0.05f, c + 1.7f);

                // Rasgueo lento (cuerda por cuerda).
                for (int s = 0; s < ch.Length; s++)
                {
                    AddPluck(buf, start + s * 0.045f, 4.5f, Midi(ch[s]), 0.16f - s * 0.012f, rng, 0.9975f);
                }

                // Respuestas: notas sueltas arriba, con aire entre medio.
                for (float t = 2.2f; t < chordSeconds - 0.8f; t += 1.1f)
                {
                    if (rng.NextDouble() < 0.4) continue;
                    int note = ch[2 + rng.Next(4)] + 12;
                    float when = start + t + (float)(rng.NextDouble() * 0.12);
                    AddPluck(buf, when, 3f, Midi(note), 0.12f, rng, 0.997f);
                    // A veces una segunda nota corta, como un "eco" de guitarra.
                    if (rng.NextDouble() < 0.35) AddPluck(buf, when + 0.32f, 2.5f, Midi(note - 2), 0.07f, rng, 0.997f);
                }
            }

            return Normalize(buf, 0.8f);
        }

        // LABORATORIO: el tema de suspenso original (Am - F - Dm - E).
        private static float[] BuildLab()
        {
            const float loop = 32f;
            var buf = new float[Mathf.RoundToInt(loop * SampleRate)];
            var rng = new System.Random(42);

            float[][] pads =
            {
                new[] { 110f, 164.81f, 220f, 261.63f },      // Am
                new[] { 87.31f, 130.81f, 174.61f, 220f },    // F
                new[] { 73.42f, 110f, 146.83f, 174.61f },    // Dm
                new[] { 82.41f, 123.47f, 164.81f, 207.65f }  // E (con sol#)
            };
            float[][] arps =
            {
                new[] { 220f, 261.63f, 329.63f, 440f },
                new[] { 174.61f, 220f, 261.63f, 349.23f },
                new[] { 146.83f, 174.61f, 220f, 293.66f },
                new[] { 164.81f, 207.65f, 246.94f, 329.63f }
            };

            float chordSeconds = loop / pads.Length;

            for (int c = 0; c < pads.Length; c++)
            {
                float start = c * chordSeconds;

                float padLen = chordSeconds + 2.5f;
                for (int n = 0; n < pads[c].Length; n++)
                {
                    float f = pads[c][n];
                    float vol = n == 0 ? 0.22f : 0.07f;
                    float detune = 1f + (n - 1.5f) * 0.0015f;
                    AddPad(buf, start, padLen, f * detune, vol, n * 1.3f);
                }

                int steps = Mathf.RoundToInt(chordSeconds / 0.5f);
                for (int s = 0; s < steps; s++)
                {
                    if (rng.NextDouble() < 0.28) continue;
                    int[] order = { 0, 2, 1, 3, 2, 1, 3, 2 };
                    float f = arps[c][order[s % order.Length]];
                    float t = start + s * 0.5f + (float)(rng.NextDouble() * 0.03);
                    AddPluck(buf, t, 2.6f, f, 0.2f + (float)rng.NextDouble() * 0.08f, rng);
                }
            }

            for (float t = 0f; t < loop; t += 2f)
            {
                AddThump(buf, t, 0.22f);
                AddThump(buf, t + 0.28f, 0.13f);
            }

            return Normalize(buf, 0.8f);
        }

        private static float[] BuildTwist()
        {
            const float bpm = 96f;
            float beat = 60f / bpm;
            const int bars = 8;
            float loop = bars * 4 * beat;
            var buf = new float[Mathf.RoundToInt(loop * SampleRate)];
            var rng = new System.Random(13);

            int[] roots = { 50, 51, 53, 54 }; // Re3, Mib3, Fa3, Fa#3 (sube cada 2 compases)

            for (int phrase = 0; phrase < 4; phrase++)
            {
                int root = roots[phrase];
                float start = phrase * 8 * beat;
                float grow = 0.75f + 0.12f * phrase;

                // Colchón medio (audible) + quinta.
                AddPad(buf, start, 8 * beat + 1.5f, Midi(root - 12), 0.05f, phrase);
                AddPad(buf, start, 8 * beat + 1.5f, Midi(root - 5), 0.05f * grow, phrase + 1.3f);

                // Trémolo agudo disonante: la fundamental y una segunda menor.
                AddTremolo(buf, start, 8 * beat + 0.8f, Midi(root + 24), 0.05f * grow, 7.5f);
                AddTremolo(buf, start, 8 * beat + 0.8f, Midi(root + 25), 0.035f * grow, 8.3f);

                // Golpe al empezar la frase.
                AddImpact(buf, start, 0.5f * grow, rng);

                // Ostinato de cuerdas: semicorcheas con acentos.
                for (int s16 = 0; s16 < 32; s16++)
                {
                    float tt = start + s16 * beat / 4f;
                    bool accent = s16 % 4 == 0;
                    int note = root + (s16 % 8 == 6 ? 12 : 0) + (s16 % 16 == 14 ? 1 : 0);
                    AddStab(buf, tt, beat / 4f * 0.9f, Midi(note), accent ? 0.16f : 0.09f);
                }
            }

            // Latido: cada 2 tiempos y en la segunda mitad en cada tiempo.
            for (int b = 0; b < bars * 4; b++)
            {
                bool secondHalf = b >= bars * 2;
                if (!secondHalf && b % 2 == 1) continue;
                float tt = b * beat;
                float k = b / (float)(bars * 4);
                AddKick(buf, tt, 0.42f + 0.18f * k);
                AddKick(buf, tt + 0.2f, 0.24f + 0.1f * k);
            }

            // Reloj en corcheas.
            for (int e8 = 0; e8 < bars * 8; e8++)
            {
                AddTick(buf, e8 * beat / 2f, e8 % 2 == 0 ? 2600f : 1900f, e8 % 2 == 0 ? 0.13f : 0.09f);
            }

            AddRiser(buf, loop - 8 * beat, 8 * beat, rng);

            Normalize(buf, 1f);
            Saturate(buf, 1.6f);
            return Normalize(buf, 0.85f);
        }

        // COCINA: 112 bpm en La menor (Am Am F E). Bajo en corcheas, bombo,
        // platillos y un pizzicato que le da el toque de comedia.
        private static float[] BuildCooking()
        {
            const float bpm = 112f;
            const int bars = 8;
            float beat = 60f / bpm;
            float loop = bars * 4 * beat;
            var buf = new float[Mathf.RoundToInt(loop * SampleRate)];
            var rng = new System.Random(99);

            int[][] chords =
            {
                new[] { 45, 57, 60, 64 }, // Am
                new[] { 45, 57, 60, 64 }, // Am
                new[] { 41, 57, 60, 65 }, // F
                new[] { 40, 56, 59, 64 }, // E
            };

            for (int bar = 0; bar < bars; bar++)
            {
                int[] ch = chords[(bar / 2) % chords.Length];
                float barStart = bar * 4 * beat;

                if (bar % 2 == 0)
                {
                    AddPad(buf, barStart, 8 * beat + 1.5f, Midi(ch[1]), 0.05f, bar);
                    AddPad(buf, barStart, 8 * beat + 1.5f, Midi(ch[2]), 0.035f, bar + 0.8f);
                }

                for (int e = 0; e < 8; e++)
                {
                    float t = barStart + e * beat / 2f;

                    // Bajo: fundamental con salto de octava.
                    int bass = ch[0] - 12 + (e % 4 == 3 ? 12 : 0);
                    AddPluck(buf, t, 0.5f, Midi(bass), 0.26f, rng, 0.994f);

                    // Platillo cerrado en cada corchea, más fuerte a contratiempo.
                    AddNoise(buf, t, 0.05f, e % 2 == 1 ? 0.07f : 0.035f, rng, true);

                    // Pizzicato arriba, saltando notas del acorde.
                    if (e % 2 == 1 || rng.NextDouble() < 0.25)
                    {
                        int note = ch[1 + (e + bar) % 3] + 12;
                        AddPluck(buf, t, 0.35f, Midi(note), 0.09f, rng, 0.992f);
                    }
                }

                // Bombo en 1 y 3, "palmas" en 2 y 4.
                AddThump(buf, barStart, 0.38f);
                AddThump(buf, barStart + 2 * beat, 0.32f);
                AddNoise(buf, barStart + beat, 0.16f, 0.10f, rng, false);
                AddNoise(buf, barStart + 3 * beat, 0.16f, 0.10f, rng, false);
            }

            // Los golpes del bombo dejaban todo bajito al normalizar: una
            // saturación suave los redondea y el tema suena más lleno.
            Normalize(buf, 1f);
            Saturate(buf, 2.2f);
            return Normalize(buf, 0.85f);
        }

        private static float[] BuildCredits()
        {
            const float bpm = 70f;
            float beat = 60f / bpm;
            const int bars = 16;
            float loop = bars * 4 * beat;
            var buf = new float[Mathf.RoundToInt(loop * SampleRate)];

            int[] cC = { 48, 55, 60, 64 };
            int[] cG = { 43, 50, 55, 59 };
            int[] cAm = { 45, 52, 57, 60 };
            int[] cF = { 41, 48, 53, 57 };
            int[] cEm = { 40, 47, 52, 55 };
            int[][] prog = { cC, cG, cAm, cF, cC, cG, cAm, cF, cF, cG, cEm, cAm, cF, cG, cC, cC };

            int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 1 };
            for (int bar = 0; bar < bars; bar++)
            {
                int[] ch = prog[bar];
                float barStart = bar * 4 * beat;

                // Cuerdas: una octava arriba, muy suaves.
                AddPad(buf, barStart, 4 * beat + 1.8f, Midi(ch[1] + 12), 0.035f, bar);
                AddPad(buf, barStart, 4 * beat + 1.8f, Midi(ch[3] + 12), 0.03f, bar + 1.1f);
                AddPad(buf, barStart, 4 * beat + 1.8f, Midi(ch[0] - 12), 0.06f, bar + 2.3f);

                for (int e = 0; e < 8; e++)
                {
                    float t = barStart + e * beat / 2f;
                    float vol = e == 0 ? 0.16f : 0.10f;
                    AddPiano(buf, t, 2.4f, Midi(ch[pattern[e]]), vol);
                }
            }

            // Melodía: (compás, tiempo, nota MIDI, duración en tiempos).
            float[,] melody =
            {
                { 0, 0, 76, 2 }, { 0, 2, 74, 1 }, { 0, 3, 72, 1 },
                { 1, 0, 74, 3 }, { 1, 3, 71, 1 },
                { 2, 0, 72, 2 }, { 2, 2, 76, 2 },
                { 3, 0, 69, 3 },
                { 4, 0, 76, 1 }, { 4, 1, 79, 1 }, { 4, 2, 76, 1 }, { 4, 3, 74, 1 },
                { 5, 0, 74, 2 }, { 5, 2, 67, 2 },
                { 6, 0, 69, 1 }, { 6, 1, 72, 1 }, { 6, 2, 76, 2 },
                { 7, 0, 77, 2 }, { 7, 2, 76, 1 }, { 7, 3, 74, 1 },
                { 8, 0, 72, 2 }, { 8, 2, 69, 2 },
                { 9, 0, 71, 2 }, { 9, 2, 74, 2 },
                { 10, 0, 76, 3 }, { 10, 3, 74, 1 },
                { 11, 0, 72, 4 },
                { 12, 0, 81, 2 }, { 12, 2, 79, 1 }, { 12, 3, 77, 1 },
                { 13, 0, 76, 2 }, { 13, 2, 74, 2 },
                { 14, 0, 72, 2 }, { 14, 2, 76, 1 }, { 14, 3, 79, 1 },
                { 15, 0, 79, 1 }, { 15, 1, 76, 1 }, { 15, 2, 72, 2 },
            };

            for (int i = 0; i < melody.GetLength(0); i++)
            {
                float t = (melody[i, 0] * 4 + melody[i, 1]) * beat;
                float f = Midi(Mathf.RoundToInt(melody[i, 2]));
                float dur = Mathf.Max(1.6f, melody[i, 3] * beat + 1.2f);
                AddPiano(buf, t, dur, f, 0.24f);
                // Octava abajo, apenas, para darle cuerpo.
                AddPiano(buf, t, dur, f * 0.5f, 0.06f);
            }

            return Normalize(buf, 0.85f);
        }

        // ── Instrumentos ──

        private static float[] Normalize(float[] buf, float peakTarget)
        {
            float peak = 0.0001f;
            for (int i = 0; i < buf.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
            float gain = peakTarget / peak;
            for (int i = 0; i < buf.Length; i++) buf[i] *= gain;
            return buf;
        }

        private static void AddPad(float[] buf, float start, float dur, float freq, float vol, float lfoPhase)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.RoundToInt(dur * SampleRate);
            double phase = 0.0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t / 2.2f) * Mathf.Min(1f, (dur - t) / 2.2f);
                float trem = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.18f * t + lfoPhase);
                phase += freq / SampleRate;
                float p = (float)(phase - System.Math.Floor(phase));
                float tri = 1f - 4f * Mathf.Abs(p - 0.5f);
                float sine = Mathf.Sin(2f * Mathf.PI * p);
                buf[(s0 + i) % len] += (sine * 0.7f + tri * 0.3f) * vol * env * trem;
            }
        }

        // Cuerda pulsada (Karplus-Strong): ruido en una línea de retardo con
        // filtro pasabajos que se va apagando solo.
        private static void AddPluck(float[] buf, float start, float dur, float freq, float vol, System.Random rng, float damping = 0.9965f)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.RoundToInt(dur * SampleRate);
            int period = Mathf.Max(2, Mathf.RoundToInt(SampleRate / freq));
            var line = new float[period];
            for (int i = 0; i < period; i++) line[i] = (float)(rng.NextDouble() * 2.0 - 1.0);

            int idx = 0;
            for (int i = 0; i < n; i++)
            {
                int next = (idx + 1) % period;
                float y = line[idx];
                line[idx] = 0.5f * (line[idx] + line[next]) * damping;
                idx = next;

                float fade = Mathf.Min(1f, (n - i) / (SampleRate * 0.3f));
                buf[(s0 + i) % len] += y * vol * fade;
            }
        }

        private static void AddPiano(float[] buf, float start, float dur, float freq, float vol)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.RoundToInt(dur * SampleRate);
            float[] amps = { 1f, 0.42f, 0.22f, 0.11f, 0.05f };

            for (int k = 0; k < amps.Length; k++)
            {
                float f = freq * (k + 1) * (1f + 0.0004f * k * k);
                if (f >= SampleRate * 0.45f) break;

                double w = 2.0 * System.Math.PI * f / SampleRate;
                double cw = System.Math.Cos(w), sw = System.Math.Sin(w);
                double c = 1.0, s = 0.0;
                double decay = System.Math.Exp(-(1.1 + 0.9 * k) / SampleRate);
                double env = 1.0;
                float a = amps[k] * vol;

                for (int i = 0; i < n; i++)
                {
                    double nc = c * cw - s * sw;
                    s = s * cw + c * sw;
                    c = nc;
                    env *= decay;

                    float attack = i < 120 ? i / 120f : 1f;
                    float release = Mathf.Min(1f, (n - i) / (SampleRate * 0.25f));
                    buf[(s0 + i) % len] += (float)(s * env) * a * attack * release;
                }
            }
        }

        // Saturación suave (tanh): sube el volumen percibido sin recortar feo.
        private static void Saturate(float[] buf, float drive)
        {
            float norm = (float)System.Math.Tanh(drive);
            for (int i = 0; i < buf.Length; i++) buf[i] = (float)System.Math.Tanh(buf[i] * drive) / norm;
        }

        // Cuerda en staccato: diente de sierra suave (6 armónicos) con
        // ataque rápido y caída corta.
        private static void AddStab(float[] buf, float start, float dur, float freq, float vol)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.RoundToInt(dur * SampleRate);
            for (int i = 0; i < n; i++)
            {
                float tt = i / (float)SampleRate;
                float u = i / (float)n;
                float env = Mathf.Min(1f, i / (SampleRate * 0.006f)) * (1f - u) * (1f - u);
                float v = 0f;
                for (int k = 1; k <= 6; k++)
                {
                    if (freq * k > SampleRate * 0.45f) break;
                    v += Mathf.Sin(2f * Mathf.PI * freq * k * tt) / k;
                }
                buf[(s0 + i) % len] += v * 0.6f * vol * env;
            }
        }

        // Nota aguda sostenida con trémolo (volumen que tiembla rápido).
        private static void AddTremolo(float[] buf, float start, float dur, float freq, float vol, float rate)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.RoundToInt(dur * SampleRate);
            double phase = 0.0;
            for (int i = 0; i < n; i++)
            {
                float tt = i / (float)SampleRate;
                float env = Mathf.Min(1f, tt / 1.2f) * Mathf.Min(1f, (dur - tt) / 0.6f);
                float trem = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * rate * tt);
                phase += freq * (1f + 0.003f * Mathf.Sin(2f * Mathf.PI * 5f * tt)) / SampleRate;
                float p = (float)(phase - System.Math.Floor(phase));
                float tri = 1f - 4f * Mathf.Abs(p - 0.5f);
                buf[(s0 + i) % len] += tri * vol * env * trem;
            }
        }

        // Golpe de suspenso: bombo medio + "clang" metálico (parciales
        // inarmónicos) + ruido, para que se note en parlantes chicos.
        private static void AddImpact(float[] buf, float start, float vol, System.Random rng)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.RoundToInt(1.8f * SampleRate);
            float[] partials = { 187f, 433f, 619f, 1042f, 1530f };
            double phase = 0.0;
            for (int i = 0; i < n; i++)
            {
                float tt = i / (float)SampleRate;
                float u = i / (float)n;
                float f = Mathf.Lerp(130f, 55f, Mathf.Sqrt(u));
                phase += f / SampleRate;
                float body = Mathf.Sin(2f * Mathf.PI * (float)phase) * Mathf.Exp(-u * 6f);
                float metal = 0f;
                for (int k = 0; k < partials.Length; k++)
                {
                    metal += Mathf.Sin(2f * Mathf.PI * partials[k] * tt) * Mathf.Exp(-tt * (2.5f + k));
                }
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-tt * 30f);
                float attack = Mathf.Min(1f, i / (SampleRate * 0.003f));
                buf[(s0 + i) % len] += (body * 0.9f + metal * 0.18f + noise * 0.35f) * vol * attack;
            }
        }

        // Latido / bombo con "click" de ataque (se oye aunque no haya graves).
        private static void AddKick(float[] buf, float start, float vol)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.RoundToInt(0.28f * SampleRate);
            double phase = 0.0;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                float f = Mathf.Lerp(150f, 55f, Mathf.Sqrt(u));
                phase += f / SampleRate;
                float env = Mathf.Min(1f, i / (SampleRate * 0.002f)) * (1f - u) * (1f - u);
                float click = i < SampleRate * 0.006f ? Mathf.Sin(2f * Mathf.PI * 1200f * i / SampleRate) * (1f - i / (SampleRate * 0.006f)) * 0.4f : 0f;
                buf[(s0 + i) % len] += (Mathf.Sin(2f * Mathf.PI * (float)phase) * env + click) * vol;
            }
        }

        // Tic de reloj: un "click" agudo cortito.
        private static void AddTick(float[] buf, float start, float freq, float vol)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.RoundToInt(0.035f * SampleRate);
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                float env = (1f - u) * (1f - u) * (1f - u);
                buf[(s0 + i) % len] += Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate) * vol * env;
            }
        }

        // Golpe grave de película: tono que cae + ruido sordo, con cola larga.
        private static void AddBoom(float[] buf, float start, float vol, System.Random rng)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.RoundToInt(2.2f * SampleRate);
            double phase = 0.0;
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                float f = Mathf.Lerp(70f, 32f, Mathf.Sqrt(u));
                phase += f / SampleRate;
                float env = Mathf.Min(1f, i / (SampleRate * 0.005f)) * Mathf.Exp(-u * 4f);
                low += ((float)(rng.NextDouble() * 2.0 - 1.0) - low) * 0.05f;
                float noiseEnv = Mathf.Exp(-u * 18f);
                buf[(s0 + i) % len] += (Mathf.Sin(2f * Mathf.PI * (float)phase) * env + low * 1.6f * noiseEnv) * vol;
            }
        }

        // Subida de tensión: ruido que se abre y un tono que sube.
        private static void AddRiser(float[] buf, float start, float dur, System.Random rng)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.RoundToInt(dur * SampleRate);
            double phase = 0.0;
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                float env = u * u;
                // Ruido filtrado que se va abriendo (más agudo al final).
                low += ((float)(rng.NextDouble() * 2.0 - 1.0) - low) * Mathf.Lerp(0.02f, 0.4f, u);
                float f = Mathf.Lerp(220f, 880f, u * u);
                phase += f / SampleRate;
                float tone = Mathf.Sin(2f * Mathf.PI * (float)phase) * 0.35f;
                // Se corta justo antes del loop, para que el golpe del
                // principio llegue "limpio".
                float cut = Mathf.Min(1f, (n - i) / (SampleRate * 0.04f));
                buf[(s0 + i) % len] += (low * 0.9f + tone) * env * 0.12f * cut;
            }
        }

        private static void AddThump(float[] buf, float start, float vol)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate) % len;
            int n = Mathf.RoundToInt(0.25f * SampleRate);
            double phase = 0.0;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                float f = Mathf.Lerp(62f, 38f, u);
                phase += f / SampleRate;
                float env = Mathf.Min(1f, i / (SampleRate * 0.004f)) * (1f - u) * (1f - u);
                buf[(s0 + i) % len] += Mathf.Sin(2f * Mathf.PI * (float)phase) * vol * env;
            }
        }

        // Golpe de ruido: platillo (bright = agudo, pasaaltos) o palmas.
        private static void AddNoise(float[] buf, float start, float dur, float vol, System.Random rng, bool bright)
        {
            int len = buf.Length;
            int s0 = Mathf.RoundToInt(start * SampleRate);
            int n = Mathf.RoundToInt(dur * SampleRate);
            float prev = 0f, low = 0f;
            for (int i = 0; i < n; i++)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                float y;
                if (bright)
                {
                    y = x - prev;
                    prev = x;
                }
                else
                {
                    low += (x - low) * 0.35f;
                    y = low;
                }
                float u = i / (float)n;
                float env = (1f - u) * (1f - u);
                buf[(s0 + i) % len] += y * vol * env;
            }
        }
    }
}
