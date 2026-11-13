using BreakingPizza.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BreakingPizza.EditorTools
{
    // Herramienta de editor: agrega colisiones a toda la escena.
    public static class AddCollidersToScene
    {
        private const float MinColliderSize = 0.3f;

        [MenuItem("Tools/Breaking Pizza/Agregar colisiones a TODA la escena")]
        private static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();

            Undo.SetCurrentGroupName("Agregar colisiones a toda la escena");
            int undoGroup = Undo.GetCurrentGroup();

            int added = 0, skippedExisting = 0, skippedSmall = 0, skippedPlayer = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    GameObject go = renderer.gameObject;

                    // El jugador y la máscara (que termina en la mano / cara)
                    // no llevan colisión extra.
                    if (go.GetComponentInParent<FirstPersonController>(true) != null ||
                        go.GetComponentInParent<BreakingPizza.Interactables.MaskEquip>(true) != null)
                    {
                        skippedPlayer++;
                        continue;
                    }

                    if (go.GetComponent<Collider>() != null)
                    {
                        skippedExisting++;
                        continue;
                    }

                    MeshFilter filter = go.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;

                    Vector3 size = renderer.bounds.size;
                    if (Mathf.Max(size.x, Mathf.Max(size.y, size.z)) < MinColliderSize)
                    {
                        skippedSmall++;
                        continue;
                    }

                    var col = Undo.AddComponent<MeshCollider>(go);
                    col.sharedMesh = filter.sharedMesh;
                    added++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            if (added > 0) EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"[AddCollidersToScene] Agregados: {added} · ya tenían collider: {skippedExisting} · chicos: {skippedSmall} · del jugador: {skippedPlayer}");
            EditorUtility.DisplayDialog(
                "Colisiones agregadas",
                $"Se agregaron {added} colisiones nuevas.\n\n" +
                $"Ya tenían: {skippedExisting}\nObjetos chicos salteados: {skippedSmall}\n\n" +
                "Guardá la escena con Ctrl+S para que queden.",
                "OK");
        }
    }
}
