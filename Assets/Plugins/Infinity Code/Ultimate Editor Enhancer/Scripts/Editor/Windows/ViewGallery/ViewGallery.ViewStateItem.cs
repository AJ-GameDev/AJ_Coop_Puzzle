/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.Windows
{
    public partial class ViewGallery
    {
        internal class ViewStateItem : ViewItem
        {
            public bool is2D;
            public Vector3 pivot;
            public bool renderUI = false;
            public Quaternion rotation;
            public float size;
            public string title;
            public SceneView view;
            public ViewState viewState;

            public ViewStateItem()
            {
            }

            public ViewStateItem(ViewState viewState)
            {
                this.viewState = viewState;
                pivot = viewState.pivot;
                size = viewState.size;
                rotation = viewState.rotation;
                title = viewState.title;
                is2D = viewState.is2D;
            }

            public override bool useInPreview
            {
                get
                {
                    if (viewState != null) return viewState.useInPreview;
                    return false;
                }
                set
                {
                    if (viewState != null) viewState.useInPreview = value;
                }
            }

            public override bool allowPreview => viewState != null;

            public override string name => title;

            public override void PrepareMenu(GenericMenuEx menu)
            {
                menu.Add("Restore", RestoreViewState, this);

                if (viewState != null)
                {
                    menu.Add("Rename", RenameViewState, this);
                    menu.Add("Delete", RemoveViewState, this);
                }
                else
                {
                    menu.AddDisabled("Delete");
                }

                if (viewState == null) menu.Add("Create View State", CreateViewState, this);
            }

            public void SetView(SceneView view)
            {
                var camera = view.camera;
                var t = camera.transform;

                if (!is2D)
                {
                    camera.orthographic = false;
                    camera.fieldOfView = 60;
                    t.position = pivot - rotation * Vector3.forward * ViewState.GetPerspectiveCameraDistance(size, 60);
                    t.rotation = rotation;
                }
                else
                {
                    camera.orthographic = true;
                    camera.orthographicSize = size;
                    t.position = pivot - Vector3.forward * size;
                    t.rotation = Quaternion.identity;
                }
            }

            public override void Set(SceneView view)
            {
                view.in2DMode = is2D;
                view.pivot = pivot;
                view.size = size;
                if (!is2D)
                {
                    view.rotation = rotation;
                    view.camera.fieldOfView = 60;
                }
            }
        }
    }
}