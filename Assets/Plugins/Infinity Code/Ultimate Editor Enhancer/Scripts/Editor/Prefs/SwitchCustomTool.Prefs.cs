/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public static partial class Prefs
    {
        public static KeyCode switchCustomToolKeyCode = KeyCode.U;
        public static EventModifiers switchCustomToolModifiers = EventModifiers.None;

        private class SwitchCustomToolManager : StandalonePrefManager<SwitchCustomToolManager>, IHasShortcutPref
        {
            public override IEnumerable<string> keywords
            {
                get
                {
                    return new[]
                    {
                        "Select Next Custom Tool"
                    };
                }
            }

            public IEnumerable<Shortcut> GetShortcuts()
            {
                var shortcuts = new List<Shortcut>
                {
                    new("Select Next Custom Tool", "Everywhere", switchCustomToolModifiers, switchCustomToolKeyCode)
                };
                return shortcuts;
            }

            public override void Draw()
            {
                GUILayout.Label("Select Next Custom Tool");
                EditorGUI.indentLevel++;

                var oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = labelWidth + 5;
                switchCustomToolKeyCode =
                    (KeyCode)EditorGUILayout.EnumPopup("Hot Key", switchCustomToolKeyCode, GUILayout.Width(420));
                EditorGUIUtility.labelWidth = oldLabelWidth;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);
                GUILayout.Label("Modifiers", GUILayout.Width(modifierLabelWidth + 15));
                switchCustomToolModifiers = DrawModifiers(switchCustomToolModifiers, true);
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
        }
    }
}