/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer.Attributes
{
    public class RequireComponentsAttribute : ValidateAttribute
    {
        private readonly Type[] types;

        public RequireComponentsAttribute(params Type[] types)
        {
            this.types = types;
        }

        public override bool Validate()
        {
            var go = Selection.activeGameObject;
            if (go == null) return false;

            foreach (var type in types)
                if (go.GetComponent(type) != null)
                    return true;

            return false;
        }
    }
}