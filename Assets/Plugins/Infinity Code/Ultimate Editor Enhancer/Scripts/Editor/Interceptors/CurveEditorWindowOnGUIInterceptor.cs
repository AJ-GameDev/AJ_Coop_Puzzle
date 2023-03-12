/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Reflection;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer.Interceptors
{
    public class CurveEditorWindowOnGUIInterceptor : StatedInterceptor<CurveEditorWindowOnGUIInterceptor>
    {
        public static Action<EditorWindow> OnGUIAfter;
        public static Action<EditorWindow> OnGUIBefore;

        protected override MethodInfo originalMethod => CurveEditorWindowRef.onGUIMethod;

        public override bool state => true;

        protected override string prefixMethodName => nameof(OnGUIPrefix);

        protected override string postfixMethodName => nameof(OnGUIPostfix);

        private static void OnGUIPrefix(EditorWindow __instance)
        {
            if (OnGUIBefore != null) OnGUIBefore(__instance);
        }

        private static void OnGUIPostfix(EditorWindow __instance)
        {
            if (OnGUIAfter != null) OnGUIAfter(__instance);
        }
    }
}