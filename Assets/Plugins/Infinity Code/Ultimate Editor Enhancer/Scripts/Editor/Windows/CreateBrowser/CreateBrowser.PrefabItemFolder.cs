﻿/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Windows
{
    public partial class CreateBrowser
    {
        internal class PrefabItemFolder : FolderItem
        {
            private List<string> skippedLabels;

            public PrefabItemFolder(string[] parts, int index, string path)
            {
                children = new List<Item>();
                label = parts[index];

                if (parts.Length == index + 2)
                {
                    var part = parts[index + 1];
                    if (part.Length < 8) return;

                    var child = new PrefabItem(part, path);
                    child.parent = this;
                    children.Add(child);
                }
                else
                {
                    var child = new PrefabItemFolder(parts, index + 1, path);
                    child.parent = this;
                    children.Add(child);
                }
            }

            public override string title
            {
                get
                {
                    if (skippedLabels == null) return label;
                    return skippedLabels.Last();
                }
            }

            public void Add(string[] parts, int index, string path)
            {
                var next = parts[index + 1];
                var nl = next.Length;

                int i;
                var count = children.Count;
                for (i = 0; i < count; i++)
                {
                    var c = children[i];
                    var l = c.label;
                    if (nl != l.Length) continue;

                    int j;
                    for (j = 0; j < nl; j++)
                        if (l[j] != next[j])
                            break;

                    if (j != nl) continue;

                    var f = c as PrefabItemFolder;
                    if (f != null) f.Add(parts, index + 1, path);
                    break;
                }

                if (i == count)
                {
                    if (parts.Length == index + 2)
                    {
                        if (nl < 8) return;

                        var child = new PrefabItem(next, path);
                        child.parent = this;
                        children.Add(child);
                    }
                    else
                    {
                        var child = new PrefabItemFolder(parts, index + 1, path);
                        child.parent = this;
                        children.Add(child);
                    }
                }
            }

            protected override void InitContent()
            {
                if (folderIconContent == null) folderIconContent = EditorIconContents.folder;
                _content = new GUIContent(folderIconContent);
                if (skippedLabels != null)
                {
                    if (skippedLabels.Count == 1) _content.text = label + "/" + skippedLabels[0];
                    else _content.text = label + "/.../" + skippedLabels.Last();

                    StaticStringBuilder.Clear();

                    StaticStringBuilder.Append(label);
                    foreach (var s in skippedLabels) StaticStringBuilder.Append("/").Append(s);
                    _content.tooltip = StaticStringBuilder.GetString(true);
                }
                else
                {
                    _content.text = label;
                    _content.tooltip = label;
                }
            }

            public void Simplify()
            {
                if (children.Count == 1)
                {
                    var fi = children[0] as PrefabItemFolder;
                    if (fi != null)
                    {
                        children = fi.children;
                        foreach (var child in children) child.parent = this;
                        if (skippedLabels == null) skippedLabels = new List<string>();
                        skippedLabels.Add(fi.label);
                        fi.children = null;
                        fi.Dispose();
                        Simplify();
                    }
                }
                else
                {
                    foreach (var child in children)
                    {
                        var fi = child as PrefabItemFolder;
                        if (fi != null) fi.Simplify();
                    }
                }
            }
        }
    }
}