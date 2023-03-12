﻿/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfinityCode.UltimateEditorEnhancer
{
    [InitializeOnLoad]
    public static class SceneManagerHelper
    {
        static SceneManagerHelper()
        {
            EditorApplication.delayCall += () =>
            {
                EditorSceneManager.sceneOpened -= OnSceneOpened;
                EditorSceneManager.sceneOpened += OnSceneOpened;

                EditorSceneManager.sceneClosed -= OnSceneClosed;
                EditorSceneManager.sceneClosed += OnSceneClosed;
            };

            SceneReferences.OnCreate += OnCreateSceneReferences;

#if !UCONTEXT
            TryFixUContext();
#endif
            UpdateInstances();
        }

        public static bool AskForSave(params Scene[] scenes)
        {
            if (scenes.Length == 0) return true;

            var paths = new List<string>();

            for (var i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];

                if (scene.isDirty) paths.Add(scene.path);
            }

            if (paths.Count > 0)
            {
                var result = EditorUtility.DisplayDialogComplex("Scene(s) Have Been Modified",
                    "Do you want to save the changes you made in the scenes:\n" + string.Join("\n", paths) +
                    "\n\nYour changes will be lost if you don't save them.", "Save", "Don't Save", "Cancel");
                if (result == 2) return false;

                if (result == 0)
                    for (var i = 0; i < scenes.Length; i++)
                    {
                        var scene = scenes[i];
                        if (scene.isDirty) EditorSceneManager.SaveScene(scene);
                    }
            }

            return true;
        }

        private static void OnCreateSceneReferences(SceneReferences r)
        {
            r.gameObject.hideFlags = Prefs.hideSceneReferences ? HideFlags.HideInHierarchy : HideFlags.None;
            Undo.RegisterCreatedObjectUndo(r.gameObject, "References");
        }

        private static void OnSceneClosed(Scene scene)
        {
            EditorApplication.delayCall += UpdateInstances;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            EditorApplication.delayCall += () =>
            {
#if !UCONTEXT
                TryFixUContext(scene);
#endif
                UpdateInstances();
            };
        }

        private static void TryFixUContext()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++) TryFixUContext(SceneManager.GetSceneAt(i));
        }

        private static void TryFixUContext(Scene scene)
        {
            if (!scene.isLoaded) return;

            var gos = scene.GetRootGameObjects();
            for (var i = gos.Length - 1; i >= 0; i--)
            {
                var go = gos[i];
                if (go.name == "uContext References")
                {
                    Selection.activeGameObject = null;
                    EditorSceneManager.MarkSceneDirty(scene);
                    Object.DestroyImmediate(go);
                }
            }
        }

        public static void UpdateInstances()
        {
            var hideFlags = Prefs.hideSceneReferences ? HideFlags.HideInHierarchy : HideFlags.None;

            SceneReferences.UpdateInstances();
            foreach (var r in SceneReferences.instances)
            {
                var go = r.gameObject;
                if (go.hideFlags == hideFlags) continue;

                go.hideFlags = hideFlags;
                EditorUtility.SetDirty(go);
            }
        }
    }
}