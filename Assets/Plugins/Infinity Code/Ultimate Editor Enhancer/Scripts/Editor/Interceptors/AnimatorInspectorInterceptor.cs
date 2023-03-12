/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Reflection;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer.Interceptors
{
    public class AnimatorInspectorInterceptor : StatedInterceptor<AnimatorInspectorInterceptor>
    {
        public static Action<Editor> OnInspectorGUI;

        protected override MethodInfo originalMethod => AnimatorInspectorRef.onInspectorGUIMethod;

        public override bool state => Prefs.animatorInspectorClips;

        protected override string postfixMethodName => nameof(OnInspectorGUIPostfix);

        private static void OnInspectorGUIPostfix(Editor __instance)
        {
            if (OnInspectorGUI != null) OnInspectorGUI(__instance);
        }
    }
}