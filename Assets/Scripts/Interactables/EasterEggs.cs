using System.Collections;
using BreakingPizza.Audio;
using BreakingPizza.Interaction;
using BreakingPizza.UI.Kit;
using TMPro;
using UnityEngine;

namespace BreakingPizza.Interactables
{
    // Easter egg: balde de Los Pollos Hermanos.
    public class EasterEggs : MonoBehaviour
    {
        [SerializeField] private float bucketHeight = 0.22f;

        [Header("Ubicación manual (opcional, en coordenadas del mundo)")]
        [SerializeField] private Vector3 bucketManualPosition = Vector3.zero;

        private const string BucketPath = "EasterEggs/lospolloshermanos_bucket/pollos";

        private static readonly string[] PollosQuotes =
        {
            "Los Pollos Hermanos: donde algo delicioso siempre te espera.",
            "Gus Fring te está mirando...",
            "\"Yo oculto las cosas a plena vista.\"",
        };

        private IEnumerator Start()
        {
            // Un par de frames para que la física y la escena estén listas.
            yield return null;
            yield return new WaitForFixedUpdate();

            var mask = FindAnyObjectByType<MaskEquip>();
            if (mask == null)
            {
                Debug.LogWarning("[EasterEggs] No encontré la máscara; no se ubican los easter eggs.");
                yield break;
            }

            Vector3 maskPos = mask.transform.position;
            float floorY = FindFloorY(maskPos);

            var bucket = Spawn(BucketPath, "EasterEgg_Bucket", bucketHeight, false, out Quaternion bucketBase);
            if (bucket == null) yield break;

            // Sin colisión mientras se busca lugar (para no chocarse a sí mismo).
            SetColliders(bucket, false);
            PlaceAgainstWall(bucket, bucketBase, maskPos, floorY, bucketManualPosition, "Balde");
            AddInteraction(bucket, "Click para revisar el balde de Los Pollos Hermanos", PollosQuotes);
            SetColliders(bucket, true);
        }

        // ── Carga y escala ──

        private static GameObject Spawn(string path, string name, float targetSize, bool useLength, out Quaternion baseRotation)
        {
            baseRotation = Quaternion.identity;
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[EasterEggs] No se pudo cargar Resources/{path}.");
                return null;
            }

            var go = Instantiate(prefab);
            go.name = name;
            baseRotation = go.transform.rotation;
            go.transform.position = Vector3.zero;

            Bounds b = WorldBounds(go);
            float measure = useLength ? Mathf.Max(b.size.x, b.size.z) : b.size.y;
            if (measure > 0.0001f) go.transform.localScale *= targetSize / measure;

            b = WorldBounds(go);
            var box = go.AddComponent<BoxCollider>();
            Vector3 c = go.transform.InverseTransformPoint(b.center);
            Vector3 s = go.transform.InverseTransformVector(b.size);
            box.center = c;
            box.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            return go;
        }

        private static void SetColliders(GameObject go, bool on)
        {
            if (go == null) return;
            foreach (var c in go.GetComponents<Collider>()) c.enabled = on;
        }

        private static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        // Gira el objeto (yaw * rotación original) y lo apoya con la base en
        // "point.y", centrado en "point.xz".
        private static void SetOnFloor(GameObject go, Quaternion baseRotation, float yaw, Vector3 point)
        {
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * baseRotation;
            go.transform.position = Vector3.zero;
            Bounds b = WorldBounds(go);
            go.transform.position = new Vector3(point.x - b.center.x, point.y - b.min.y + 0.005f, point.z - b.center.z);
        }

        // Medidas del objeto "parado" sin giro: largo en X, profundidad en Z.
        private static Vector3 UprightSize(GameObject go, Quaternion baseRotation, out bool longIsX)
        {
            go.transform.rotation = baseRotation;
            go.transform.position = Vector3.zero;
            Vector3 size = WorldBounds(go).size;
            longIsX = size.x >= size.z;
            return longIsX ? size : new Vector3(size.z, size.y, size.x);
        }

        // ── Ubicación contra la pared ──

        // Busca, cerca de la máscara, un tramo de pared con piso adelante y
        // lugar libre, y apoya el objeto ahí. Devuelve la normal de la pared.
        private static Vector3 PlaceAgainstWall(GameObject bench, Quaternion baseRotation, Vector3 maskPos, float floorY, Vector3 manualPosition, string label)
        {
            Vector3 size = UprightSize(bench, baseRotation, out bool longIsX);
            float extraYaw = longIsX ? 0f : 90f;

            if (manualPosition != Vector3.zero)
            {
                SetOnFloor(bench, baseRotation, extraYaw, manualPosition);
                Debug.Log($"[EasterEggs] {label} en posición manual {manualPosition}.");
                return Vector3.forward;
            }

            Vector3 eye = new Vector3(maskPos.x, floorY + 1.2f, maskPos.z);
            float[] radii = { 1.0f, 1.5f, 2.0f, 2.5f, 3.0f };

            foreach (float r in radii)
            {
                for (int a = 0; a < 360; a += 20)
                {
                    Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                    Vector3 probe = new Vector3(maskPos.x, floorY, maskPos.z) + dir * r;

                    if (!HasFloor(probe, floorY)) continue;
                    // Que el punto se vea desde la máscara (misma habitación).
                    if (Physics.Linecast(eye, probe + Vector3.up * 1.2f, ~0, QueryTriggerInteraction.Ignore)) continue;

                    // Pared más cercana desde ese punto (rayos a la altura de la rodilla).
                    if (!FindNearestWall(probe + Vector3.up * 0.5f, 1.2f, out RaycastHit wall)) continue;

                    Vector3 n = wall.normal; n.y = 0f; n.Normalize();
                    // El banco va pegado a la pared, mirando hacia la habitación.
                    Vector3 pos = wall.point + n * (size.z / 2f + 0.03f);
                    pos.y = floorY;
                    if (!HasFloor(pos, floorY)) continue;

                    float yaw = Quaternion.LookRotation(n).eulerAngles.y;
                    Quaternion check = Quaternion.Euler(0f, yaw, 0f);
                    Vector3 half = new Vector3(size.x / 2f, size.y / 2f, size.z / 2f) * 0.92f;
                    if (Physics.CheckBox(pos + Vector3.up * (size.y / 2f + 0.06f), half, check, ~0, QueryTriggerInteraction.Ignore)) continue;

                    SetOnFloor(bench, baseRotation, yaw + extraYaw, pos);
                    Debug.Log($"[EasterEggs] {label} contra la pared en {bench.transform.position} (yaw {yaw:0}°).");
                    return n;
                }
            }

            SetOnFloor(bench, baseRotation, extraYaw, new Vector3(maskPos.x + 1f, floorY, maskPos.z));
            Debug.LogWarning($"[EasterEggs] No encontré pared libre para: {label}; quedó cerca de la máscara. ¿Corriste 'Agregar colisiones a TODA la escena'?");
            return Vector3.forward;
        }

        private static bool FindNearestWall(Vector3 from, float maxDistance, out RaycastHit best)
        {
            best = default;
            float bestDist = float.MaxValue;
            for (int a = 0; a < 360; a += 15)
            {
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                if (!Physics.Raycast(from, dir, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (Mathf.Abs(hit.normal.y) > 0.3f) continue; // solo superficies verticales
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    best = hit;
                }
            }
            return bestDist < float.MaxValue;
        }

        private static bool HasFloor(Vector3 p, float floorY)
        {
            if (!Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out RaycastHit hit, 1.6f, ~0, QueryTriggerInteraction.Ignore)) return false;
            return Mathf.Abs(hit.point.y - floorY) < 0.15f && hit.normal.y > 0.8f;
        }

        // El piso debajo de la máscara = el impacto MÁS BAJO del rayo.
        private static float FindFloorY(Vector3 from)
        {
            var hits = Physics.RaycastAll(from + Vector3.up * 0.2f, Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            foreach (var h in hits)
            {
                if (h.point.y < best) best = h.point.y;
            }
            return best < float.MaxValue ? best : from.y - 1f;
        }

        private static void AddInteraction(GameObject go, string prompt, string[] quotes)
        {
            var prop = go.AddComponent<EasterEggProp>();
            prop.Setup(prompt, quotes);
        }

        // ── Cartel con la frase ──

        private static Canvas toastCanvas;
        private static CanvasGroup toastGroup;
        private static TextMeshProUGUI toastText;
        private static Coroutine toastRoutine;
        private static EasterEggs instance;

        private void Awake()
        {
            instance = this;
        }

        public static void ShowQuote(string quote)
        {
            if (instance == null) return;
            if (toastCanvas == null)
            {
                toastCanvas = UIKit.CreateCanvas("EasterEggToast", 25, instance.transform);
                toastGroup = toastCanvas.gameObject.AddComponent<CanvasGroup>();
                toastGroup.blocksRaycasts = false;
                var panel = UIKit.Anchor(UIKit.Rect("Panel", toastCanvas.transform, new Vector2(1000f, 90f), new Vector2(0f, -150f)), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f));
                UIKit.Box("Border", panel, Palette.Hazmat, new Vector2(1006f, 96f), Vector2.zero);
                UIKit.Box("Fill", panel, new Color(0.02f, 0.05f, 0.035f, 0.94f), new Vector2(1000f, 90f), Vector2.zero);
                toastText = UIKit.Text("Quote", panel, string.Empty, 30f, Color.white, new Vector2(960f, 84f), Vector2.zero, TextAlignmentOptions.Center, true);
            }

            toastText.text = quote;
            Sfx.Play(SfxId.Complete, 0.6f);
            if (toastRoutine != null) instance.StopCoroutine(toastRoutine);
            toastRoutine = instance.StartCoroutine(ToastRoutine());
        }

        private static IEnumerator ToastRoutine()
        {
            var panel = toastText.transform.parent;
            yield return Tween.Run(0.25f, u =>
            {
                toastGroup.alpha = u;
                panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, Ease.OutBack(u));
            });
            yield return new WaitForSeconds(2.8f);
            yield return Tween.Fade(toastGroup, 1f, 0f, 0.4f);
        }
    }

    // Objeto de easter egg: muestra un cartel al mirarlo y una frase al
    // hacer click (va rotando entre varias).
    public class EasterEggProp : MonoBehaviour, IInteractable
    {
        private string prompt;
        private string[] quotes;
        private int index;

        public void Setup(string promptText, string[] quoteList)
        {
            prompt = promptText;
            quotes = quoteList;
        }

        public string GetPrompt() => prompt;

        public void Interact()
        {
            if (quotes == null || quotes.Length == 0) return;
            EasterEggs.ShowQuote(quotes[index % quotes.Length]);
            index++;
        }
    }
}
