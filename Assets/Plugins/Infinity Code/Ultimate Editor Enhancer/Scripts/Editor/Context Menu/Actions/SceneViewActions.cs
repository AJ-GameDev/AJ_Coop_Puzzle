/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Linq;
using System.Text.RegularExpressions;
using InfinityCode.UltimateEditorEnhancer.Tools;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.EditorMenus.Actions
{
    public class SceneViewActions : ActionItem
    {
        protected override bool closeOnSelect => false;

        private void AlignViewToCamera(object userdata)
        {
            SceneViewHelper.AlignViewToCamera(userdata as Camera);
            EditorMenu.Close();
        }

        private void AlignViewToSelected()
        {
            SceneView.lastActiveSceneView.AlignViewToObject(targets[0].transform);
            EditorMenu.Close();
        }

        private void AlignWithView()
        {
            SceneView.lastActiveSceneView.AlignWithView();
            EditorMenu.Close();
        }

        private static Camera CreateCameraFromSceneView()
        {
            if (!EditorApplication.ExecuteMenuItem("GameObject/Camera")) return null;

            var sceneViewCamera = SceneView.lastActiveSceneView.camera;
            var camera = Selection.activeGameObject.GetComponent<Camera>();
            camera.transform.position = sceneViewCamera.transform.position;
            camera.transform.rotation = sceneViewCamera.transform.rotation;
            return camera;
        }

        [MenuItem(WindowsHelper.MenuPath + "Cameras/Create Permanent", false, 101)]
        private static void CreatePermanentCameraFromSceneView()
        {
            CreateCameraFromSceneView();
        }

        [MenuItem(WindowsHelper.MenuPath + "Cameras/Create Temporary", false, 101)]
        private static void CreateTemporaryCameraFromSceneView()
        {
            var container = TemporaryContainer.GetContainer();
            if (container == null) return;

            var pattern = @"Camera \((\d+)\)";

            var maxIndex = 1;
            var cameras = container.GetComponentsInChildren<Camera>();
            for (var i = 0; i < cameras.Length; i++)
            {
                var name = cameras[i].gameObject.name;
                var match = Regex.Match(name, pattern);
                if (match.Success)
                {
                    var strIndex = match.Groups[1].Value;
                    var index = int.Parse(strIndex);
                    if (index >= maxIndex) maxIndex = index + 1;
                }
            }

            var defaultName = "Camera (" + maxIndex + ")";
            InputDialog.Show("Enter name of Camera GameObject", defaultName, s =>
            {
                var camera = CreateCameraFromSceneView();
                if (camera == null) return;

                camera.gameObject.name = !string.IsNullOrEmpty(s) ? s : defaultName;
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, SceneView.lastActiveSceneView.size * 2);

                camera.transform.SetParent(container.transform, true);
                camera.tag = "EditorOnly";
            });
        }

        private void DeleteAllViewStates()
        {
            var views = Object.FindObjectsOfType<ViewState>();
            for (var i = 0; i < views.Length; i++) Object.DestroyImmediate(views[i].gameObject);
            EditorMenu.Close();
        }

        private void DeleteViewState(object userdata)
        {
            Object.DestroyImmediate((userdata as ViewState).gameObject);
            EditorMenu.Close();
        }

        private void FrameSelected()
        {
            SceneView.FrameLastActiveSceneView();
            EditorMenu.Close();
        }

        protected override void Init()
        {
            guiContent = new GUIContent(Icons.focus, "Views and Cameras");
        }

        private void InitCreateCameraFromViewMenu(GenericMenuEx menu)
        {
            menu.Add("Create Camera From View/Permanent", CreatePermanentCameraFromSceneView);
            menu.Add("Create Camera From View/Temporary", CreateTemporaryCameraFromSceneView);
        }

        private void InitAlignViewToCameraMenu(GenericMenuEx menu)
        {
            var cameras = Object.FindObjectsOfType<Camera>().OrderBy(c => c.name).ToArray();
            if (cameras.Length > 0)
                for (var i = 0; i < cameras.Length; i++)
                    menu.Add("Align View To Camera/" + cameras[i].gameObject.name, AlignViewToCamera, cameras[i]);
        }

        private void InitViewStatesMenu(GenericMenuEx menu)
        {
            menu.Add("View States/Gallery", ViewGallery.OpenWindow);
            menu.AddSeparator("View States/");
            menu.Add("View States/Create", SaveViewState);

            menu.Add("View States/Create For Selection", SelectionViewStates.AddToSelection);

            var views = Object.FindObjectsOfType<ViewState>().OrderBy(v => v.title).ToArray();
            if (views.Length > 0)
                for (var i = 0; i < views.Length; i++)
                {
                    menu.Add("View States/Restore/" + views[i].title, RestoreViewState, views[i]);

                    if (i == 0)
                    {
                        menu.Add("View States/Delete/All States", DeleteAllViewStates);
                        menu.AddSeparator("View States/Delete/");
                    }

                    menu.Add("View States/Delete/" + views[i].title, DeleteViewState, views[i]);
                }
        }

        public override void Invoke()
        {
            var menu = GenericMenuEx.Start();

            InitCreateCameraFromViewMenu(menu);
            InitAlignViewToCameraMenu(menu);
            InitViewStatesMenu(menu);

            if (targets != null && targets.Length > 0 && targets[0] != null)
            {
                menu.Add("Frame Selected", FrameSelected);
                menu.Add("Move To View", MoveToView);
                menu.Add("Align With View", AlignWithView);
                menu.Add("Align View To Selected", AlignViewToSelected);
            }

            menu.Show();
        }

        private void MoveToView()
        {
            SceneView.lastActiveSceneView.MoveToView();
            EditorMenu.Close();
        }

        private void RestoreViewState(object userdata)
        {
            var state = userdata as ViewState;
            var view = SceneView.lastActiveSceneView;
            view.in2DMode = state.is2D;
            view.pivot = state.pivot;
            view.size = state.size;
            if (!view.in2DMode) view.rotation = state.rotation;
            EditorMenu.Close();
        }

        [MenuItem(WindowsHelper.MenuPath + "View States/Create", false, 104)]
        public static void SaveViewState()
        {
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

                var view = SceneView.lastActiveSceneView;
                viewState.pivot = view.pivot;
                viewState.rotation = view.rotation;
                viewState.size = view.size;
                viewState.is2D = view.in2DMode;
                viewState.title = s;

                go.transform.SetParent(container.transform, true);
                EditorMenu.Close();
            });
        }
    }
}