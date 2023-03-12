/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using InfinityCode.UltimateEditorEnhancer.UnityTypes;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.SceneTools
{
    [InitializeOnLoad]
    public static class TerrainBrushSize
    {
        private static bool isActive;
        private static Terrain terrain;

        static TerrainBrushSize()
        {
            try
            {
                Selection.selectionChanged += OnSelectionChanged;
                SceneViewManager.AddListener(OnSceneGUI);

                OnSelectionChanged();
            }
            catch (Exception e)
            {
                Log.Add(e);
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!isActive || terrain == null) return;
            if (Preview.isActive) return;

            var e = Event.current;
            if (e.type != EventType.ScrollWheel || (e.modifiers != Prefs.terrainBrushSizeModifiers &&
                                                    e.modifiers != Prefs.terrainBrushSizeBoostModifiers)) return;

            var editor = TerrainInspectorRef.GetActiveTerrainInspectorInstance();
            if (editor == null) return;

            var value = TerrainInspectorRef.GetSelectedTool(editor);

            float size;
            var instance = PaintTreesToolRef.GetInstance();
            if (value == 2) size = PaintTreesToolRef.GetBrushSize(instance);
            else
                size = TerrainInspectorRef.GetBrushSize(editor);

            var delta = e.delta.y;
            if (e.modifiers == Prefs.terrainBrushSizeBoostModifiers) delta *= 10;
            size = Mathf.Clamp(size + delta, 0.1f,
                Mathf.Round(Mathf.Min(terrain.terrainData.size.x, terrain.terrainData.size.z) * 15f / 16f));

            if (value == 2) PaintTreesToolRef.SetBrushSize(instance, size);
            else TerrainInspectorRef.SetBrushSize(editor, size);

            e.Use();
        }

        private static void OnSelectionChanged()
        {
            if (!Prefs.terrainBrushSize) return;

            isActive = false;
            terrain = null;

            if (Selection.activeGameObject == null) return;
            var go = Selection.activeGameObject;
            terrain = go.GetComponent<Terrain>();
            if (terrain == null) return;

            isActive = true;
        }
    }
}