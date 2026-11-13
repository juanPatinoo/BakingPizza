using UnityEditor;
using UnityEngine;

namespace BreakingPizza.EditorTools
{
    // Herramienta de editor: agrega colisiones a un modelo.
    public static class AddCollidersToModel
    {
        private const float MinColliderSize = 0.3f;

        [MenuItem("Tools/Breaking Pizza/Add Colliders To Selection")]
        private static void AddColliders()
        {
            GameObject target = Selection.activeGameObject;
            if (target == null)
            {
                EditorUtility.DisplayDialog(
                    "Nada seleccionado",
                    "Seleccioná en la Hierarchy la raíz del modelo (ej. \"kitchen_lab\") antes de correr este comando.",
                    "OK");
                return;
            }

            Undo.SetCurrentGroupName("Agregar Mesh Colliders");
            int undoGroup = Undo.GetCurrentGroup();

            MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>(true);

            int added = 0;
            int skippedExisting = 0;
            int skippedSmall = 0;
            int skippedNoMesh = 0;
            int unreadableMeshWarnings = 0;

            foreach (MeshRenderer renderer in renderers)
            {
                GameObject go = renderer.gameObject;

                if (go.GetComponent<MeshCollider>() != null)
                {
                    skippedExisting++;
                    continue;
                }

                MeshFilter filter = go.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    skippedNoMesh++;
                    continue;
                }

                // renderer.bounds ya está en espacio mundo, con la escala real
                // del import aplicada — no la escala local del mesh en sí.
                float largestDimension = Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.y, renderer.bounds.size.z);
                if (largestDimension < MinColliderSize)
                {
                    skippedSmall++;
                    continue;
                }

                if (!filter.sharedMesh.isReadable)
                {
                    Debug.LogWarning($"AddCollidersToModel: \"{go.name}\" tiene un mesh sin Read/Write Enabled — el collider se agregó igual, pero si falla en un build standalone, revisá el import settings del mesh.", go);
                    unreadableMeshWarnings++;
                }

                MeshCollider collider = Undo.AddComponent<MeshCollider>(go);
                collider.sharedMesh = filter.sharedMesh;
                added++;
            }

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"AddCollidersToModel: {added} Mesh Collider agregados sobre \"{target.name}\" " +
                $"({skippedExisting} ya tenían collider, {skippedSmall} salteados por chicos [<{MinColliderSize}m], " +
                $"{skippedNoMesh} sin mesh, {unreadableMeshWarnings} con mesh no legible).");
        }
    }
}
