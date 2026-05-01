using System.Collections.Generic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    public static class AnimationFrameDiscovery
    {
        private static readonly Dictionary<string, List<Texture2D>> _cache =
            new Dictionary<string, List<Texture2D>>();
        
        public static int GetFrameCount(EditorState state)
        {
            if (string.IsNullOrEmpty(state.SelectedDefName)) return 0;

            // 1. If an animation is active, fall through to atlas detection below.
            // (ActiveAnimation is typed as Def; access duration via atlas frame count instead.)

            // 2. Check the primary texture for Atlas slicing (Fireballs)
            var layer = state.ActiveLayer;
            if (layer?.CachedTexture != null)
            {
                int texWidth = layer.CachedTexture.width;
                int texHeight = layer.CachedTexture.height;
                if (texWidth > texHeight) return texWidth / texHeight;
            }

            return 0;
        }
        
        public static List<Texture2D> GetFrames(string defName)
        {
            if (string.IsNullOrEmpty(defName)) return null;
            
            // Check cache, but ensure the list isn't empty (null check handles the 'not found' state)
            if (_cache.TryGetValue(defName, out var cached)) return cached;

            var def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            // Fix: Check graphicData for the path if the instantiated graphic is null
            string path = def?.graphic?.path ?? def?.graphicData?.texPath;
            if (path == null) return null;

            var frames = new List<Texture2D>();

            // Naming convention: _0, _1, _2...
            for (int i = 0; i < 100; i++)
            {
                var tex = ContentFinder<Texture2D>.Get($"{path}_{i}", false);
                if (tex == null) break;
                frames.Add(tex);
            }

            // Fallback: plain 0, 1, 2...
            if (frames.Count == 0)
            {
                for (int i = 0; i < 100; i++)
                {
                    var tex = ContentFinder<Texture2D>.Get($"{path}{i}", false);
                    if (tex == null) break;
                    frames.Add(tex);
                }
            }

            // Always cache the result (even if empty) to prevent redundant lookups every frame
            _cache[defName] = frames.Count > 0 ? frames : null;
            return _cache[defName];
        }

        // --- THE SURGICAL ADDITION ---
        public static void ClearCacheFor(string defName)
        {
            if (_cache.ContainsKey(defName)) _cache.Remove(defName);
        }

        public static void InvalidateCache() => _cache.Clear();
    }
}