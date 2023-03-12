/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.IO;

namespace InfinityCode.UltimateEditorEnhancer.Windows
{
    [Serializable]
    public class SceneHistoryItem : SearchableItem
    {
        public string name;
        public string path;

        [NonSerialized] public bool exists;

        public void CheckExists()
        {
            exists = File.Exists(path);
        }

        protected override int GetSearchCount()
        {
            return 1;
        }

        protected override string GetSearchString(int index)
        {
            return name;
        }
    }
}