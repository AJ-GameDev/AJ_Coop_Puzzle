/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Interceptors
{
    public abstract class Interceptor
    {
        protected static Harmony harmony;
        protected MethodInfo patch;

        protected virtual InitType initType => InitType.immediately;

        protected abstract MethodInfo originalMethod { get; }

        protected virtual string prefixMethodName => null;

        protected virtual string postfixMethodName => null;

        protected virtual void Init()
        {
        }

        [InitializeOnLoadMethod]
        private static async void InitInterceptors()
        {
            var types = TypeCache.GetTypesDerivedFrom<Interceptor>();
            var phaseOne = new List<Interceptor>();
            var phaseTwo = new List<Interceptor>();

            harmony = new Harmony("InfinityCode.UltimateEditorEnhancer");

            foreach (var type in types)
            {
                if (type.IsAbstract) continue;
                var interceptor = Activator.CreateInstance(type) as Interceptor;
                interceptor.Init();
                if (interceptor.initType == InitType.immediately) phaseOne.Add(interceptor);
                else phaseTwo.Add(interceptor);
            }

            foreach (var interceptor in phaseOne)
                try
                {
                    interceptor.Patch();
                }
                catch
                {
                }

            while (GUISkinRef.GetCurrent() == null) await Task.Delay(1);

            Debug.unityLogger.logEnabled = false;
            foreach (var interceptor in phaseTwo)
                try
                {
                    interceptor.Patch();
                }
                catch
                {
                }

            Debug.unityLogger.logEnabled = true;
        }

        protected virtual void Patch()
        {
            if (!Prefs.unsafeFeatures) return;
            if (patch != null) return;

            var original = originalMethod;
            if (original == null) return;

            try
            {
                HarmonyMethod prefix = null;
                HarmonyMethod postfix = null;

                var _prefixName = prefixMethodName;
                if (!string.IsNullOrEmpty(_prefixName))
                {
                    var prefixMethod = AccessTools.Method(GetType(), _prefixName);
                    if (prefixMethod != null) prefix = new HarmonyMethod(prefixMethod);
                }

                var _postfixName = postfixMethodName;
                if (!string.IsNullOrEmpty(_postfixName))
                {
                    var postfixMethod = AccessTools.Method(GetType(), _postfixName);
                    if (postfixMethod != null) postfix = new HarmonyMethod(postfixMethod);
                }

                patch = harmony.Patch(original, prefix, postfix);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected virtual void Unpatch()
        {
            if (harmony == null || originalMethod == null || patch == null) return;

            if (!string.IsNullOrEmpty(prefixMethodName))
                harmony.Unpatch(originalMethod, AccessTools.Method(GetType(), prefixMethodName));
            if (!string.IsNullOrEmpty(postfixMethodName))
                harmony.Unpatch(originalMethod, AccessTools.Method(GetType(), postfixMethodName));
            patch = null;
        }

        protected enum InitType
        {
            immediately,
            gui
        }
    }
}