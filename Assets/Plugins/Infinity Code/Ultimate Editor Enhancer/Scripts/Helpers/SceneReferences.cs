/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InfinityCode.UltimateEditorEnhancer
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class SceneReferences : MonoBehaviour
    {
        public static Action<SceneReferences> OnCreate;
        public static Action OnUpdateInstances;

        public static List<SceneReferences> instances = new();

        public List<SceneBookmark> bookmarks = new();
        public List<HierarchyBackground> hierarchyBackgrounds = new();

#if UNITY_EDITOR
        static SceneReferences()
        {
            EditorApplication.delayCall += UpdateInstances;
        }
#endif

        public static SceneReferences Get(Scene scene, bool createIfMissed = true)
        {
            if (instances == null) return null;
            var r = instances.FirstOrDefault(i => i != null && i.gameObject.scene == scene);
            if (r != null) return r;
            if (!createIfMissed) return null;

            var activeScene = SceneManager.GetActiveScene();
            var sceneChanged = false;
            if (activeScene != scene)
            {
                SceneManager.SetActiveScene(scene);
                sceneChanged = true;
            }

            var go = new GameObject("Ultimate Editor Enhancer References");
            go.tag = "EditorOnly";
            r = go.AddComponent<SceneReferences>();
            if (OnCreate != null) OnCreate(r);
            instances.Add(r);
            if (sceneChanged) SceneManager.SetActiveScene(activeScene);

            return r;
        }

        public HierarchyBackground GetBackground(GameObject target, bool useNonRecursive = false)
        {
            foreach (var b in hierarchyBackgrounds)
                if (b.gameObject == target)
                    return b;

            var t = target.transform.parent;
            if (useNonRecursive)
                while (t != null)
                {
                    var go = t.gameObject;

                    foreach (var b in hierarchyBackgrounds)
                        if (b.gameObject == go)
                            return b;

                    t = t.parent;
                }
            else
                while (t != null)
                {
                    var go = t.gameObject;

                    foreach (var b in hierarchyBackgrounds)
                        if (b.gameObject == go)
                        {
                            if (!b.recursive) return null;
                            return b;
                        }

                    t = t.parent;
                }

            return null;
        }

        public static void UpdateInstances()
        {
            instances = FindObjectsOfType<SceneReferences>().ToList();
            if (OnUpdateInstances != null) OnUpdateInstances();
        }

        [Serializable]
        public class HierarchyBackground
        {
            public GameObject gameObject;
            public Color color;
            public bool recursive;
        }
    }
}