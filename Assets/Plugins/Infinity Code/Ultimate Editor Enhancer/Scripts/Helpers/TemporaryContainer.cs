/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public class TemporaryContainer : MonoBehaviour
    {
        private void Awake()
        {
            Destroy(gameObject);
        }

        public static GameObject GetContainer()
        {
            var temporaryContainer = FindObjectOfType<TemporaryContainer>();
            if (temporaryContainer == null)
            {
                var go = new GameObject("Temporary Container");
                go.tag = "EditorOnly";
                temporaryContainer = go.AddComponent<TemporaryContainer>();
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(go, "Create Temporary Container");
#endif
            }

            return temporaryContainer.gameObject;
        }
    }
}