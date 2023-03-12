/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.HierarchyTools
{
    [InitializeOnLoad]
    public static class Header
    {
        static Header()
        {
            HierarchyItemDrawer.Register("Header", OnHierarchyItem, -10000);
        }

        [MenuItem("GameObject/Create Header", priority = 1)]
        public static GameObject Create()
        {
            var go = new GameObject(Prefs.hierarchyHeaderPrefix + "Header");
            go.tag = "EditorOnly";
            var active = Selection.activeGameObject;
            if (active != null)
            {
                go.transform.SetParent(active.transform.parent);
                go.transform.SetSiblingIndex(active.transform.GetSiblingIndex());
            }

            Undo.RegisterCreatedObjectUndo(go, go.name);
            Selection.activeGameObject = go;
            return go;
        }

        private static void OnHierarchyItem(HierarchyItem item)
        {
            if (!Prefs.hierarchyHeaders) return;

            var go = item.gameObject;
            if (go == null) return;

            var rules = ReferenceManager.headerRules;
            HeaderRule rule = null;

            for (var i = 0; i < rules.Count; i++)
                if (rules[i].Validate(go))
                {
                    rule = rules[i];
                    break;
                }

            if (rule == null) return;

            if (Event.current.type == EventType.Repaint)
            {
                var textPadding = 8;

                var hasChildren = item.gameObject.transform.childCount > 0;
                if (hasChildren) textPadding = (int)item.rect.x - 30;

                rule.Draw(item, textPadding);

                if (hasChildren)
                {
                    var isExpanded = HierarchyHelper.IsExpanded(item.id);
                    var r = new Rect(item.rect);
                    r.width = 16;
                    r.x -= 14;
                    EditorStyles.foldout.Draw(r, GUIContent.none, -1, isExpanded);
                }
            }

            HierarchyItemDrawer.StopCurrentRowGUI();
        }
    }
}