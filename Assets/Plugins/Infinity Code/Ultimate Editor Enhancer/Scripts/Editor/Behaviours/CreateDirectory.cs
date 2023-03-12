/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Behaviors
{
    [InitializeOnLoad]
    public static class CreateDirectory
    {
        static CreateDirectory()
        {
            var binding = KeyManager.AddBinding();
            binding.OnPress += OnInvoke;
            binding.OnValidate += OnValidate;
        }

        private static void OnInvoke()
        {
            var obj = Selection.objects[0];
            var path = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(path)) return;
            ProjectWindowUtil.CreateFolder();
        }

        private static bool OnValidate()
        {
            if (Event.current.keyCode != KeyCode.F7) return false;
            if (Selection.objects.Length != 1) return false;
            return true;
        }
    }
}