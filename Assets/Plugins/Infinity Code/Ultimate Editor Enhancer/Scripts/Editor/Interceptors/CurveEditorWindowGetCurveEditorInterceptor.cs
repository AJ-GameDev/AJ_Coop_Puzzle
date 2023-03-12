/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Reflection;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Interceptors
{
    public class
        CurveEditorWindowGetCurveEditorInterceptor : StatedInterceptor<CurveEditorWindowGetCurveEditorInterceptor>
    {
        public static Func<EditorWindow, Rect> OnGetCurveEditorRect;

        protected override MethodInfo originalMethod => CurveEditorWindowRef.getCurveEditorRectMethod;

        public override bool state => true;

        protected override string prefixMethodName => nameof(GetCurveEditorRectPrefix);

        private static bool GetCurveEditorRectPrefix(EditorWindow __instance, ref Rect __result)
        {
            if (OnGetCurveEditorRect != null)
            {
                var rect = OnGetCurveEditorRect(__instance);
                if (rect != default)
                {
                    __result = rect;
                    return false;
                }
            }

            return true;
        }
    }
}