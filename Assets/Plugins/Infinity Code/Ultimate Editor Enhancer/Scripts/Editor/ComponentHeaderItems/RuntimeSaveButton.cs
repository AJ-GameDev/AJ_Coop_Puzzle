/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using InfinityCode.UltimateEditorEnhancer.Attributes;
using InfinityCode.UltimateEditorEnhancer.Interceptors;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer.ComponentHeader
{
    [InitializeOnLoad]
    public static class RuntimeSaveButton
    {
        private const string FIELD_SEPARATOR = "~«§";
        private static readonly Dictionary<string, object> savedFields = new();

        static RuntimeSaveButton()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            PropertyHandlerInterceptor.OnAddMenuItems += OnAddMenuItems;
        }

        [ComponentHeaderButton]
        public static bool Draw(Rect rectangle, Object[] targets)
        {
            var target = targets[0];
            if (!Validate(target)) return false;

            if (GUI.Button(rectangle, EditorIconContents.saveActive, Styles.iconButton))
            {
                var c = target as Component;
                if (c == null) return true;

                var so = new SerializedObject(target);
                var p = so.GetIterator();
                if (p.Next(true))
                    do
                    {
                        savedFields[c.GetInstanceID() + FIELD_SEPARATOR + p.propertyPath] =
                            SerializedPropertyHelper.GetBoxedValue(p.Copy());
                    } while (p.NextVisible(true));

                Debug.Log(
                    $"{c.gameObject.name}/{ObjectNames.NicifyVariableName(target.GetType().Name)} component state saved.");
            }

            return true;
        }

        private static void OnAddMenuItems(SerializedProperty property, GenericMenu menu)
        {
            if (!EditorApplication.isPlaying) return;

            var target = property.serializedObject.targetObject as Component;
            if (target == null) return;
            if (target.gameObject.scene.name == null) return;

            var instanceID = target.GetInstanceID();
            var path = property.propertyPath;

            var prop = property.Copy();

            menu.AddItem(TempContent.Get("Save Field Value"), false,
                () =>
                {
                    savedFields[instanceID + FIELD_SEPARATOR + path] = SerializedPropertyHelper.GetBoxedValue(prop);
                });
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                savedFields.Clear();
            else if (state == PlayModeStateChange.EnteredEditMode) RestoreSavedValues();
        }

        private static void RestoreSavedValues()
        {
            Undo.SetCurrentGroupName("Set saved state");
            var group = Undo.GetCurrentGroup();

            foreach (var pair in savedFields)
            {
                var parts = pair.Key.Split(new[] { FIELD_SEPARATOR }, StringSplitOptions.None);
                int id;
                if (!int.TryParse(parts[0], out id)) continue;

                var obj = EditorUtility.InstanceIDToObject(id);
                if (obj == null) continue;

                Undo.RecordObject(obj, "Set saved state");
                var so = new SerializedObject(obj);
                var prop = so.FindProperty(parts[1]);
                so.Update();
                SerializedPropertyHelper.SetBoxedValue(prop, pair.Value);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(obj);
            }

            Undo.CollapseUndoOperations(group);
        }

        private static bool Validate(Object target)
        {
            if (!Prefs.componentExtraHeaderButtons || !Prefs.saveComponentRuntime) return false;
            if (!EditorApplication.isPlaying) return false;
            var component = target as Component;
            if (component == null) return false;
            if (component.gameObject.scene.name == null) return false;
            return true;
        }
    }
}