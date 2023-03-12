/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Integration
{
    [InitializeOnLoad]
    public static class ProGrids
    {
        private static readonly MethodInfo isEnabledMethod;
        private static readonly FieldInfo instanceField;
        private static readonly FieldInfo menuOpenField;
        private static readonly MethodInfo snapEnabledMethod;
        private static readonly MethodInfo snapToGridMethod;
        private static readonly PropertyInfo snapValueInGridUnitsProp;
        private static readonly PropertyInfo snapValueInUnityUnitsProp;
        private static readonly PropertyInfo SnapModifierProp;

        static ProGrids()
        {
            var assembly = Reflection.GetAssembly("Unity.ProGrids.Editor");
            if (assembly == null) return;

            var editorType = assembly.GetType("UnityEditor.ProGrids.ProGridsEditor");
            if (editorType == null) return;

            isEnabledMethod = Reflection.GetMethod(editorType, "IsEnabled", Reflection.StaticLookup);
            snapEnabledMethod = Reflection.GetMethod(editorType, "SnapEnabled", Reflection.StaticLookup);
            snapToGridMethod = Reflection.GetMethod(editorType, "SnapToGrid", new[] { typeof(Transform[]) },
                Reflection.InstanceLookup);
            instanceField = editorType.GetField("s_Instance", Reflection.StaticLookup);
            menuOpenField = editorType.GetField("menuOpen", Reflection.InstanceLookup);
            snapValueInUnityUnitsProp = editorType.GetProperty("SnapValueInUnityUnits", Reflection.InstanceLookup);
            snapValueInGridUnitsProp = editorType.GetProperty("SnapValueInGridUnits", Reflection.InstanceLookup);
            SnapModifierProp = editorType.GetProperty("SnapModifier", Reflection.InstanceLookup);

            if (isEnabledMethod == null ||
                snapEnabledMethod == null ||
                instanceField == null ||
                menuOpenField == null ||
                snapValueInGridUnitsProp == null ||
                snapValueInUnityUnitsProp == null ||
                SnapModifierProp == null)
                return;

            isPresent = true;
        }

        public static bool isEnabled => isPresent && (bool)isEnabledMethod.Invoke(null, null);

        public static bool isMenuOpen
        {
            get
            {
                if (!isPresent) return false;
                var instance = instanceField.GetValue(null);
                if (instance == null) return false;
                return (bool)menuOpenField.GetValue(instance);
            }
        }

        public static bool isPresent { get; }

        public static bool snapEnabled
        {
            get
            {
                if (!isPresent) return false;
                return (bool)snapEnabledMethod.Invoke(null, null);
            }
        }

        public static float snapValueInUnityUnits
        {
            get
            {
                if (!isEnabled) return 0;
                var instance = instanceField.GetValue(null);
                if (instance == null) return 0;
                return (float)snapValueInUnityUnitsProp.GetValue(instance);
            }
            set
            {
                if (!isEnabled) return;

                var instance = instanceField.GetValue(null);
                if (instance == null) return;

                snapValueInGridUnitsProp.SetValue(instance, value);
                SnapModifierProp.SetValue(instance, 2048);
            }
        }

        private static float Snap(float val, float round)
        {
            return round * Mathf.Round(val / round);
        }

        public static Vector3 SnapToGrid(Vector3 position)
        {
            if (!snapEnabled) return position;

            var snapValue = snapValueInUnityUnits;

            return new Vector3(
                Snap(position.x, snapValue),
                Snap(position.y, snapValue),
                Snap(position.z, snapValue));
        }

        public static void SnapToGrid(Transform transform)
        {
            SnapToGrid(new[] { transform });
        }

        public static void SnapToGrid(Transform[] transforms)
        {
            if (!snapEnabled) return;
            var instance = instanceField.GetValue(null);
            snapToGridMethod.Invoke(instance, new object[] { transforms });
        }
    }
}