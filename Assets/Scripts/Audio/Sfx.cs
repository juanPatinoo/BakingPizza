using System.Collections.Generic;
using UnityEngine;

namespace BreakingPizza.Audio
{
    // Efectos de sonido sintetizados por código.
    public enum SfxId
    {
        Tick,
        Go,
        Perfect,
        Good,
        Miss,
        Thud,
        Grate,
        Hurt,
        Place,
        Splash,
        Complete,
        Fanfare,
        Fail,
        Alarm,
        Whoosh,
        Count,
        Stamp,
        StepDirt0,
        StepDirt1,
        StepDirt2,
        StepIndoor0,
        StepIndoor1,
        Land
    }

    public enum LoopId
    {
        Pour,
        Sizzle,
        Wind
    }

    public class Sfx : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const int PoolSize = 8;

        [Range(0f, 1f)] public float masterVolume = 0.7f;

        private static Sfx instance;
        private static readonly Dictionary<SfxId, AudioClip> clips = new Dictionary<SfxId, AudioClip>();
        private static readonly Dictionary<LoopId, AudioClip> loopClips = new Dictionary<LoopId, AudioClip>();

        private AudioSource[] pool;
        private int poolIndex;
        private readonly Dictionary<LoopId, AudioSource> loops = new Dictionary<LoopId, AudioSource>();
        private System.Random rng = new System.Random(1234);

        private static Sfx Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[Sfx]");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<Sfx>();
                }
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            clips.Clear();
            loopClips.Clear();
        }

        private void Awake()
        {
            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
                pool[i].spatialBlend = 0f;
            }
        }

        public static void Play(SfxId id, float volume = 1f, float pitch = 1f)
        {
            var s = Instance;
            var src = s.pool[s.poolIndex];
            s.poolIndex = (s.poolIndex + 1) % PoolSize;
            src.pitch = pitch;
            src.PlayOneShot(s.GetClip(id), volume * s.masterVolume);
        }

        // Pisada: tierra con grava (desierto) o piso duro (interior).
        public static void Step(bool dirt, float volume = 1f)
        {
            int i = Random.Range(0, dirt ? 3 : 2);
            var id = dirt ? SfxId.StepDirt0 + i : SfxId.StepIndoor0 + i;
            Play(id, volume, 1f + Random.Range(-0.12f, 0.12f));
        }

        public static void PlayVaried(SfxId id, float volume = 1f, float pitchRange = 0.08f)
        {
            Play(id, volume, 1f + Random.Range(-pitchRange, pitchRange));
        }

        public static void SetLoop(LoopId id, bool on, float volume = 1f, float pitch = 1f)
        {
            var s = Instance;
            if (!s.loops.TryGetValue(id, out var src))
            {
                if (!on) return;
                src = s.gameObject.AddComponent<AudioSource>();
                src.loop = true;
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.clip = s.GetLoopClip(id);
                s.loops[id] = src;
            }

            src.volume = volume * s.masterVolume;
            src.pitch = pitch;
            if (on && !src.isPlaying) src.Play();
            if (!on && src.isPlaying) src.Stop();
        }

        public static void StopAllLoops()
        {
            if (instance == null) return;
            foreach (var src in instance.loops.Values)
            {
                if (src != null) src.Stop();
            }
        }

        // ── Síntesis ──

        private enum Wave { Sine, Square, Saw, Triangle }

        private AudioClip GetClip(SfxId id)
        {
            if (clips.TryGetValue(id, out var c) && c != null) return c;
            c = Build(id);
            clips[id] = c;
            return c;
        }

        private AudioClip GetLoopClip(LoopId id)
        {
            if (loopClips.TryGetValue(id, out var c) && c != null) return c;
            switch (id)
            {
                case LoopId.Pour: c = BuildPour(); break;
                case LoopId.Wind: c = BuildWind(); break;
                default: c = BuildSizzle(); break;
            }
            loopClips[id] = c;
            return c;
        }

        private AudioClip Build(SfxId id)
        {
            float[] b;
            switch (id)
            {
                case SfxId.Tick:
                    b = Buf(0.1f);
                    Tone(b, 0f, 0.09f, 880f, 880f, Wave.Square, 0.25f);
                    break;
                case SfxId.Go:
                    b = Buf(0.4f);
                    Tone(b, 0f, 0.35f, 1320f, 1320f, Wave.Square, 0.25f);
                    Tone(b, 0f, 0.35f, 660f, 660f, Wave.Sine, 0.3f);
                    break;
                case SfxId.Perfect:
                    b = Buf(0.45f);
                    Tone(b, 0f, 0.12f, 1046f, 1046f, Wave.Triangle, 0.45f);
                    Tone(b, 0.08f, 0.32f, 1568f, 1568f, Wave.Triangle, 0.45f);
                    Tone(b, 0.08f, 0.3f, 3136f, 3136f, Wave.Sine, 0.12f);
                    break;
                case SfxId.Good:
                    b = Buf(0.25f);
                    Tone(b, 0f, 0.2f, 784f, 784f, Wave.Triangle, 0.45f);
                    break;
                case SfxId.Miss:
                    b = Buf(0.35f);
                    Tone(b, 0f, 0.32f, 190f, 85f, Wave.Saw, 0.3f);
                    break;
                case SfxId.Thud:
                    b = Buf(0.22f);
                    Tone(b, 0f, 0.2f, 140f, 55f, Wave.Sine, 0.8f);
                    Noise(b, 0f, 0.12f, 0.35f, 0.08f, false);
                    break;
                case SfxId.Grate:
                    b = Buf(0.09f);
                    Noise(b, 0f, 0.08f, 0.4f, 0.5f, true);
                    break;
                case SfxId.Hurt:
                    b = Buf(0.4f);
                    Tone(b, 0f, 0.35f, 320f, 110f, Wave.Saw, 0.35f);
                    Noise(b, 0f, 0.2f, 0.3f, 0.3f, false);
                    break;
                case SfxId.Place:
                    b = Buf(0.12f);
                    Tone(b, 0f, 0.1f, 420f, 180f, Wave.Sine, 0.7f);
                    Noise(b, 0f, 0.04f, 0.2f, 0.2f, false);
                    break;
                case SfxId.Splash:
                    b = Buf(0.5f);
                    Noise(b, 0f, 0.48f, 0.55f, 0.15f, false);
                    Tone(b, 0f, 0.2f, 300f, 120f, Wave.Sine, 0.3f);
                    break;
                case SfxId.Complete:
                    b = Buf(0.45f);
                    Tone(b, 0f, 0.12f, 523f, 523f, Wave.Triangle, 0.4f);
                    Tone(b, 0.09f, 0.12f, 659f, 659f, Wave.Triangle, 0.4f);
                    Tone(b, 0.18f, 0.25f, 784f, 784f, Wave.Triangle, 0.45f);
                    break;
                case SfxId.Fanfare:
                    b = Buf(1.6f);
                    Tone(b, 0f, 0.15f, 523f, 523f, Wave.Square, 0.16f);
                    Tone(b, 0.15f, 0.15f, 659f, 659f, Wave.Square, 0.16f);
                    Tone(b, 0.3f, 0.15f, 784f, 784f, Wave.Square, 0.16f);
                    Tone(b, 0.45f, 1.1f, 1046f, 1046f, Wave.Square, 0.16f);
                    Tone(b, 0.45f, 1.1f, 784f, 784f, Wave.Triangle, 0.25f);
                    Tone(b, 0.45f, 1.1f, 523f, 523f, Wave.Triangle, 0.25f);
                    break;
                case SfxId.Fail:
                    b = Buf(1.4f);
                    Tone(b, 0f, 0.3f, 392f, 392f, Wave.Saw, 0.2f);
                    Tone(b, 0.3f, 0.3f, 370f, 370f, Wave.Saw, 0.2f);
                    Tone(b, 0.6f, 0.3f, 349f, 349f, Wave.Saw, 0.2f);
                    Tone(b, 0.9f, 0.5f, 330f, 300f, Wave.Saw, 0.2f);
                    break;
                case SfxId.Alarm:
                    b = Buf(0.5f);
                    Tone(b, 0f, 0.12f, 900f, 900f, Wave.Square, 0.18f);
                    Tone(b, 0.13f, 0.12f, 700f, 700f, Wave.Square, 0.18f);
                    Tone(b, 0.26f, 0.12f, 900f, 900f, Wave.Square, 0.18f);
                    break;
                case SfxId.Whoosh:
                    b = Buf(0.3f);
                    Noise(b, 0f, 0.28f, 0.3f, 0.05f, false, swell: true);
                    break;
                case SfxId.Count:
                    b = Buf(0.06f);
                    Tone(b, 0f, 0.05f, 1500f, 1500f, Wave.Sine, 0.25f);
                    break;
                case SfxId.Stamp:
                    b = Buf(0.35f);
                    Tone(b, 0f, 0.3f, 110f, 45f, Wave.Sine, 0.9f);
                    Noise(b, 0f, 0.1f, 0.5f, 0.1f, false);
                    break;
                case SfxId.StepDirt0:
                case SfxId.StepDirt1:
                case SfxId.StepDirt2:
                    // Pisada amortiguada sobre tierra: golpe grave y blando,
                    // y un crujido de grava suave que llega un instante después.
                    b = Buf(0.2f);
                    Noise(b, 0.004f, 0.11f, 0.30f, 0.07f, false);
                    Tone(b, 0f, 0.09f, 80f, 48f, Wave.Sine, 0.22f);
                    for (int k = 0; k < 5; k++)
                    {
                        Noise(b, 0.03f + (float)rng.NextDouble() * 0.1f, 0.01f + (float)rng.NextDouble() * 0.012f, 0.05f + (float)rng.NextDouble() * 0.07f, 0.45f, true);
                    }
                    break;
                case SfxId.StepIndoor0:
                case SfxId.StepIndoor1:
                    b = Buf(0.1f);
                    Tone(b, 0f, 0.08f, 150f, 65f, Wave.Sine, 0.3f);
                    Noise(b, 0f, 0.03f, 0.07f, 0.4f, true);
                    break;
                case SfxId.Land:
                    b = Buf(0.28f);
                    Noise(b, 0f, 0.2f, 0.4f, 0.08f, false);
                    Tone(b, 0f, 0.2f, 75f, 40f, Wave.Sine, 0.4f);
                    break;
                default:
                    b = Buf(0.05f);
                    break;
            }

            return MakeClip(id.ToString(), b);
        }

        private AudioClip BuildPour()
        {
            // Ruido grave filtrado con un "gorgoteo" que sube y baja.
            float[] b = Buf(1.5f);
            float y = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SampleRate;
                float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                y += 0.06f * (x - y);
                float glug = 0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 6f) * Mathf.Sin(t * 2f * Mathf.PI * 1.3333f);
                b[i] = y * 2.4f * glug;
            }
            return MakeClip("PourLoop", b);
        }

        private AudioClip BuildWind()
        {
            int loop = SampleRate * 12;
            int fade = SampleRate;
            var gen = new float[loop + fade];
            float a = 0f, b = 0f;

            for (int i = 0; i < gen.Length; i++)
            {
                float t = i / (float)SampleRate;
                float x = (float)(rng.NextDouble() * 2.0 - 1.0);

                float gust = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 12f + 1f);
                gust *= gust;
                float swell = 0.78f + 0.22f * Mathf.Sin(2f * Mathf.PI * t / 4f);

                // Con la ráfaga el filtro se abre: suena más "silbante".
                float k = Mathf.Lerp(0.012f, 0.05f, gust);
                a += k * (x - a);
                b += k * (a - b);

                float amp = (0.22f + 0.78f * gust) * swell;
                gen[i] = b * amp * 7f;
            }

            var buf = new float[loop];
            for (int i = 0; i < loop; i++)
            {
                buf[i] = i < fade ? Mathf.Lerp(gen[loop + i], gen[i], i / (float)fade) : gen[i];
            }
            return MakeClip("WindLoop", buf);
        }

        private AudioClip BuildSizzle()
        {
            // Siseo agudo + "chasquidos" aleatorios de grasa.
            float[] b = Buf(2f);
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += 0.3f * (x - lp);
                b[i] = (x - lp) * 0.18f;
            }
            for (int k = 0; k < 90; k++)
            {
                int start = rng.Next(0, b.Length - 2000);
                float amp = 0.2f + (float)rng.NextDouble() * 0.4f;
                for (int j = 0; j < 600; j++)
                {
                    float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                    b[start + j] += x * amp * Mathf.Exp(-j / 90f);
                }
            }
            return MakeClip("SizzleLoop", b);
        }

        private static float[] Buf(float seconds)
        {
            return new float[Mathf.CeilToInt(seconds * SampleRate)];
        }

        private static void Tone(float[] b, float start, float dur, float f0, float f1, Wave wave, float vol)
        {
            int s0 = (int)(start * SampleRate);
            int n = (int)(dur * SampleRate);
            double phase = 0.0;
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx >= b.Length) break;
                float t = i / (float)SampleRate;
                float u = (float)i / n;
                float f = f0 * Mathf.Pow(f1 / f0, u);
                phase += f / SampleRate;
                float p = (float)(phase - System.Math.Floor(phase));
                float v;
                switch (wave)
                {
                    case Wave.Square: v = p < 0.5f ? 0.6f : -0.6f; break;
                    case Wave.Saw: v = 2f * p - 1f; break;
                    case Wave.Triangle: v = 1f - 4f * Mathf.Abs(p - 0.5f); break;
                    default: v = Mathf.Sin(2f * Mathf.PI * p); break;
                }
                float env = Mathf.Min(1f, t / 0.005f) * (1f - u) * (1f - u);
                b[idx] += v * vol * env;
            }
        }

        private void Noise(float[] b, float start, float dur, float vol, float smoothing, bool highpass, bool swell = false)
        {
            int s0 = (int)(start * SampleRate);
            int n = (int)(dur * SampleRate);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx >= b.Length) break;
                float u = (float)i / n;
                float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += smoothing * (x - lp);
                float v = highpass ? x - lp : lp * (1f / Mathf.Sqrt(Mathf.Max(smoothing, 0.01f)));
                float env = swell ? Mathf.Sin(u * Mathf.PI) : (1f - u) * (1f - u);
                b[idx] += v * vol * env;
            }
        }

        private static AudioClip MakeClip(string name, float[] b)
        {
            for (int i = 0; i < b.Length; i++) b[i] = Mathf.Clamp(b[i], -1f, 1f);
            var clip = AudioClip.Create(name, b.Length, 1, SampleRate, false);
            clip.SetData(b, 0);
            return clip;
        }
    }
}
