/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer.Attributes
{
    public class RuntimeOnlyAttribute : ValidateAttribute
    {
        public override bool Validate()
        {
            return EditorApplication.isPlaying;
        }
    }
}