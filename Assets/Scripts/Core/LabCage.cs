using UnityEngine;

namespace BreakingPizza.Core
{
    // Techo y paredes invisibles para no salir del laboratorio.
    public static class LabCage
    {
        private const string LabName = "kitchen_lab";
        private const string SpawnName = "InteriorSpawnPoint";
        private const float CeilingAboveFloor = 3f;
        private const float Thickness = 1f;
        private const int IgnoreRaycastLayer = 2;

        public static void Build()
        {
            if (GameObject.Find("[LabCage]") != null) return;

            var lab = GameObject.Find(LabName);
            if (lab == null)
            {
                Debug.LogWarning("[LabCage] No encontré el laboratorio (" + LabName + "); no se arma la jaula.");
                return;
            }

            var renderers = lab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);

            // Piso: el que está debajo del punto donde aparece el jugador.
            float floorY = b.min.y;
            var spawn = GameObject.Find(SpawnName);
            if (spawn != null && Physics.Raycast(spawn.transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 15f, ~0, QueryTriggerInteraction.Ignore))
            {
                floorY = hit.point.y;
            }

            float ceilingY = Mathf.Min(b.max.y, floorY + CeilingAboveFloor);
            float bottom = b.min.y - 3f;
            float height = ceilingY - bottom;

            var root = new GameObject("[LabCage]") { layer = IgnoreRaycastLayer };

            // Techo.
            AddBox(root, "Ceiling",
                new Vector3(b.center.x, ceilingY + Thickness / 2f, b.center.z),
                new Vector3(b.size.x + 2f * Thickness, Thickness, b.size.z + 2f * Thickness));

            // Paredes (apenas por fuera del modelo).
            float midY = bottom + height / 2f;
            AddBox(root, "WallXMin", new Vector3(b.min.x - Thickness / 2f, midY, b.center.z), new Vector3(Thickness, height, b.size.z + 2f * Thickness));
            AddBox(root, "WallXMax", new Vector3(b.max.x + Thickness / 2f, midY, b.center.z), new Vector3(Thickness, height, b.size.z + 2f * Thickness));
            AddBox(root, "WallZMin", new Vector3(b.center.x, midY, b.min.z - Thickness / 2f), new Vector3(b.size.x + 2f * Thickness, height, Thickness));
            AddBox(root, "WallZMax", new Vector3(b.center.x, midY, b.max.z + Thickness / 2f), new Vector3(b.size.x + 2f * Thickness, height, Thickness));
        }

        private static void AddBox(GameObject root, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name) { layer = IgnoreRaycastLayer };
            go.transform.SetParent(root.transform, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
        }
    }
}
