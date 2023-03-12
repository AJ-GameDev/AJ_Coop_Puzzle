/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Behaviors
{
    [InitializeOnLoad]
    public static class Ungroup
    {
        static Ungroup()
        {
            var binding = KeyManager.AddBinding();
            binding.OnValidate += () => Selection.gameObjects.Length > 0;
            binding.OnPress += OnInvoke;
        }

        private static void OnInvoke()
        {
            var e = Event.current;
            if (e.keyCode != Prefs.ungroupKeyCode || e.modifiers != Prefs.ungroupModifiers) return;

            if (EditorUtility.DisplayDialog("Ungroup", "Ungroup selected GameObjects?", "Yes", "Cancel"))
                UngroupSelection();
        }

        [MenuItem("Edit/Ungroup", false, 120)]
        [MenuItem("GameObject/Ungroup", false, 0)]
        public static void UngroupSelection()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0) return;
            UngroupTargets(Selection.gameObjects);
        }

        public static void UngroupTargets(params GameObject[] targets)
        {
            Undo.SetCurrentGroupName("Ungroup GameObjects");
            var group = Undo.GetCurrentGroup();

            var newSelection = new List<GameObject>();

            var selections = targets;

            for (var i = 0; i < selections.Length; i++)
            {
                var go = selections[i];
                var isPartOfPrefab = PrefabUtility.IsPartOfAnyPrefab(go);
                if (isPartOfPrefab)
                {
                    var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                    go = selections[i];
                }

                var t = go.transform;
                while (t.childCount != 0)
                {
                    var ct = t.GetChild(0);
                    Undo.SetTransformParent(ct, t.parent, "Update Parent");
                    newSelection.Add(ct.gameObject);
                }

                Undo.DestroyObjectImmediate(go);
            }

            Selection.objects = newSelection.ToArray();

            Undo.CollapseUndoOperations(group);
        }

        [MenuItem("GameObject/Ungroup", true)]
        [MenuItem("Edit/Ungroup", true)]
        public static bool ValidateUngroup()
        {
            return Selection.gameObjects.Length > 0;
        }
    }
}