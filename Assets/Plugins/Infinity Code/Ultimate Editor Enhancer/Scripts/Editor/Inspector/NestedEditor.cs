/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using System.Reflection;
using InfinityCode.UltimateEditorEnhancer.Interceptors;
using InfinityCode.UltimateEditorEnhancer.PropertyDrawers;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.InspectorTools
{
    [InitializeOnLoad]
    public static class NestedEditor
    {
        private static readonly int NestedEditorHash = "NestedEditor".GetHashCode();
        public static bool disallowNestedEditors;

        private static readonly Dictionary<int, bool> disallowCache;

        private static GUIContent content;
        private static Rect? contentArea;
        private static Object target;

        static NestedEditor()
        {
            disallowCache = new Dictionary<int, bool>();
            ObjectFieldDrawer.OnGUIBefore += OnGUIBefore;
            ObjectFieldDrawer.OnGUIAfter += OnGUIAfter;
        }

        private static void OnGUIBefore(Rect area, SerializedProperty property, GUIContent label)
        {
            contentArea = null;
            target = null;

            if (!Prefs.nestedEditors || disallowNestedEditors) return;

            var obj = property.objectReferenceValue;
            if (obj == null) return;

            if (!ReorderableListInterceptor.insideList)
            {
                bool disallow;
                var id = property.serializedObject.targetObject.GetInstanceID() ^ property.propertyPath.GetHashCode();

                if (!disallowCache.TryGetValue(id, out disallow))
                {
                    var type = property.serializedObject.targetObject.GetType();
                    var field = type.GetField(property.name, Reflection.InstanceLookup);
                    if (field != null)
                    {
                        disallowCache[id] = disallow = field.GetCustomAttribute<DisallowNestedEditor>() != null;
                        if (disallow) return;
                    }
                    else
                    {
                        disallowCache[id] = false;
                    }
                }
                else if (disallow)
                {
                    return;
                }
            }

            if (Prefs.nestedEditorsSide == NestedEditorSide.left)
            {
                area.xMin += EditorGUI.indentLevel * 15 - 16;
            }
            else
            {
                area.xMin = area.xMax - 36;
                area.y += 1;
            }

            area.width = 16;

            contentArea = area;
            target = obj;

            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && area.Contains(e.mousePosition))
            {
                if (target is Component) ComponentWindow.Show(target as Component, false).closeOnLossFocus = false;
                else if (target is GameObject) PropertyEditorRef.OpenPropertyEditor(target);
                else ObjectWindow.Show(new[] { target }, false).closeOnLossFocus = false;

                e.Use();
            }
        }

        private static void OnGUIAfter(Rect area, SerializedProperty property, GUIContent label)
        {
            if (!contentArea.HasValue) return;

            area = contentArea.Value;

            var color = GUI.color;

            var e = Event.current;
            var mousePosition = e.mousePosition;
            if (area.Contains(mousePosition)) GUI.color = Color.gray;

            if (content == null) content = new GUIContent(EditorIconContents.editIcon);

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append("Open ");
            StaticStringBuilder.Append(target.name);

            if (target is Component)
            {
                StaticStringBuilder.Append(" (");
                StaticStringBuilder.Append(ObjectNames.NicifyVariableName(target.GetType().Name));
                StaticStringBuilder.Append(")");
            }

            StaticStringBuilder.Append(" in window");

            content.tooltip = StaticStringBuilder.GetString(true);

            var controlId = GUIUtility.GetControlID(NestedEditorHash, FocusType.Passive, area);
            if (e.type == EventType.Repaint)
                GUIStyle.none.Draw(area, content, controlId, false, area.Contains(e.mousePosition));

            GUI.color = color;
        }
    }
}