/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Reflection;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;

namespace InfinityCode.UltimateEditorEnhancer.Interceptors
{
    public class DrawPresetButtonInterceptor : StatedInterceptor<DrawPresetButtonInterceptor>
    {
        protected override InitType initType => InitType.gui;

        protected override MethodInfo originalMethod => PresetSelectorRef.drawPresetButtonMethod;

        protected override string prefixMethodName => nameof(DrawPresetButtonPrefix);

        public override bool state => Prefs.hidePresetButton;

        private static bool DrawPresetButtonPrefix()
        {
            return !Prefs.hidePresetButton;
        }
    }
}