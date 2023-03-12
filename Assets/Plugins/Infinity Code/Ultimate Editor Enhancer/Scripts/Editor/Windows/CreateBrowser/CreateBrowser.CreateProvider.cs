/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace InfinityCode.UltimateEditorEnhancer.Windows
{
    public partial class CreateBrowser
    {
        public class CreateProvider : Provider
        {
            public string[] skipMenuItems =
            {
                "GameObject/Create Empty Child".ToLower(),
                "GameObject/Create Empty Parent".ToLower(),
                "GameObject/Group".ToLower(),
                "GameObject/Ungroup".ToLower()
            };

            public override float order => 0;

            public override string title => instance.createLabel;

            public override void Cache()
            {
                items = new List<Item>();

                foreach (var submenu in Unsupported.GetSubmenus("GameObject"))
                {
                    var l = submenu.ToLower();
                    if (l == "GameObject/Center On Children".ToLower()) break;
                    if (skipMenuItems.Contains(l)) continue;

                    var menuItemStr = submenu.Substring(11);
                    if (menuItemStr == "Create Empty") menuItemStr = "Empty GameObject";
                    if (menuItemStr == "Create Empty Collection") menuItemStr = "Collection";
                    if (menuItemStr == "Create Header") menuItemStr = "Header";
                    var parts = menuItemStr.Split('/');
                    if (parts.Length == 1)
                    {
                        items.Add(new CreateItem(menuItemStr, submenu));
                    }
                    else
                    {
                        var root = items.FirstOrDefault(i => i.label == parts[0]) as CreateItemFolder;
                        if (root != null) root.Add(parts, 0, submenu);
                        else items.Add(new CreateItemFolder(parts, 0, submenu));
                    }
                }

                items = items.OrderBy(o =>
                {
                    if (o is FolderItem) return 1;
                    if (o.label == "Empty GameObject") return -1;
                    return 0;
                }).ThenBy(o => o.label).ToList();
            }

            public override int IndexOf(Item item)
            {
                return items.IndexOf(item);
            }
        }
    }
}