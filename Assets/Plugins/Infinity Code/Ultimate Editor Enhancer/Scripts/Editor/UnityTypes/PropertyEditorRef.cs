/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer.UnityTypes
{
    public static class PropertyEditorRef
    {
        private static Type _type;

        private static MethodInfo _addMethod;
        private static MethodInfo _openPropertyEditorMethod;

        private static MethodInfo openPropertyEditorMethod
        {
            get
            {
                if (_openPropertyEditorMethod == null)
                    _openPropertyEditorMethod = type.GetMethod("OpenPropertyEditor", Reflection.StaticLookup, null,
                        new[] { typeof(Object), typeof(bool) }, null);
                return _openPropertyEditorMethod;
            }
        }

        public static Type type
        {
            get
            {
                if (_type == null) _type = Reflection.GetEditorType("PropertyEditor");
                return _type;
            }
        }

        public static EditorWindow OpenPropertyEditor(object obj, bool showWindow = true)
        {
            return openPropertyEditorMethod.Invoke(null, new[] { obj, showWindow }) as EditorWindow;
        }
    }
}