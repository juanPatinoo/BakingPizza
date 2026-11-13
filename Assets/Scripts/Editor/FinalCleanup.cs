using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BreakingPizza.EditorTools
{
    // Herramienta de editor: limpia objetos y scripts viejos de la escena.
    public static class FinalCleanup
    {
        private const string ScenePath = "Assets/Tiny Teacup Studio/Low Poly Desert Environment/Demo/DemoScene.unity";

        private static readonly string[] ObjectsToDelete =
        {
            "Canvas/BakePanel",
            "Canvas/CirclesPanel",
            "Canvas/KitchenChecklist",
            "Canvas/KitchenProgressText",
            "Canvas/CircleProgressText",
            "Global Volume",
            "Orthographic Camera",
            "Perspective Camera 2",
            "Perspective Camera 3",
            "Perspective Camera 4",
        };

        private static readonly HashSet<string> ObsoleteTypes = new HashSet<string>
        {
            "KitchenChecklistUI", "KitchenProgressUI", "CircleClickMinigameUI", "CircleProgressUI",
            "CircleTimerVisual", "HitProgressPipsUI", "TargetZoneIndicatorUI", "TimingBarIndicatorUI", "TimingMinigame",
        };

        private static readonly string[] AssetsToTrash =
        {
            "Assets/Scripts/UI/KitchenChecklistUI.cs",
            "Assets/Scripts/UI/KitchenProgressUI.cs",
            "Assets/Scripts/UI/CircleClickMinigameUI.cs",
            "Assets/Scripts/UI/CircleProgressUI.cs",
            "Assets/Scripts/UI/CircleTimerVisual.cs",
            "Assets/Scripts/UI/HitProgressPipsUI.cs",
            "Assets/Scripts/UI/TargetZoneIndicatorUI.cs",
            "Assets/Scripts/UI/TimingBarIndicatorUI.cs",
            "Assets/Scripts/Minigames/TimingMinigame.cs",
            "Assets/Prefabs/Circle.prefab",
        };

        [MenuItem("Tools/Breaking Pizza/Limpieza final (una sola vez)")]
        private static void Run()
        {
            if (!EditorUtility.DisplayDialog("Limpieza final",
                    "Va a borrar de la escena del juego los objetos viejos (paneles de la UI anterior, Global Volume roto y cámaras apagadas), " +
                    "limpiar eventos y scripts faltantes, guardar la escena y mandar a la papelera los scripts viejos.\n\n¿Seguimos?",
                    "Limpiar", "Cancelar"))
            {
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var report = new StringBuilder();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1) Objetos viejos.
            foreach (string path in ObjectsToDelete)
            {
                var go = FindByPath(scene, path);
                if (go == null)
                {
                    report.AppendLine("  (no estaba) " + path);
                    continue;
                }
                Object.DestroyImmediate(go);
                report.AppendLine("  borrado: " + path);
            }

            // Componentes obsoletos que hubieran quedado en otros objetos.
            foreach (var mb in AllComponents<MonoBehaviour>(scene))
            {
                if (mb != null && ObsoleteTypes.Contains(mb.GetType().Name))
                {
                    report.AppendLine("  componente quitado: " + mb.GetType().Name + " en " + mb.gameObject.name);
                    Object.DestroyImmediate(mb);
                }
            }

            int deadCalls = 0;
            foreach (var mb in AllComponents<MonoBehaviour>(scene))
            {
                if (mb == null) continue;
                var so = new SerializedObject(mb);

                // Primero se juntan las listas de llamadas, después se tocan
                // (borrar mientras se recorre con el mismo iterador lo rompe).
                var callLists = new List<string>();
                var it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.isArray && it.propertyPath.EndsWith("m_PersistentCalls.m_Calls")) callLists.Add(it.propertyPath);
                }

                bool changed = false;
                foreach (string path in callLists)
                {
                    var calls = so.FindProperty(path);
                    for (int i = calls.arraySize - 1; i >= 0; i--)
                    {
                        var target = calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Target");
                        if (target != null && target.objectReferenceValue == null)
                        {
                            calls.DeleteArrayElementAtIndex(i);
                            deadCalls++;
                            changed = true;
                        }
                    }
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
            report.AppendLine("  llamadas de eventos muertas quitadas: " + deadCalls);

            // 3) Scripts faltantes.
            int missing = 0;
            foreach (var t in AllComponents<Transform>(scene))
            {
                missing += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            }
            report.AppendLine("  componentes con script faltante quitados: " + missing);

            // 4) Guardar.
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine("  escena guardada.");

            // 5) Archivos viejos a la papelera.
            foreach (string asset in AssetsToTrash)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(asset) == null) continue;
                bool ok = AssetDatabase.MoveAssetToTrash(asset);
                report.AppendLine((ok ? "  a la papelera: " : "  NO se pudo mover: ") + asset);
            }
            AssetDatabase.Refresh();

            Debug.Log("[Limpieza final]\n" + report);
            EditorUtility.DisplayDialog("Limpieza final", "Listo. El detalle quedó en la Consola.", "OK");
        }

        private static GameObject FindByPath(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                if (parts.Length == 1) return root;
                var child = root.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
                if (child != null) return child.gameObject;
            }
            return null;
        }

        private static List<T> AllComponents<T>(Scene scene) where T : Component
        {
            var list = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
            {
                list.AddRange(root.GetComponentsInChildren<T>(true));
            }
            return list;
        }
    }
}
