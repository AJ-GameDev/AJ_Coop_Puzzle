/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using InfinityCode.UltimateEditorEnhancer.EditorMenus;
using InfinityCode.UltimateEditorEnhancer.EditorMenus.Actions;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Windows
{
    [InitializeOnLoad]
    public partial class ViewGallery : EditorWindow
    {
        public delegate void DrawCamerasDelegate(ViewGallery gallery, float rowHeight, float maxLabelWidth,
            ref int offsetY, ref int row);

        private const int VERTICAL_MARGIN = 50;
        private const int MAX_FLAT_HEIGHT = 150;

        public static Action<GenericMenuEx> OnPrepareViewStatesMenu;
        public static bool closeOnSelect;

        private static GUIStyle selectedStyle;
        public static bool isDirty = true;

        public int countCols;
        public float itemWidth;
        public float itemHeight;
        public Vector2 lastSize;
        public float offsetX;
        private string _filter;

        public CameraStateItem[] cameras;
        private int countAutoViews;
        private int countRows;
        private int countTemporaryCameras;
        private ViewItem[] filteredItems;
        private ViewStateItem[] views;

        static ViewGallery()
        {
            var binding = KeyManager.AddBinding();
            binding.OnValidate += OnValidate;
            binding.OnPress += OnInvoke;
        }

        private void OnEnable()
        {
            isDirty = true;
        }

        private void OnDestroy()
        {
            isDirty = true;
            closeOnSelect = false;
            DestroyTextures();
        }

        private void OnGUI()
        {
            if (selectedStyle == null)
            {
                selectedStyle = new GUIStyle(Styles.selectedRow);
                selectedStyle.fixedHeight = 0;
            }

            if (position.size != lastSize) isDirty = true;
            else if (cameras == null || views == null) isDirty = true;
            else if (cameras.Any(c => c == null) || views.Any(v => v == null)) isDirty = true;

            if (isDirty) CacheItems();

            DrawToolbar();

            if (filteredItems == null)
            {
                if (position.height > VERTICAL_MARGIN + MAX_FLAT_HEIGHT) DrawAllItems();
                else DrawFlatItems();
            }
            else
            {
                DrawFilteredItems();
            }


            GUI.changed = true;
        }

        private void OnFocus()
        {
            isDirty = true;
        }

        private void OnHierarchyChange()
        {
            isDirty = true;
        }

        private static bool OnValidate()
        {
            if (!Prefs.viewGalleryHotKey) return false;

            var e = Event.current;
            if (e.modifiers != Prefs.viewGalleryModifiers) return false;
            if (e.keyCode != Prefs.viewGalleryKeyCode) return false;
            return true;
        }

        private void CacheItems()
        {
            DestroyTextures();

            var sceneViews = SceneView.sceneViews;
            if (sceneViews == null || sceneViews.Count == 0)
            {
                sceneViews = new ArrayList();
                sceneViews.Add(GetWindow<SceneView>());
            }

            InitItems(sceneViews);

            CalcItemSize();
            RenderItems();

            isDirty = false;
        }

        private void CalcItemSize()
        {
            var size = lastSize = position.size;
            size.x -= 20; // margin horizontal
            size.y -= VERTICAL_MARGIN; // margin vertical + labels height

            if (filteredItems == null)
            {
                if (size.y > MAX_FLAT_HEIGHT)
                {
                    size.y -= 70;
                    countCols = Mathf.Max(cameras.Length, views.Length);
                    countRows = cameras.Length > 0 ? 2 : 1;
                }
                else
                {
                    countCols = cameras.Length + views.Length - 1;
                    countRows = 1;
                }
            }
            else
            {
                countCols = filteredItems.Length;
                countRows = 1;
            }

            if (countCols == 0) countCols = 1;

            itemWidth = size.x / countCols;
            itemHeight = itemWidth * 0.75f; // 4:3

            if (itemHeight * countRows < size.y)
            {
                for (var cols = countCols - 1; cols > 0; cols--)
                {
                    var w = size.x / cols;
                    var h = w * 0.75f;

                    int rows;

                    if (filteredItems == null)
                        rows = Mathf.CeilToInt(cameras.Length / (float)cols) +
                               Mathf.CeilToInt(views.Length / (float)cols);
                    else rows = Mathf.CeilToInt(filteredItems.Length / (float)cols);

                    if (w > itemWidth && h * rows < size.y)
                    {
                        itemWidth = w;
                        itemHeight = h;
                        countCols = cols;
                        countRows = rows;
                    }
                    else
                    {
                        break;
                    }
                }

                itemHeight = Mathf.FloorToInt((itemHeight - 15) / 3) * 3;
                itemWidth = itemHeight / 3 * 4;
            }
            else
            {
                itemHeight = Mathf.FloorToInt(size.y / countRows / 3) * 3;
                itemWidth = itemHeight / 3 * 4;
            }

            offsetX = (lastSize.x - itemWidth * countCols) / (countCols + 1);
        }

        private static void CreateViewState(object userdata)
        {
            var vi = userdata as ViewStateItem;
            if (vi != null)
            {
                SceneViewActions.SaveViewState();
                return;
            }

            Camera cam;
            if (userdata is Camera) cam = userdata as Camera;
            else if (userdata is CameraStateItem) cam = (userdata as CameraStateItem).camera;
            else return;

            var container = TemporaryContainer.GetContainer();
            if (container == null) return;

            var pattern = @"View State \((\d+)\)";

            var maxIndex = 1;
            var viewStates = container.GetComponentsInChildren<ViewState>();
            for (var i = 0; i < viewStates.Length; i++)
            {
                var name = viewStates[i].gameObject.name;
                var match = Regex.Match(name, pattern);
                if (match.Success)
                {
                    var strIndex = match.Groups[1].Value;
                    var index = int.Parse(strIndex);
                    if (index >= maxIndex) maxIndex = index + 1;
                }
            }

            var viewStateName = "View State (" + maxIndex + ")";
            InputDialog.Show("Enter title of View State", viewStateName, s =>
            {
                var go = new GameObject(viewStateName);
                go.tag = "EditorOnly";
                var viewState = go.AddComponent<ViewState>();

                var t = cam.transform;
                var dist = cam.orthographic
                    ? 0
                    : 5 / Mathf.Tan((float)(SceneView.lastActiveSceneView.camera.fieldOfView * 0.5 *
                                            (Math.PI / 180.0)));
                viewState.pivot = t.position + t.forward * dist;
                viewState.rotation = t.rotation;
                viewState.size = 10;
                viewState.is2D = cam.orthographic;
                viewState.title = s;

                go.transform.SetParent(container.transform, true);
                EditorMenu.Close();
            });
        }

        private void DestroyTextures()
        {
            if (cameras != null)
                foreach (var cam in cameras)
                    if (cam.texture != null)
                        DestroyImmediate(cam.texture);

            if (views != null)
                foreach (var view in views)
                    if (view.texture != null)
                        DestroyImmediate(view.texture);
        }

        private void DrawAllItems()
        {
            var offsetY = 25;
            var row = 0;
            var rowHeight = itemHeight + 25;
            var maxLabelWidth = lastSize.x / countCols - 10;

            DrawCameras(rowHeight, maxLabelWidth, ref offsetY, ref row);
            DrawViewStates(row, rowHeight, offsetY, maxLabelWidth);
        }

        private void DrawCameras(float rowHeight, float maxLabelWidth, ref int offsetY, ref int row)
        {
            GUI.Label(new Rect(new Vector2(offsetX, offsetY), new Vector2(lastSize.x, 20)), "Cameras:",
                EditorStyles.boldLabel);

            offsetY += 20;
            for (var i = 0; i < cameras.Length; i++)
            {
                var col = i % countCols;

                var x = col * itemWidth + (col + 1) * offsetX;
                var rect = new Rect(x, row * rowHeight + offsetY, itemWidth, itemHeight);
                var cameraState = cameras[i];

                if (cameraState.Draw(rect, maxLabelWidth))
                {
                    cameraState.Set();
                    TryCloseDelayed();
                }

                if (i != cameras.Length - 1 && col == countCols - 1) row++;
            }

            row++;
            offsetY += 5;
        }

        private void DrawFilteredItems()
        {
            var offsetY = 25;
            var row = 0;
            var rowHeight = itemHeight + 25;
            var maxLabelWidth = lastSize.x / countCols - 10;

            for (var i = 0; i < filteredItems.Length; i++)
            {
                var col = i % countCols;

                var x = col * itemWidth + (col + 1) * offsetX;
                var rect = new Rect(x, row * rowHeight + offsetY, itemWidth, itemHeight);
                var item = filteredItems[i];
                if (item == null) continue;

                if (item.Draw(rect, maxLabelWidth))
                {
                    item.Set();
                    TryCloseDelayed();
                }

                if (i != filteredItems.Length - 1 && col == countCols - 1) row++;
            }
        }

        private void DrawFlatItems()
        {
            var offsetY = 25;
            var row = 0;
            var rowHeight = itemHeight + 25;
            var maxLabelWidth = lastSize.x / countCols - 10;
            var index = 0;
            var totalItems = cameras.Length + views.Length - 1;

            for (var i = 0; i < cameras.Length; i++)
            {
                var col = index % countCols;

                var x = col * itemWidth + (col + 1) * offsetX;
                var rect = new Rect(x, row * rowHeight + offsetY, itemWidth, itemHeight);
                ViewItem item = cameras[i];
                if (item == null) continue;

                if (item.Draw(rect, maxLabelWidth))
                {
                    item.Set();
                    TryCloseDelayed();
                }

                if (index != totalItems - 1 && col == countCols - 1) row++;
                index++;
            }

            for (var i = 1; i < views.Length; i++)
            {
                var col = index % countCols;

                var x = col * itemWidth + (col + 1) * offsetX;
                var rect = new Rect(x, row * rowHeight + offsetY, itemWidth, itemHeight);
                ViewItem item = views[i];
                if (item == null) continue;

                if (item.Draw(rect, maxLabelWidth))
                {
                    item.Set();
                    TryCloseDelayed();
                }

                if (index != totalItems - 1 && col == countCols - 1) row++;
                index++;
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (countTemporaryCameras > 0)
                if (GUILayoutUtils.ToolbarButton("Cameras"))
                {
                    var menu = GenericMenuEx.Start();

                    menu.Add("Remove All Temporary Cameras", RemoveAllTemporaryCameras);

                    foreach (var cam in cameras.Where(c => c.camera.GetComponentInParent<TemporaryContainer>() != null))
                        menu.Add("Remove " + cam.camera.gameObject.name, RemoveTemporaryCamera, cam);

                    menu.Show();
                }

            if (GUILayoutUtils.ToolbarButton("View States"))
            {
                var menu = GenericMenuEx.Start();

                menu.Add("Create/From Current View", SceneViewActions.SaveViewState);

                if (views.Length > countAutoViews)
                {
                    menu.Add("Remove/All View States", RemoveAllViewStates);
                    menu.AddSeparator("Remove/");

                    for (var i = countAutoViews; i < views.Length; i++)
                    {
                        var v = views[i];
                        menu.Add("Remove/" + v.title, RemoveViewState, v);
                    }
                }

                if (OnPrepareViewStatesMenu != null) OnPrepareViewStatesMenu(menu);

                menu.Show();
            }

            EditorGUI.BeginChangeCheck();
            _filter = GUILayoutUtils.ToolbarSearchField(_filter);
            if (EditorGUI.EndChangeCheck()) UpdateFilteredItems();

            if (GUILayoutUtils.ToolbarButton("Refresh")) isDirty = true;
            if (GUILayoutUtils.ToolbarButton("?")) Links.OpenDocumentation("view-gallery");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawViewStates(int row, float rowHeight, int offsetY, float maxLabelWidth)
        {
            GUI.Label(new Rect(new Vector2(offsetX, row * rowHeight + offsetY), new Vector2(lastSize.x, 20)),
                "View States:", EditorStyles.boldLabel);
            offsetY += 20;

            for (var i = 0; i < views.Length; i++)
            {
                var col = i % countCols;

                var x = col * itemWidth + (col + 1) * offsetX;
                var rect = new Rect(x, row * rowHeight + offsetY, itemWidth, itemHeight);
                var view = views[i];
                if (view == null) continue;
                if (view.Draw(rect, maxLabelWidth))
                {
                    view.Set();
                    TryCloseDelayed();
                }

                if (i != views.Length - 1 && col == countCols - 1) row++;
            }
        }

        private void InitItems(ArrayList sceneViews)
        {
            cameras = FindObjectsOfType<Camera>().OrderBy(c => c.name).Select(c => new CameraStateItem(c)).ToArray();
            countTemporaryCameras = cameras.Count(c => c.camera.GetComponentInParent<TemporaryContainer>() != null);

            var viewStates = FindObjectsOfType<ViewState>().OrderBy(v => v.gameObject.name).ToArray();

            var sceneCount = sceneViews.Count;
            countAutoViews = 0;
            var tempViews = new List<ViewStateItem>();
            for (var i = 0; i < sceneCount; i++)
            {
                var sceneView = sceneViews[i] as SceneView;
                var t = "Scene View";
                if (sceneCount > 1) t += " " + (i + 1);
                countAutoViews++;
                tempViews.Add(new ViewStateItem
                {
                    title = t,
                    pivot = sceneView.pivot,
                    size = sceneView.size,
                    rotation = sceneView.rotation,
                    is2D = sceneView.in2DMode,
                    view = sceneView
                });
            }

            var canvases = FindObjectsOfType<Canvas>().Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay)
                .ToArray();
            if (canvases.Length > 0)
            {
                var bounds = new Bounds();

                var fourCorners = new Vector3[4];
                for (var i = 0; i < canvases.Length; i++)
                {
                    var rt = canvases[i].GetComponent<RectTransform>();
                    rt.GetWorldCorners(fourCorners);

                    if (i == 0) bounds = new Bounds(fourCorners[0], Vector3.zero);
                    for (var k = 0; k < 4; k++) bounds.Encapsulate(fourCorners[k]);
                }

                tempViews.Add(new ViewStateItem
                {
                    title = "UI",
                    is2D = true,
                    renderUI = true,
                    pivot = bounds.center,
                    size = bounds.extents.magnitude
                });
            }

            tempViews.AddRange(viewStates.Select(t => new ViewStateItem(t)));
            views = tempViews.ToArray();

            if (!string.IsNullOrEmpty(_filter)) UpdateFilteredItems();
        }

        private static void OnInvoke()
        {
            OpenWindow();
        }

        [MenuItem(WindowsHelper.MenuPath + "View Gallery", false, 102)]
        public static void OpenWindow()
        {
            GetWindow<ViewGallery>(false, "View Gallery", true);
        }

        private void RemoveAllTemporaryCameras()
        {
            if (!EditorUtility.DisplayDialog(
                    "Confirmation",
                    "Are you sure you want to remove all temporary cameras?",
                    "Remove", "Cancel")) return;

            var tempCameras = cameras.Select(c => c.camera)
                .Where(c => c.GetComponentInParent<TemporaryContainer>() != null).ToArray();

            for (var i = 0; i < tempCameras.Length; i++) DestroyImmediate(tempCameras[i].gameObject);
            isDirty = true;
        }

        private void RemoveAllViewStates()
        {
            if (!EditorUtility.DisplayDialog(
                    "Confirmation",
                    "Are you sure you want to remove all View States?",
                    "Remove", "Cancel")) return;

            var container = TemporaryContainer.GetContainer();
            if (container == null) return;

            var viewStates = container.GetComponentsInChildren<ViewState>();
            for (var i = 0; i < viewStates.Length; i++) DestroyImmediate(viewStates[i].gameObject);

            isDirty = true;
        }

        private static void RemoveTemporaryCamera(object obj)
        {
            var go = (obj as Camera).gameObject;

            if (!EditorUtility.DisplayDialog(
                    "Confirmation",
                    "Are you sure you want to remove " + go.name + " camera?",
                    "Remove", "Cancel")) return;

            DestroyImmediate(go);
            isDirty = true;
        }

        private static void RemoveViewState(object userdata)
        {
            var item = userdata as ViewStateItem;

            if (!EditorUtility.DisplayDialog(
                    "Confirmation",
                    "Are you sure you want to remove " + item.title + "?",
                    "Remove", "Cancel")) return;

            var go = item.viewState.gameObject;
            if (go.tag == "EditorOnly" && go.GetComponents<Component>().Length == 2) DestroyImmediate(go);
            else DestroyImmediate(item.viewState);
            isDirty = true;
        }

        private static void RenameViewState(object userdata)
        {
            var item = userdata as ViewStateItem;
            InputDialog.Show("Rename View State", item.title,
                delegate(string s) { item.viewState.title = item.title = s; });
        }

        private void RenderItems()
        {
            if (itemWidth <= 0 || itemHeight <= 0) return;

            var renderTexture = new RenderTexture((int)itemWidth, (int)itemHeight, 16, RenderTextureFormat.ARGB32);
            RenderTexture.active = renderTexture;
            RenderTexture lastAT = null;
            var clearFlags = CameraClearFlags.Skybox;

            if (cameras != null)
                for (var i = 0; i < cameras.Length; i++)
                {
                    var camera = cameras[i];
                    if (camera.texture != null) DestroyImmediate(camera.texture);
                }

            var canvases = FindObjectsOfType<Canvas>();
            var modifiedCanvases = new List<Canvas>();

            try
            {
                foreach (var canvas in canvases)
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        modifiedCanvases.Add(canvas);
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    }

                for (var i = 0; i < cameras.Length; i++)
                {
                    var cameraState = cameras[i];
                    if (cameraState == null || cameraState.camera == null) continue;
                    Camera camera = null;
                    try
                    {
                        camera = cameraState.camera;

                        clearFlags = camera.clearFlags;
                        if (clearFlags == CameraClearFlags.Depth || clearFlags == CameraClearFlags.Nothing)
                            camera.clearFlags = CameraClearFlags.Skybox;
                        lastAT = camera.targetTexture;
                        camera.targetTexture = renderTexture;
                        camera.Render();

                        var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24,
                            false);
                        texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                        texture.Apply();
                        cameraState.texture = texture;
                    }
                    catch (Exception e)
                    {
                        Log.Add(e);
                    }

                    camera.targetTexture = lastAT;
                    camera.clearFlags = clearFlags;
                }

                for (var i = 0; i < views.Length; i++)
                {
                    var item = views[i];
                    if (item.texture != null) DestroyImmediate(item.texture);
                }

                var sceneView = views[0].view;
                var cam = sceneView.camera;
                lastAT = cam.activeTexture;
                clearFlags = cam.clearFlags;
                var nearClipPlane = cam.nearClipPlane;
                cam.nearClipPlane = 0.01f;
                var activeClearFlags = cam.clearFlags;
                var camBackgroundColor = cam.backgroundColor;
                if (clearFlags == CameraClearFlags.Depth || clearFlags == CameraClearFlags.Nothing)
                    cam.clearFlags = activeClearFlags = CameraClearFlags.Skybox;
                cam.targetTexture = renderTexture;

                for (var i = 0; i < views.Length; i++)
                {
                    var item = views[i];
                    item.SetView(sceneView);

                    if (item.renderUI)
                    {
                        var canvasCamera = cam;
                        cam.clearFlags = CameraClearFlags.Color;
                        cam.backgroundColor = Color.gray;

                        for (var j = 0; j < modifiedCanvases.Count; j++)
                        {
                            var canvas = modifiedCanvases[j];
                            canvas.worldCamera = canvasCamera;
                            canvas.planeDistance = cam.nearClipPlane * 1.1f;
                            canvas.scaleFactor = renderTexture.width / sceneView.position.width;
                        }
                    }
                    else
                    {
                        cam.clearFlags = activeClearFlags;
                        cam.backgroundColor = camBackgroundColor;
                        for (var j = 0; j < modifiedCanvases.Count; j++) modifiedCanvases[j].worldCamera = null;
                    }

                    cam.Render();

                    var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                    texture.Apply();
                    item.texture = texture;
                }

                views[0].SetView(sceneView);

                cam.targetTexture = lastAT;
                cam.clearFlags = clearFlags;
                cam.nearClipPlane = nearClipPlane;

                RenderTexture.active = null;
                renderTexture.Release();
                DestroyImmediate(renderTexture);
            }
            catch
            {
            }

            foreach (var canvas in modifiedCanvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                canvas.scaleFactor = 1;
            }
        }

        private static void RestoreViewState(object userdata)
        {
            var viewItem = userdata as ViewStateItem;
            var sceneView = SceneView.lastActiveSceneView;
            sceneView.in2DMode = viewItem.is2D;
            sceneView.pivot = viewItem.pivot;
            sceneView.size = viewItem.size;

            if (!viewItem.is2D)
            {
                sceneView.rotation = viewItem.rotation;
                sceneView.camera.fieldOfView = 60;
            }

            GetWindow<SceneView>();
        }

        private void TryCloseDelayed()
        {
            if (closeOnSelect) EditorApplication.delayCall += Close;
        }

        private void UpdateFilteredItems()
        {
            if (string.IsNullOrEmpty(_filter))
            {
                filteredItems = null;
                CalcItemSize();
                RenderItems();
                return;
            }

            var pattern = SearchableItem.GetPattern(_filter);

            filteredItems = cameras.Select(c => c as ViewItem).Concat(views).Where(i => i.UpdateAccuracy(pattern) > 0)
                .OrderByDescending(i => i.accuracy).ToArray();

            CalcItemSize();
            RenderItems();
        }
    }
}