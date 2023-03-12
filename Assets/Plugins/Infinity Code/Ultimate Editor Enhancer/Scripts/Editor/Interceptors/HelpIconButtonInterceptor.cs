/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Reflection;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer.Interceptors
{
    public class HelpIconButtonInterceptor : StatedInterceptor<HelpIconButtonInterceptor>
    {
        private static readonly Dictionary<Type, bool> typeCache = new();

        protected override MethodInfo originalMethod => EditorGUIRef.helpIconButtonMethod;

        protected override string prefixMethodName => nameof(HelpIconButtonPrefix);

        protected override InitType initType => InitType.gui;

        public override bool state => Prefs.hideEmptyHelpButton;

        private static bool HelpIconButtonPrefix(Rect position, Object[] objs)
        {
            var obj = objs[0];
            if (!(obj is MonoBehaviour)) return true;

            var type = obj.GetType();
            if (type.FullName.StartsWith("UnityEngine.")) return true;

            bool v;
            if (typeCache.TryGetValue(type, out v)) return v;

            typeCache[type] = v = type.GetCustomAttribute(typeof(HelpURLAttribute)) != null;

            return v;
        }
    }
}