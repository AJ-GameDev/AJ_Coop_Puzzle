/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public class GenericMenuEx
    {
        public List<GenericMenuItem> items;

        public GenericMenuEx()
        {
            items = new List<GenericMenuItem>();
        }

        public static GenericMenuEx instance { get; private set; }

        public GenericMenuItem this[int index] => items[index];

        public int count => items.Count;

        public void Add(string label, GenericMenu.MenuFunction action)
        {
            Add(new GUIContent(label), false, action);
        }

        public void Add(GUIContent content, GenericMenu.MenuFunction action)
        {
            items.Add(new GenericMenuItem
            {
                content = content,
                action = action
            });
        }

        public void Add(string label, GenericMenu.MenuFunction2 action, object userdata)
        {
            Add(new GUIContent(label), false, action, userdata);
        }

        public void Add(string label, bool on, GenericMenu.MenuFunction action)
        {
            Add(new GUIContent(label), on, action);
        }

        public void Add(GUIContent content, bool on, GenericMenu.MenuFunction action)
        {
            items.Add(new GenericMenuItem
            {
                content = content,
                action = action,
                on = on
            });
        }

        public void Add(string label, bool on, GenericMenu.MenuFunction2 action, object userdata)
        {
            Add(new GUIContent(label), false, action, userdata);
        }

        public void Add(GUIContent content, bool on, GenericMenu.MenuFunction2 action, object userdata)
        {
            items.Add(new GenericMenuItem
            {
                content = content,
                action2 = action,
                on = on,
                userdata = userdata
            });
        }

        public void AddDisabled(string label, bool on = false)
        {
            items.Add(new GenericMenuItem
            {
                content = new GUIContent(label),
                on = on,
                disabled = true
            });
        }

        public void AddSeparator(string path = "")
        {
            items.Add(new GenericMenuItem
            {
                separator = true,
                path = path
            });
        }

        public void Insert(int index, string label, GenericMenu.MenuFunction action)
        {
            Insert(index, new GUIContent(label), false, action);
        }

        public void Insert(int index, string label, bool on, GenericMenu.MenuFunction action)
        {
            Insert(index, new GUIContent(label), on, action);
        }

        public void Insert(int index, GUIContent content, bool on, GenericMenu.MenuFunction action)
        {
            items.Insert(index, new GenericMenuItem
            {
                content = content,
                action = action,
                on = on
            });
        }

        public void Show()
        {
            var menu = new GenericMenu();
            foreach (var item in items)
            {
                if (item.separator) menu.AddSeparator(item.path);
                else if (item.disabled) menu.AddDisabledItem(item.content, item.on);
                else if (item.userdata != null) menu.AddItem(item.content, item.on, item.action2, item.userdata);
                else menu.AddItem(item.content, item.on, item.action);
                item.Dispose();
            }

            items.Clear();

            menu.ShowAsContext();
        }

        public void ShowAsContext()
        {
            Show();
        }

        public static GenericMenuEx Start()
        {
            if (instance == null) instance = new GenericMenuEx();
            else instance.items.Clear();

            return instance;
        }
    }
}