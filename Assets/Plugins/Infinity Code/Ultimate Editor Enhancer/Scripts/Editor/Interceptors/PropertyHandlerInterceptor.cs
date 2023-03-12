/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Reflection;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer.Interceptors
{
    public class PropertyHandlerInterceptor : StatedInterceptor<PropertyHandlerInterceptor>
    {
        public static Action<SerializedProperty, GenericMenu> OnAddMenuItems;

        protected override MethodInfo originalMethod => PropertyHandlerRef.addMenuItemsMethod;

        protected override string postfixMethodName => nameof(AddMenuItemsPostfix);

        public override bool state => true;

        private static void AddMenuItemsPostfix(SerializedProperty property, GenericMenu menu)
        {
            if (OnAddMenuItems == null) return;
            var invocationList = OnAddMenuItems.GetInvocationList();
            object[] args = { property, menu };

            for (var i = 0; i < invocationList.Length; i++)
            {
                var d = invocationList[i];
                try
                {
                    d.DynamicInvoke(args);
                }
                catch (Exception e)
                {
                    Log.Add(e);
                }
            }
        }
    }
}