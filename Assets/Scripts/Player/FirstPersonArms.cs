using System.IO;
using BreakingPizza.Controls;
using BreakingPizza.Core;
using BreakingPizza.Interactables;
using BreakingPizza.Interaction;
using BreakingPizza.Minigames;
using UnityEngine;
using UnityEngine.Rendering;

namespace BreakingPizza.Player
{
    // Brazos del jugador en primera persona.
    public class FirstPersonArms : MonoBehaviour
    {
        [SerializeField] private bool showArms = true;
        [Tooltip("Destildado (default): los brazos están ocultos y solo aparece la mano derecha al interactuar (click / E / X), con una animación de \"tocar\". Tildado: los dos brazos se ven siempre.")]
        [SerializeField] private bool alwaysVisible = false;
        [Tooltip("Reloj en la muñeca izquierda. Destildalo para sacarlo.")]
        [SerializeField] private bool showWatch = false;

        [Header("Posición (relativa a la cámara)")]
        [SerializeField] private Vector3 rightArmPosition = new Vector3(0.21f, -0.265f, 0.10f);
        [SerializeField] private Vector3 rightArmRotation = new Vector3(-20f, -6f, -22f);
        [SerializeField] private Vector3 leftArmPosition = new Vector3(-0.21f, -0.265f, 0.10f);
        [SerializeField] private Vector3 leftArmRotation = new Vector3(-20f, 6f, 22f);
        [SerializeField] private float armScale = 1f;

        [Header("Animación de tocar (mano derecha)")]
        [SerializeField] private Vector3 touchPosition = new Vector3(0.15f, -0.25f, 0.14f);
        [SerializeField] private Vector3 touchRotation = new Vector3(-18f, -5f, -25f);
        [SerializeField] private Vector3 hiddenPosition = new Vector3(0.20f, -0.50f, 0.0f);
        [SerializeField] private Vector3 hiddenRotation = new Vector3(10f, -5f, -25f);
        [SerializeField] private float touchDuration = 0.55f;

        [Header("Sosteniendo la máscara (mano derecha)")]
        [SerializeField] private Vector3 holdPosition = new Vector3(0.15f, -0.28f, 0.12f);
        [Tooltip("Z = -150: la mano gira con la palma hacia arriba para sostener la máscara.")]
        [SerializeField] private Vector3 holdRotation = new Vector3(-20f, -12f, -150f);
        [Tooltip("Dónde queda la máscara respecto del brazo derecho (sobre la palma).")]
        [SerializeField] private Vector3 maskGripOffset = new Vector3(0f, -0.10f, 0.30f);

        [Header("Dedos (0 = abiertos, 1 = puño)")]
        [SerializeField, Range(0f, 1f)] private float holdCurl = 0.45f;
        [SerializeField, Range(0f, 1f)] private float touchCurl = 0.15f;

        [Header("Movimiento")]
        [SerializeField] private float walkBob = 0.012f;
        [SerializeField] private float lookSway = 0.0009f;
        [SerializeField] private float interactPunch = 0.07f;

        private Transform cam;
        private Transform root;
        private Transform rightArm, leftArm;
        private Renderer[] renderers;
        private Renderer[] rightRenderers;
        private Renderer[] leftRenderers;
        private float touchTime = -1f; // -1 = sin animación
        private MaskEquip mask;
        private float hold; // 0 = mano abajo, 1 = sosteniendo la máscara

        // Malla de la mano derecha deformable (para cerrar los dedos).
        private Mesh rightMesh;
        private Vector3[] basePositions, baseNormals, curlPositions, curlNormals;
        private float currentCurl = -1f;
        private float curl;
        private CharacterController body;
        private PlayerInteractor interactor;
        private bool built;
        private bool failed;

        private float bobPhase;
        private Vector3 swayOffset;
        private Quaternion lastCamRotation;
        private float punch;

        private void LateUpdate()
        {
            if (failed) return;
            if (!built)
            {
                TryBuild();
                if (!built) return;
            }

            bool allowed = showArms
                           && !MinigameHost.IsRunning
                           && Cursor.lockState == CursorLockMode.Locked
                           && (GameManager.Instance == null || GameManager.Instance.CurrentStage != GameStage.Result);

            if (!allowed)
            {
                touchTime = -1f;
                foreach (var r in renderers) r.enabled = false;
                return;
            }

            if (alwaysVisible)
            {
                foreach (var r in renderers) r.enabled = true;
                Animate();
                return;
            }

            if (mask == null) mask = FindAnyObjectByType<MaskEquip>();
            bool holding = mask != null && (mask.IsHeld || mask.IsBeingGrabbed);
            if (holding || hold > 0f)
            {
                AnimateHold(holding);
                return;
            }

            AnimateTouch();
        }

        // ── Construcción ──

        private void TryBuild()
        {
            var player = FirstPersonController.Instance;
            if (player == null) return;
            var camera = player.GetComponentInChildren<Camera>();
            if (camera == null) return;

            var data = Resources.Load<TextAsset>("WalterArms/arms");
            var texture = Resources.Load<Texture2D>("WalterArms/WalterBody");
            if (data == null)
            {
                Debug.LogWarning("[FirstPersonArms] No se encontró Resources/WalterArms/arms.bytes; no se muestran los brazos.");
                failed = true;
                return;
            }

            cam = camera.transform;
            body = player.GetComponent<CharacterController>();
            interactor = FindAnyObjectByType<PlayerInteractor>();
            lastCamRotation = cam.rotation;

            root = new GameObject("FirstPersonArms").transform;
            root.SetParent(cam, false);

            var suit = MakeMaterial(Color.white, texture, 0.25f);

            using (var reader = new BinaryReader(new MemoryStream(data.bytes)))
            {
                int magic = reader.ReadInt32();
                reader.ReadInt32(); // versión
                if (magic != 0x534D5241)
                {
                    Debug.LogWarning("[FirstPersonArms] arms.bytes con formato inesperado.");
                    failed = true;
                    return;
                }

                rightArm = BuildArm("RightArm", reader, suit, out _);
                leftArm = BuildArm("LeftArm", reader, suit, out var leftWrist);
                rightArm.SetParent(root, false);
                leftArm.SetParent(root, false);

                rightMesh = rightArm.GetComponent<MeshFilter>().sharedMesh;
                rightMesh.MarkDynamic();
                basePositions = rightMesh.vertices;
                baseNormals = rightMesh.normals;
                curlPositions = new Vector3[basePositions.Length];
                curlNormals = new Vector3[baseNormals.Length];
                if (showWatch) BuildWatch(leftArm, leftWrist);
            }

            renderers = root.GetComponentsInChildren<Renderer>(true);
            rightRenderers = rightArm.GetComponentsInChildren<Renderer>(true);
            leftRenderers = leftArm.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            built = true;
            Debug.Log($"[FirstPersonArms] Brazos creados en la cámara \"{cam.name}\".");
        }

        private static Transform BuildArm(string name, BinaryReader reader, Material material, out Vector4 wrist)
        {
            int vertexCount = reader.ReadInt32();
            int indexCount = reader.ReadInt32();
            float wx = reader.ReadSingle(), wy = reader.ReadSingle(), wz = reader.ReadSingle();
            reader.ReadSingle(); reader.ReadSingle(); // semiejes del puño (no se usan)
            reader.ReadSingle(); // largo
            wrist = new Vector4(wx, wy, wz, 0f);

            var positions = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                positions[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                uvs[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }
            var indices = new int[indexCount];
            for (int i = 0; i < indexCount; i++) indices[i] = reader.ReadInt32();

            var mesh = new Mesh { name = name };
            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = indices;
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go.transform;
        }

        private const float WatchZ = 0.245f;
        private const float WristCenterY = -0.008f;
        private const float WristHalfWidth = 0.045f;
        private const float WristHalfHeight = 0.031f;
        private const float FaceAngle = 31f; // 0 = lado interno, 90 = arriba

        private void BuildWatch(Transform arm, Vector4 wrist)
        {
            var watch = new GameObject("Watch").transform;
            watch.SetParent(arm, false);
            watch.localPosition = new Vector3(wrist.x, WristCenterY, WatchZ);

            // Malla: cilindro con el eje a lo largo del brazo, apenas más
            // grande que la muñeca para que la abrace sin hundirse.
            var band = Primitive(PrimitiveType.Cylinder, watch, MakeMaterial(new Color(0.13f, 0.09f, 0.06f), null, 0.35f));
            band.localRotation = Quaternion.Euler(90f, 0f, 0f);
            band.localScale = new Vector3(WristHalfWidth * 2f + 0.006f, 0.008f, WristHalfHeight * 2f + 0.006f);

            // Caja + esfera + agujas, apoyadas sobre la malla en el ángulo
            // que mira a la cámara.
            float rad = FaceAngle * Mathf.Deg2Rad;
            var dial = new GameObject("Dial").transform;
            dial.SetParent(watch, false);
            dial.localPosition = new Vector3(Mathf.Cos(rad) * (WristHalfWidth + 0.003f), Mathf.Sin(rad) * (WristHalfHeight + 0.003f), 0f);
            dial.localRotation = Quaternion.Euler(0f, 0f, FaceAngle - 90f);

            var caseT = Primitive(PrimitiveType.Cylinder, dial, MakeMaterial(new Color(0.78f, 0.78f, 0.8f), null, 0.85f));
            caseT.localPosition = new Vector3(0f, 0.003f, 0f);
            caseT.localScale = new Vector3(0.028f, 0.003f, 0.028f);

            var face = Primitive(PrimitiveType.Cylinder, dial, MakeMaterial(new Color(0.95f, 0.93f, 0.84f), null, 0.6f));
            face.localPosition = new Vector3(0f, 0.0062f, 0f);
            face.localScale = new Vector3(0.022f, 0.0006f, 0.022f);

            var handMat = MakeMaterial(new Color(0.08f, 0.08f, 0.08f), null, 0.4f);
            var hourHand = Primitive(PrimitiveType.Cube, dial, handMat);
            hourHand.localPosition = new Vector3(0.002f, 0.0072f, 0.0015f);
            hourHand.localRotation = Quaternion.Euler(0f, 35f, 0f);
            hourHand.localScale = new Vector3(0.0014f, 0.0005f, 0.006f);
            var minuteHand = Primitive(PrimitiveType.Cube, dial, handMat);
            minuteHand.localPosition = new Vector3(-0.002f, 0.0073f, 0.003f);
            minuteHand.localRotation = Quaternion.Euler(0f, -40f, 0f);
            minuteHand.localScale = new Vector3(0.001f, 0.0005f, 0.009f);
        }

        private static Transform Primitive(PrimitiveType type, Transform parent, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Material MakeMaterial(Color color, Texture2D texture, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");
            var m = new Material(shader);
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (texture != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
                m.mainTexture = texture;
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            // Doble cara: el modelo original lo es (y así no se ve "hueco"
            // el borde de la manga si asoma).
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            return m;
        }

        // ── Animación ──

        private void AnimateTouch()
        {
            foreach (var r in leftRenderers) r.enabled = false;

            // Un click nuevo reinicia el toque (si ya va bajando).
            if (GameInput.InteractPressed && (touchTime < 0f || touchTime > touchDuration * 0.5f))
            {
                touchTime = 0f;
            }

            if (touchTime < 0f)
            {
                foreach (var r in rightRenderers) r.enabled = false;
                return;
            }

            touchTime += Time.deltaTime;
            float p = touchTime / touchDuration;
            if (p >= 1f)
            {
                touchTime = -1f;
                foreach (var r in rightRenderers) r.enabled = false;
                return;
            }

            // Subida rápida (35 %), pausa (15 %), bajada (50 %).
            float a;
            if (p < 0.35f) a = 1f - Mathf.Pow(1f - p / 0.35f, 3f);
            else if (p < 0.5f) a = 1f;
            else a = 1f - Mathf.Pow((p - 0.5f) / 0.5f, 2f);

            // Un pequeño "empuje" hacia adelante en el momento del toque.
            float push = p > 0.3f && p < 0.55f ? Mathf.Sin((p - 0.3f) / 0.25f * Mathf.PI) * 0.025f : 0f;

            foreach (var r in rightRenderers) r.enabled = true;
            SetCurl(touchCurl * a);
            rightArm.localPosition = Vector3.Lerp(hiddenPosition, touchPosition, a) + new Vector3(0f, 0f, push);
            rightArm.localRotation = Quaternion.Slerp(Quaternion.Euler(hiddenRotation), Quaternion.Euler(touchRotation), a);
            rightArm.localScale = Vector3.one * armScale;
            lastCamRotation = cam.rotation;
        }

        private void AnimateHold(bool holding)
        {
            foreach (var r in leftRenderers) r.enabled = false;
            touchTime = -1f;

            hold = Mathf.MoveTowards(hold, holding ? 1f : 0f, Time.deltaTime * (holding ? 4f : 3f));
            float a = 1f - Mathf.Pow(1f - hold, 3f);

            bool show = hold > 0.001f;
            foreach (var r in rightRenderers) r.enabled = show;
            if (!show) return;

            float grab = holding ? Mathf.Clamp01((hold - 0.5f) / 0.5f) : hold;
            SetCurl(holdCurl * grab);
            rightArm.localPosition = Vector3.Lerp(hiddenPosition, holdPosition, a);
            rightArm.localRotation = Quaternion.Slerp(Quaternion.Euler(hiddenRotation), Quaternion.Euler(holdRotation), a);
            rightArm.localScale = Vector3.one * armScale;
            lastCamRotation = cam.rotation;

            // Solo cuando ya está en la mano (no mientras vuela hacia ella ni
            // hacia la cara, que eso lo anima MaskEquip).
            if (mask != null && mask.IsHeld)
            {
                Vector3 grip = rightArm.TransformPoint(maskGripOffset);
                var mt = mask.transform;
                mt.position = Vector3.Lerp(mt.position, grip, 1f - Mathf.Exp(-18f * Time.deltaTime));
            }
        }

        // ── Dedos ──
        private const float KnuckleZ = 0.315f;
        private const float PivotY = 0f;
        private static readonly float[] Phalanx = { 0.045f, 0.035f };
        private static readonly float[] JointMax = { 70f, 80f, 60f };
        private const float ThumbX = -0.046f;
        private static readonly Vector3 ThumbPivot = new Vector3(-0.038f, -0.02f, 0.27f);
        private const float ThumbMax = 55f;

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void SetCurl(float value)
        {
            if (rightMesh == null) return;
            curl = Mathf.Clamp01(value);
            if (Mathf.Abs(curl - currentCurl) < 0.002f) return;
            currentCurl = curl;

            for (int i = 0; i < basePositions.Length; i++)
            {
                Vector3 p = basePositions[i];
                Vector3 n = baseNormals[i];
                bool thumb = p.x < ThumbX && p.z < 0.35f && p.z > 0.24f;

                if (!thumb && p.z > KnuckleZ - 0.01f)
                {
                    float s = p.z - KnuckleZ;
                    float h = p.y - PivotY;
                    float k = Smooth((s + 0.01f) / 0.02f);
                    float py = PivotY, pz = KnuckleZ, ang = 0f, start = 0f;

                    for (int j = 0; j < 3; j++)
                    {
                        ang += JointMax[j] * curl * Mathf.Deg2Rad * k;
                        float end = j < 2 ? start + Phalanx[j] : float.MaxValue;
                        if (s < end || j == 2)
                        {
                            float local = s - start;
                            float c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                            p.y = py + h * c - local * sn;
                            p.z = pz + h * sn + local * c;
                            n = new Vector3(n.x, n.y * c - n.z * sn, n.y * sn + n.z * c);
                            break;
                        }
                        float len = end - start;
                        py -= len * Mathf.Sin(ang);
                        pz += len * Mathf.Cos(ang);
                        start = end;
                    }
                }
                else if (thumb)
                {
                    float w = Smooth((ThumbX - p.x) / 0.02f) * Smooth((p.z - 0.25f) / 0.03f);
                    float a = -ThumbMax * curl * Mathf.Deg2Rad * w;
                    float c = Mathf.Cos(a), sn = Mathf.Sin(a);
                    float dx = p.x - ThumbPivot.x, dy = p.y - ThumbPivot.y;
                    p.x = ThumbPivot.x + dx * c + dy * sn;
                    p.y = ThumbPivot.y - dx * sn + dy * c;
                    n = new Vector3(n.x * c + n.y * sn, -n.x * sn + n.y * c, n.z);
                }

                curlPositions[i] = p;
                curlNormals[i] = n;
            }

            rightMesh.vertices = curlPositions;
            rightMesh.normals = curlNormals;
            rightMesh.RecalculateBounds();
        }

        private void Animate()
        {
            float dt = Time.deltaTime;

            // Balanceo al caminar (según la velocidad real del jugador).
            float speed = 0f;
            if (body != null)
            {
                Vector3 v = body.velocity;
                speed = new Vector2(v.x, v.z).magnitude;
            }
            bobPhase += dt * Mathf.Lerp(1.6f, 9f, Mathf.Clamp01(speed / 4f));
            float walk = Mathf.Clamp01(speed / 3f);
            Vector3 bob = new Vector3(Mathf.Cos(bobPhase) * walkBob * 0.6f * walk,
                                      -Mathf.Abs(Mathf.Sin(bobPhase)) * walkBob * walk + Mathf.Sin(Time.time * 1.6f) * 0.0025f,
                                      0f);

            // Inercia al girar la cámara: los brazos se atrasan un poquito.
            Quaternion delta = Quaternion.Inverse(lastCamRotation) * cam.rotation;
            lastCamRotation = cam.rotation;
            Vector3 e = delta.eulerAngles;
            float yaw = e.y > 180f ? e.y - 360f : e.y;
            float pitch = e.x > 180f ? e.x - 360f : e.x;
            Vector3 targetSway = new Vector3(-yaw, pitch, 0f) * lookSway;
            targetSway = Vector3.ClampMagnitude(targetSway, 0.04f);
            swayOffset = Vector3.Lerp(swayOffset, targetSway, 10f * dt);

            // "Empujón" de la mano derecha al interactuar con algo.
            if (GameInput.InteractPressed && interactor != null && interactor.HasTarget) punch = 1f;
            punch = Mathf.MoveTowards(punch, 0f, dt * 5f);
            float punchCurve = Mathf.Sin(punch * Mathf.PI) * interactPunch;

            Vector3 shared = bob + swayOffset;
            rightArm.localPosition = rightArmPosition + shared + new Vector3(0f, punchCurve * 0.3f, punchCurve);
            rightArm.localRotation = Quaternion.Euler(rightArmRotation);
            leftArm.localPosition = leftArmPosition + new Vector3(-shared.x, shared.y, shared.z);
            leftArm.localRotation = Quaternion.Euler(leftArmRotation);
            rightArm.localScale = leftArm.localScale = Vector3.one * armScale;
        }
    }
}
