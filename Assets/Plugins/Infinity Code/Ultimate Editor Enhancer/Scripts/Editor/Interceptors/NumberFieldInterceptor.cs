/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Reflection;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Interceptors
{
    public class NumberFieldInterceptor : StatedInterceptor<NumberFieldInterceptor>
    {
        private static string recycledText;

        protected override MethodInfo originalMethod => EditorGUIRef.doNumberFieldMethod;

        public override bool state => Prefs.changeNumberFieldValueByArrow;

        protected override string prefixMethodName => nameof(DoNumberFieldPrefix);

        protected override InitType initType => InitType.gui;

        private static void DoNumberFieldPrefix(
            object editor,
            Rect position,
            Rect dragHotZone,
            int id,
#if !UNITY_2021_2_OR_NEWER
            bool isDouble,
            ref double doubleVal,
            ref long longVal,
#else
            ref EditorGUIRef.NumberFieldValue value,
#endif
            string formatString,
            GUIStyle style,
            bool draggable,
            double dragSensitivity)
        {
            var e = Event.current;
            var v = 0;

            if (e.type == EventType.KeyDown && GUIUtility.keyboardControl == id)
            {
                if (e.keyCode == KeyCode.UpArrow)
                {
                    if (e.control || e.command) v = 100;
                    else if (e.shift) v = 10;
                    else v = 1;

                    e.Use();
                }
                else if (e.keyCode == KeyCode.DownArrow)
                {
                    if (e.control || e.command) v = -100;
                    else if (e.shift) v = -10;
                    else v = -1;
                    e.Use();
                }

                if (v != 0)
                {
#if !UNITY_2021_2_OR_NEWER
                    if (isDouble)
                    {
                        if (!double.IsInfinity(doubleVal) && !double.IsNaN(doubleVal))
                        {
                            doubleVal += v;
                            recycledText = doubleVal.ToString(Culture.numberFormat);
                            GUI.changed = true;
                        }
                    }
                    else
                    {
                        longVal += v;
                        recycledText = longVal.ToString();
                        GUI.changed = true;
                    }
#else
                    if (value.isDouble)
                    {
                        if (!double.IsInfinity(value.doubleVal) && !double.IsNaN(value.doubleVal))
                        {
                            value.doubleVal += v;
                            value.success = true;
                            recycledText = value.doubleVal.ToString(Culture.numberFormat);
                            GUI.changed = true;
                        }
                    }
                    else
                    {
                        value.longVal += v;
                        value.success = true;
                        recycledText = value.longVal.ToString();
                        GUI.changed = true;
                    }
#endif

                    var textEditor = editor as TextEditor;
                    if (textEditor != null)
                    {
                        textEditor.text = recycledText;
                        textEditor.SelectAll();
                    }
                }
            }
        }
    }
}