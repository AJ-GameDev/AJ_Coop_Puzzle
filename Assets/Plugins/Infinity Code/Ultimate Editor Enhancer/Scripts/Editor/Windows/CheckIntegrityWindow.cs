/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using InfinityCode.UltimateEditorEnhancer.Attributes;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Windows
{
    public class CheckIntegrityWindow : EditorWindow
    {
        private static bool hasProblems;
        private static string message;
        private Vector2 scrollPosition;
        private TypeRecord[] types;

        private void OnEnable()
        {
            var nspace = "InfinityCode.UltimateEditorEnhancer.UnityTypes";
            StaticStringBuilder.Clear();
            types = typeof(CheckIntegrityWindow).Assembly.GetTypes()
                .Where(t => t.IsClass && t.Namespace == nspace && t.IsAbstract && t.IsSealed &&
                            t.GetCustomAttribute<HideInIntegrityAttribute>() == null)
                .Select(t => new TypeRecord(t)).ToArray();
            var problems = StaticStringBuilder.GetString(true);
            if (string.IsNullOrEmpty(problems))
            {
                message = "No problems were found.";
                hasProblems = false;
            }
            else
            {
                message = "Found the following problems:\n" + problems;
                hasProblems = true;
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var normalStyle = EditorStyles.label;
            var missedStyle = new GUIStyle(normalStyle)
            {
                normal =
                {
                    textColor = Color.red
                },
                fontStyle = FontStyle.Bold
            };

            foreach (var type in types)
            {
                var typeName = type.name;
                var style = normalStyle;
                if (!type.exist)
                {
                    typeName = "[Missed] " + typeName;
                    style = missedStyle;
                }

                EditorGUILayout.LabelField(typeName, style);
                EditorGUI.indentLevel++;

                foreach (var prop in type.properties)
                {
                    var propName = prop.name;
                    style = normalStyle;
                    if (!prop.exist)
                    {
                        propName = "[Missed] " + propName;
                        style = missedStyle;
                    }

                    EditorGUILayout.LabelField(propName, style);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();

            if (!hasProblems)
            {
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(message, MessageType.Error);
                if (GUILayout.Button("Report To Infinity Code"))
                {
                    var subject = "Ultimate Editor Enhancer Integrity Check";
                    StaticStringBuilder.Clear();
                    StaticStringBuilder.Append("OS: ").Append(SystemInfo.operatingSystem).Append("\nUnity version: ")
                        .Append(Application.unityVersion).Append("\nUltimate Editor Enhancer version: ")
                        .Append(Version.version);
                    StaticStringBuilder.Append("\n").Append(message);
                    Process.Start("mailto:support@infinity-code.com?subject=" + Uri.EscapeUriString(subject) +
                                  "&body=" + Uri.EscapeUriString(StaticStringBuilder.GetString(true)));
                }
            }
        }

        [MenuItem(WindowsHelper.MenuPath + "Check Integrity", false, 123)]
        public static void OpenWindow()
        {
            GetWindow<CheckIntegrityWindow>("Check Integrity");
        }

        public class TypeRecord
        {
            public bool exist;
            public string name;
            public PropRecord[] properties;

            public TypeRecord(Type type)
            {
                name = type.Name;
                if (name.EndsWith("Ref")) name = name.Substring(0, name.Length - 3);

                var allProps = type.GetProperties(Reflection.StaticLookup);

                var typeProp = allProps.FirstOrDefault(p => p.Name == "type");
                if (typeProp != null) exist = typeProp.GetValue(null) != null;
                else StaticStringBuilder.Append("Missed Type: ").Append(name).Append("\n");

                properties = allProps
                    .Where(p => p.Name.EndsWith("Field") || p.Name.EndsWith("Prop") || p.Name.EndsWith("Method"))
                    .Select(p => new PropRecord(p, name)).ToArray();
            }
        }

        public class PropRecord
        {
            public bool exist;
            public string name;

            public PropRecord(PropertyInfo prop, string typeName)
            {
                name = prop.Name;

                try
                {
                    exist = prop.GetValue(null) != null;
                }
                catch
                {
                    exist = false;
                }


                if (name[name.Length - 4] == 'P') name = name.Substring(0, name.Length - 4) + " (Property)";
                else if (name[name.Length - 5] == 'F') name = name.Substring(0, name.Length - 5) + " (Field)";
                else if (name[name.Length - 6] == 'M')
                    name = char.ToUpperInvariant(name[0]) + name.Substring(1, name.Length - 7) + " (Method)";

                if (!exist)
                    StaticStringBuilder.Append("Missed ").Append(typeName).Append(" - ").Append(name).Append("\n");
            }
        }
    }
}