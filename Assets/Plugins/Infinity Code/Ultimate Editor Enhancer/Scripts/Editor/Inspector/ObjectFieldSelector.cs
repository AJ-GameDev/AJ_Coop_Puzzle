/*           INFINITY CODE          */
/*     https://infinity-code.com    */


using System;
using System.Linq;
using InfinityCode.UltimateEditorEnhancer.Interceptors;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif


namespace InfinityCode.UltimateEditorEnhancer.InspectorTools
{
    [InitializeOnLoad]
    public static class ObjectFieldSelector
    {
        private static bool blockMouseUp;

        static ObjectFieldSelector()
        {
            ObjectFieldInterceptor.OnGUIBefore += OnGUIBefore;
        }

        private static void OnGUIBefore(
            Rect position,
            Rect dropRect,
            int id,
            Object obj,
            Object objBeingEdited,
            Type objType,
            Type additionalType,
            SerializedProperty property,
            object validator,
            bool allowSceneObjects,
            GUIStyle style)
        {
            if (!Prefs.objectFieldSelector) return;
            if (property == null) return;

            var e = Event.current;
            if (e.type == EventType.MouseUp && blockMouseUp)
            {
                blockMouseUp = false;
                e.Use();
                return;
            }

            if (e.type != EventType.MouseDown || e.button != 1) return;

            var rect = new Rect(position);
            rect.xMin = rect.xMax - 16;
            if (!rect.Contains(e.mousePosition)) return;

            var serializedObject = property.serializedObject;
            if (serializedObject == null) return;

            var targets = serializedObject.targetObjects;
            var target = targets[0];
            var type = target.GetType();
            var field = Reflection.GetField(type, property.propertyPath, true);
            if (field == null) return;

            var fieldType = field.FieldType;

            Object[] objects = null;
            GUIContent[] contents = null;

            if (fieldType.IsSubclassOf(typeof(Component)))
            {
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null)
                {
                    objects = prefabStage.prefabContentsRoot.GetComponentsInChildren(fieldType, true);
                }
                else
                {
#if UNITY_2020_3_OR_NEWER
                    objects = Object.FindObjectsOfType(fieldType, true);
#else
                    objects = Object.FindObjectsOfType(field.FieldType);
#endif
                }

                if (objects.Length == 1 && property.objectReferenceValue == null)
                {
                    Undo.RecordObjects(targets, "Modified Property");
                    property.objectReferenceValue = objects[0];
                    serializedObject.SetIsDifferentCacheDirty();
                }
                else
                {
                    objects = new Object[1].Concat(objects.OrderBy(o => o.name)).ToArray();
                    contents = new GUIContent[objects.Length];

                    contents[0] = new GUIContent("None");

                    for (var i = 1; i < objects.Length; i++)
                    {
                        var component = objects[i] as Component;
                        StaticStringBuilder.Clear();
                        StaticStringBuilder.Append(component.name)
                            .Append(" (")
                            .Append(component.GetType().Name)
                            .Append(")");

                        contents[i] = new GUIContent(StaticStringBuilder.GetString(true),
                            GameObjectUtils.GetGameObjectPath(component.gameObject).ToString());
                    }
                }
            }
            else if (fieldType.IsSubclassOf(typeof(ScriptableObject)))
            {
                objects = UnityEngine.Resources.FindObjectsOfTypeAll(fieldType);
                if (objects.Length == 1 && property.objectReferenceValue == null)
                {
                    Undo.RecordObjects(targets, "Modified Property");
                    property.objectReferenceValue = objects[0];
                    serializedObject.SetIsDifferentCacheDirty();
                }
                else
                {
                    objects = new Object[1].Concat(objects.OrderBy(o => o.name)).ToArray();
                    contents = new GUIContent[objects.Length];
                    contents[0] = new GUIContent("None");

                    for (var i = 1; i < objects.Length; i++)
                    {
                        var obj2 = objects[i];
                        var so = obj2 as ScriptableObject;
                        contents[i] = new GUIContent(so.name, AssetDatabase.GetAssetPath(so));
                    }
                }
            }
            else if (fieldType == typeof(GameObject))
            {
                objects = Object.FindObjectsOfType<GameObject>();
                contents = new GUIContent[objects.Length];
                for (var i = 0; i < objects.Length; i++) contents[i] = new GUIContent(objects[i].name);
            }
            else
            {
                Debug.Log(fieldType);
                objects = UnityEngine.Resources.FindObjectsOfTypeAll(fieldType);
                contents = new GUIContent[objects.Length];
                for (var i = 0; i < objects.Length; i++)
                    contents[i] = new GUIContent(objects[i].name, AssetDatabase.GetAssetPath(objects[i]));
            }

            blockMouseUp = true;
            e.Use();

            if (contents == null || contents.Length == 0) return;

            position.xMin += EditorGUIUtility.labelWidth;

            FlatSelectorWindow.Show(position, contents, -1).OnSelect += index =>
            {
                Undo.SetCurrentGroupName("Modified Property");
                var group = Undo.GetCurrentGroup();
                for (var i = 0; i < targets.Length; i++)
                {
                    Undo.RecordObject(targets[i], "Modified Property");
                    field.SetValue(targets[i], objects[index]);
                    EditorUtility.SetDirty(targets[i]);
                }

                Undo.CollapseUndoOperations(group);
                GUI.changed = true;
            };
        }
    }
}