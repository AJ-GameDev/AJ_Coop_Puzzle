/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public static class Resources
    {
        private static string _assetFolder;
        private static string _iconsFolder;
        private static string _settingsFolder;

        public static string assetFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_assetFolder))
                {
                    var paths = Directory.GetFiles(Application.dataPath, "Ultimate Editor Enhancer.asmdef",
                        SearchOption.AllDirectories);
                    if (paths.Length != 0)
                    {
                        var info = new FileInfo(paths[0]);
                        _assetFolder = info.Directory.Parent.FullName.Substring(Application.dataPath.Length - 6) + "/";
                    }
                    else
                    {
                        _assetFolder = "Assets/Plugins/Infinity Code/Ultimate Editor Enhancer/";
                    }
                }

                return _assetFolder;
            }
        }

        public static string iconsFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_iconsFolder)) _iconsFolder = assetFolder + "Icons/";

                return _iconsFolder;
            }
        }

        public static string settingsFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_settingsFolder))
                {
                    var info = new DirectoryInfo(assetFolder);
                    _settingsFolder = info.Parent.FullName.Substring(Application.dataPath.Length - 6) +
                                      "/Ultimate Editor Enhancer Settings/";
                }

                return _settingsFolder;
            }
        }

        public static Texture2D CreateSinglePixelTexture(byte r, byte g, byte b, byte a)
        {
            return CreateSinglePixelTexture(new Color32(r, g, b, a));
        }

        public static Texture2D CreateSinglePixelTexture(float v, float a = 1)
        {
            return CreateSinglePixelTexture(v, v, v, a);
        }

        public static Texture2D CreateSinglePixelTexture(float r, float g, float b, float a)
        {
            return CreateSinglePixelTexture(new Color(r, g, b, a));
        }

        public static Texture2D CreateSinglePixelTexture(Color color)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, color);
            t.Apply();
            return t;
        }

        public static Texture2D CreateTexture(int width, int height, Color color)
        {
            var t = new Texture2D(width, height);
            var colors = new Color[width * height];
            for (var i = 0; i < colors.Length; i++) colors[i] = color;
            t.SetPixels(colors);
            t.Apply();
            return t;
        }

        public static Texture2D DuplicateTexture(Texture source)
        {
            var rt = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(source, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(source.width, source.height);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return texture;
        }

        public static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(assetFolder + path);
        }

        public static Texture2D LoadIcon(string path, string ext = ".png")
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(iconsFolder + path + ext);
        }

        public static void Unload(Object asset)
        {
            if (asset != null) UnityEngine.Resources.UnloadAsset(asset);
        }
    }
}