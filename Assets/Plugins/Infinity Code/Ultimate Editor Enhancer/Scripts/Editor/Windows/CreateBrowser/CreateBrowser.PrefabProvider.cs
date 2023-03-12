/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer.Windows
{
    public partial class CreateBrowser
    {
        public class PrefabProvider : Provider
        {
            public override float order => 1;

            public override string title => instance.prefabsLabel;

            public override void Cache()
            {
                items = new List<Item>();

                var blacklist =
                    Prefs.createBrowserBlacklist.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

                CacheItems(blacklist, "t:prefab");
                CacheItems(blacklist, "t:model");

                foreach (var item in items)
                {
                    var fi = item as PrefabItemFolder;
                    if (fi == null) continue;
                    fi.Simplify();
                }

                items = items.OrderBy(o =>
                {
                    if (o is FolderItem) return 0;
                    return -1;
                }).ThenBy(o => o.label).ToList();
            }

            private void CacheItems(string[] blacklist, string filter)
            {
                var hasBlacklist = blacklist.Length > 0;

                var assets = AssetDatabase.FindAssets(filter, new[] { "Assets" });
                foreach (var guid in assets)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (hasBlacklist)
                    {
                        var inBlackList = false;
                        for (var i = 0; i < blacklist.Length; i++)
                        {
                            var s = blacklist[i];
                            if (s.Length > assetPath.Length) continue;

                            int j;
                            for (j = 0; j < s.Length; j++)
                                if (s[i] != assetPath[i])
                                    break;

                            if (j == assetPath.Length)
                            {
                                inBlackList = true;
                                break;
                            }
                        }

                        if (inBlackList) continue;
                    }

                    var shortPath = assetPath.Substring(7);
                    var parts = shortPath.Split('/');
                    if (parts.Length == 1)
                    {
                        if (shortPath.Length < 8) continue;
                        items.Add(new PrefabItem(shortPath, assetPath));
                    }
                    else
                    {
                        var label = parts[0];
                        int i;
                        for (i = 0; i < items.Count; i++)
                        {
                            var item = items[i];
                            var l = item.label;
                            if (l.Length != label.Length) continue;

                            int j;
                            for (j = 0; j < l.Length; j++)
                                if (l[j] != label[j])
                                    break;

                            if (j != l.Length) continue;

                            var f = item as PrefabItemFolder;
                            if (f != null) f.Add(parts, 0, assetPath);
                            break;
                        }

                        if (i == items.Count) items.Add(new PrefabItemFolder(parts, 0, assetPath));
                    }
                }
            }
        }
    }
}