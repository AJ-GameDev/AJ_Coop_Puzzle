/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Interceptors
{
    public class HierarchyToolbarInterceptor : StatedInterceptor<HierarchyToolbarInterceptor>
    {
        private static GUIContent filterByType;

        protected override InitType initType => InitType.gui;

        protected override MethodInfo originalMethod => SearchableEditorWindowRef.searchFieldGUIMethod;

        public override bool state => Prefs.hierarchyTypeFilter;

        protected override string postfixMethodName => nameof(SearchFieldGUI);

        private static void SearchFieldGUI(EditorWindow __instance)
        {
            if (__instance.GetType() != SceneHierarchyWindowRef.type) return;

            var mode = SceneHierarchyWindowRef.GetSearchMode(__instance);
            if (filterByType == null)
            {
                filterByType = EditorGUIUtility.IconContent("FilterByType", "Search by Type");
                filterByType.tooltip = "Filter by Type";
            }

            if (mode != 1 && GUILayoutUtils.Button(filterByType, EditorStyles.toolbarButton) == ButtonEvent.click)
            {
                var components = Object.FindObjectsOfType<Component>();
                var types = new HashSet<string>();
                for (var i = 0; i < components.Length; i++)
                {
                    var type = components[i].GetType();
                    var name = type.Name;
                    if (!types.Contains(name)) types.Add(name);
                }

                var lastRect = GUILayoutUtils.lastRect;
                var contents = types.OrderBy(t => t).Select(t => new GUIContent(t)).ToArray();
                FlatSelectorWindow.Show(new Rect(new Vector2(lastRect.x, lastRect.yMax), Vector2.zero), contents, -1)
                    .OnSelect += i =>
                {
                    if (i < 0 || i >= contents.Length) return;

                    if (mode == 0)
                    {
                        var search = SceneHierarchyWindowRef.GetSearchFilter(__instance);
                        search = Regex.Replace(search, @"t:\w+", "").Trim();
                        if (search.Length > 0) search += " ";
                        search += "t:" + contents[i].text;
                        SceneHierarchyWindowRef.SetSearchFilter(__instance, search, mode);
                    }
                    else
                    {
                        SceneHierarchyWindowRef.SetSearchFilter(__instance, contents[i].text, mode);
                    }
                };
            }
        }
    }
}