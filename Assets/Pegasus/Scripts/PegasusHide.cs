using UnityEngine;

namespace Pegasus
{
    /// <summary>
    ///     Simple class to hide mesh renderers and disable colliders at runtime
    /// </summary>
    public class PegasusHide : MonoBehaviour
    {
        public bool m_hideAtRuntime = true;

        // Use this for initialization
        private void Start()
        {
            if (m_hideAtRuntime)
            {
                var colliders = GetComponentsInChildren<Collider>();
                foreach (var collider in colliders) collider.enabled = false;

                var renderers = GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in renderers) renderer.enabled = false;
            }
        }
    }
}