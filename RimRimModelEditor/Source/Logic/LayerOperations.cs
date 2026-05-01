#pragma warning disable CS8600, CS8604
using System.IO;
using System.Reflection;
using UnityEngine;
using D4egon.RimRimModelEditor.Model;

namespace D4egon.RimRimModelEditor.Logic
{
    public static class LayerOperations
    {
        private static readonly MethodInfo LoadImageMethod = typeof(Texture2D).Assembly
            .GetType("UnityEngine.ImageConversion")?
            .GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });

        public static void CopySettings(LayerData source, LayerData target)
        {
            target.Scale      = source.Scale;
            target.Position   = source.Position;
            target.Rotation   = source.Rotation;
            target.Color      = source.Color;
            target.Opacity    = source.Opacity;
            target.IsDirectional = source.IsDirectional;
        }

        /// <summary>Loads an image from disk into a layer's CachedTexture.</summary>
        public static void LoadExternalTexture(LayerData layer, string fullPath)
        {
            if (!File.Exists(fullPath)) return;
            var tex = LoadFileToTexture(fullPath);
            if (tex == null) return;
            layer.CachedTexture = tex;
            layer.TexturePath   = fullPath;
            layer.Color         = Color.white;
            layer.Opacity       = 1f;
        }

        /// <summary>Loads an image from disk into EditorState.BackgroundTexture.</summary>
        public static void LoadExternalTexture(EditorState state, string fullPath)
        {
            if (!File.Exists(fullPath)) return;
            var tex = LoadFileToTexture(fullPath);
            if (tex != null) state.BackgroundTexture = tex;
        }

        private static Texture2D LoadFileToTexture(string fullPath)
        {
            byte[] data = File.ReadAllBytes(fullPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            LoadImageMethod?.Invoke(null, new object[] { tex, data });
            return tex;
        }
    }
}
