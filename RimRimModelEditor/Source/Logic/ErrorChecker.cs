using System.Collections.Generic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    public static class ErrorChecker
    {
        public static List<string> Check(EditorState state)
        {
            var issues = new List<string>();

            if (state == null)
            {
                issues.Add("Editor state is null.");
                return issues;
            }

            if (string.IsNullOrEmpty(state.SelectedDefName) || state.SelectedDefName == "None")
            {
                issues.Add("No ThingDef selected.");
                return issues; // Critical stop
            }

            // 1. Def Existence Check
            var def = DefDatabase<ThingDef>.GetNamedSilentFail(state.SelectedDefName);
            if (def == null)
                issues.Add($"Def '{state.SelectedDefName}' is not loaded in the current game database.");

            // 2. Layer & Texture Validation
            if (state.Layers.Count == 0)
            {
                issues.Add("The model has no visual layers.");
            }
            else
            {
                for (int i = 0; i < state.Layers.Count; i++)
                {
                    var layer = state.Layers[i];
                    string layerLabel = string.IsNullOrEmpty(layer.Name) ? $"Layer {i}" : layer.Name;

                    if (string.IsNullOrEmpty(layer.TexturePath))
                    {
                        issues.Add($"{layerLabel}: Missing texture path.");
                    }
                    else if (layer.CachedTexture == null)
                    {
                        // Logic: If CachedTexture is null, it means ContentFinder failed 
                        // AND our manual FileWatcher hasn't loaded it yet.
                        issues.Add($"{layerLabel}: Texture '{layer.TexturePath}' could not be loaded.");
                    }
                }
            }

            // 3. Physical Dimensions
            if (state.DrawSize.x < 0.1f || state.DrawSize.y < 0.1f)
                issues.Add($"Draw Size ({state.DrawSize.x}x{state.DrawSize.y}) is too small for rendering.");

            if (state.CollisionSize.x < 0.1f || state.CollisionSize.y < 0.1f)
                issues.Add($"Collision Size ({state.CollisionSize.x}x{state.CollisionSize.y}) is too small for physics.");

            return issues;
        }
    }
}