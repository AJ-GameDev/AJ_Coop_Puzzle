/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Integration
{
    [InitializeOnLoad]
    public static class Cinemachine
    {
        static Cinemachine()
        {
            Assembly assembly = Reflection.GetAssembly("Cinemachine");
            if (assembly != null) isPresent = true;
        }

        public static bool isPresent { get; }

        public static bool ContainBrain(GameObject gameObject)
        {
            return isPresent && gameObject.GetComponent("Cinemachine.CinemachineBrain") != null;
        }
    }
}