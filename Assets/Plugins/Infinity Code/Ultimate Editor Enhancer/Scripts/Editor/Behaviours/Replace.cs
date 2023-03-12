﻿/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Collections.Generic;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Behaviors
{
    [InitializeOnLoad]
    public static class Replace
    {
        private static GameObject[] replaceTargets;

        static Replace()
        {
            var binding = KeyManager.AddBinding();
            binding.OnValidate = OnValidate;
            binding.OnPress = OnInvoke;

            GameObjectUtils.OnPrepareGameObjectMenu += OnPrepareGameObjectMenu;
        }

        private static void OnBrowserClose(CreateBrowser browser)
        {
            browser.OnClose -= OnBrowserClose;
            browser.OnSelectCreate -= OnBrowserCreate;
            browser.OnSelectPrefab -= OnBrowserPrefab;
        }

        private static void OnBrowserCreate(string menuItem)
        {
            var s = Selection.activeGameObject;
            EditorApplication.ExecuteMenuItem(menuItem);
            if (Selection.activeGameObject == s || Selection.activeGameObject == null) return;

            var asset = Selection.activeGameObject;

            Undo.SetCurrentGroupName("Replace GameObjects");
            var group = Undo.GetCurrentGroup();

            var newSelection = new List<GameObject>();

            foreach (var go in replaceTargets)
            {
                var t = go.transform;
                var index = t.GetSiblingIndex();
                var ngo = Object.Instantiate(asset);
                newSelection.Add(ngo);
                Undo.RegisterCreatedObjectUndo(ngo, ngo.name);
                ngo.name = asset.name;
                var nt = ngo.transform;
                Undo.SetTransformParent(nt.transform, t.transform.parent, "Parenting");
                nt.position = t.position;
                nt.rotation = t.rotation;
                nt.localScale = t.localScale;
                Undo.DestroyObjectImmediate(go);
                Undo.RecordObject(nt, "Sibling");
                nt.SetSiblingIndex(index);
            }

            Object.DestroyImmediate(asset);

            Undo.CollapseUndoOperations(group);
            Selection.objects = newSelection.ToArray();
        }

        private static void OnBrowserPrefab(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null) return;

            Undo.SetCurrentGroupName("Replace GameObjects");
            var group = Undo.GetCurrentGroup();

            var newSelection = new List<GameObject>();

            foreach (var go in replaceTargets)
            {
                var t = go.transform;
                var index = t.GetSiblingIndex();
                var ngo = PrefabUtility.InstantiatePrefab(asset) as GameObject;
                newSelection.Add(ngo);
                Undo.RegisterCreatedObjectUndo(ngo, ngo.name);
                var nt = ngo.transform;
                Undo.SetTransformParent(nt.transform, t.transform.parent, "Parenting");
                nt.position = t.position;
                nt.rotation = t.rotation;
                nt.localScale = t.localScale;
                Undo.DestroyObjectImmediate(go);
                Undo.RecordObject(nt, "Sibling");
                nt.SetSiblingIndex(index);
            }

            Undo.CollapseUndoOperations(group);
            Selection.objects = newSelection.ToArray();
        }

        private static void OnInvoke()
        {
            Show(Selection.gameObjects);
        }

        private static void OnPrepareGameObjectMenu(GenericMenuEx menu, GameObject[] targets)
        {
            var match = false;

            for (var i = 0; i < menu.count; i++)
            {
                var item = menu[i];
                if (item.content != null && item.content.text == "Group %g")
                {
                    menu.Insert(i, "Replace", () => Show(targets));
                    match = true;
                    break;
                }
            }

            if (!match) menu.Add("Replace", () => Show(targets));
        }

        private static bool OnValidate()
        {
            if (!Prefs.replace) return false;

            var e = Event.current;
            if (e.keyCode != Prefs.replaceKeyCode) return false;
            if (e.modifiers != Prefs.replaceModifiers) return false;
            if (Selection.gameObjects.Length == 0) return false;
            return true;
        }

        public static void Show(GameObject[] targets)
        {
            replaceTargets = targets;
            var browser = CreateBrowser.OpenWindow();
            browser.titleContent = new GUIContent("Replace to");
            browser.OnClose += OnBrowserClose;
            browser.OnSelectCreate += OnBrowserCreate;
            browser.OnSelectPrefab += OnBrowserPrefab;

            browser.createLabel = "Replace to New Item";
            browser.prefabsLabel = "Replace to Prefab";
        }
    }
}