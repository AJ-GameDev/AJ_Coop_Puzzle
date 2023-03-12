﻿/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer.SceneTools
{
    [InitializeOnLoad]
    public static class ObjectPlacer
    {
        private static Vector3 lastWorldPosition;
        private static GameObject parent;
        private static Vector3 lastNormal;
        private static bool in2DMode;

        static ObjectPlacer()
        {
            SceneViewManager.AddListener(Invoke);
        }

        public static string GetHelpMessage()
        {
#if !UNITY_EDITOR_OSX
            string rootKey = "CTRL";
#else
            var rootKey = "CMD";
#endif

            var alternativeMessage = GetMessage(Prefs.createBrowserAlternativeTarget);

            StaticStringBuilder.Clear();
            StaticStringBuilder.Append($"Hold {rootKey} to create an object {alternativeMessage}.");
            if (!in2DMode) StaticStringBuilder.Append("\nHold SHIFT to create an object without alignment.");
            return StaticStringBuilder.GetString(true);
        }

        private static string GetMessage(CreateBrowserTarget target)
        {
            if (target == CreateBrowserTarget.root) return "at the root of the scene";
            if (target == CreateBrowserTarget.child) return "as a child of the object under the cursor";
            return "as a sibling of the object under the cursor";
        }

        private static void Invoke(SceneView sceneView)
        {
            var e = Event.current;

            if (e.type != EventType.MouseDown) return;
            if (e.button != 1) return;
            if (e.modifiers != Prefs.objectPlacerModifiers) return;

            in2DMode = sceneView.in2DMode;

            Waila.Close();
            var wnd = CreateBrowser.OpenWindow();
            wnd.OnClose += OnCreateBrowserClose;
            wnd.OnSelectCreate += OnSelectCreate;
            wnd.OnSelectPrefab += OnBrowserPrefab;
            wnd.helpMessage = GetHelpMessage();
            lastWorldPosition = SceneViewManager.lastWorldPosition;
            lastNormal = SceneViewManager.lastNormal;
            parent = SceneViewManager.lastGameObjectUnderCursor;

            e.Use();
        }

        private static void OnBrowserPrefab(string assetPath)
        {
            var go = PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(assetPath)) as GameObject;
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            PlaceObject(go);
        }

        private static void OnCreateBrowserClose(CreateBrowser browser)
        {
            browser.OnClose = null;
            browser.OnSelectCreate = null;
            browser.OnSelectPrefab = null;
        }

        private static void OnSelectCreate(string menuItem)
        {
            var go = Selection.activeGameObject;
            EditorApplication.ExecuteMenuItem(menuItem);
            if (go != Selection.activeGameObject) PlaceObject(Selection.activeGameObject);
        }

        private static void PlaceObject(GameObject go)
        {
            if (go == null) return;

            if (go.GetComponent<Camera>() != null)
            {
                if (Object.FindObjectsOfType<AudioListener>().Length > 1)
                {
                    var audioListener = go.GetComponent<AudioListener>();
                    if (audioListener != null) Object.DestroyImmediate(audioListener);
                }

                parent = null;
            }

            var e = Event.current;

            var rectTransform = go.GetComponent<RectTransform>();
            var sizeDelta = rectTransform != null ? rectTransform.sizeDelta : Vector2.zero;
            var isDefaultTarget = (e.modifiers & EventModifiers.Control) == 0 &&
                                  (e.modifiers & EventModifiers.Command) == 0;
            var target = isDefaultTarget ? Prefs.createBrowserDefaultTarget : Prefs.createBrowserAlternativeTarget;

            if ((target == CreateBrowserTarget.sibling || target == CreateBrowserTarget.child) && parent != null)
            {
                var parentTransform = parent.transform;
                if (target == CreateBrowserTarget.sibling) parentTransform = parentTransform.parent;

                while (parentTransform != null && PrefabUtility.IsPartOfAnyPrefab(parentTransform))
                    parentTransform = parentTransform.parent;

                if (parentTransform != null)
                {
                    parent = parentTransform.gameObject;
                    go.transform.SetParent(parentTransform);
                }
                else
                {
                    parent = null;
                }
            }

            var allowDown = true;
            var useCanvas = parent != null && parent.GetComponent<RectTransform>() != null;
            var hasRectTransform = rectTransform != null;

            if (useCanvas || hasRectTransform || in2DMode) allowDown = false;

            go.transform.position = lastWorldPosition;
            if (allowDown && (e.modifiers & EventModifiers.Shift) == 0)
            {
                var cubeSide = MathHelper.NormalToCubeSide(lastNormal);
                var c = go.GetComponent<Collider>();
                if (c != null)
                {
                    var extents = c.bounds.extents;
                    if (extents != Vector3.zero)
                    {
                        extents.Scale(cubeSide);
                        //Vector3 v = extents - c.bounds.center;
                        go.transform.Translate(extents.x, extents.y, extents.z, Space.World);
                    }
                }
                else
                {
                    var r = go.GetComponent<Renderer>();
                    if (r != null)
                    {
                        var extents = r.bounds.extents;
                        if (extents != Vector3.zero)
                        {
                            extents.Scale(cubeSide);
                            go.transform.Translate(extents.x, extents.y, extents.z, Space.World);
                        }
                    }
                }
            }
            else if (!useCanvas && hasRectTransform)
            {
                var canvas = CanvasUtils.GetCanvas();
                go.transform.SetParent(canvas.transform);
                go.transform.localPosition = Vector3.zero;
                useCanvas = true;
            }

            if (useCanvas && rectTransform != null)
            {
                var pos = rectTransform.localPosition;
                if (Math.Abs(rectTransform.anchorMin.x) < float.Epsilon &&
                    Math.Abs(rectTransform.anchorMax.x - 1) < float.Epsilon) pos.x = 0;
                if (Math.Abs(rectTransform.anchorMin.y) < float.Epsilon &&
                    Math.Abs(rectTransform.anchorMax.y - 1) < float.Epsilon) pos.y = 0;

                rectTransform.localPosition = pos;
                rectTransform.sizeDelta = sizeDelta;
            }

            Selection.activeGameObject = go;

            if (SnapHelper.enabled) SnapHelper.Snap(go.transform);
        }
    }
}