#pragma warning disable CS8600, CS8604
using System;
using System.Collections.Generic;
using System.Linq;
using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using RimWorld;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    public class DefListPanel
    {
        private string _search = "";
        private Vector2 _scroll = Vector2.zero;
        private List<Def> _cachedDefs = null!;
        private RimWorldDefType _browsingType = RimWorldDefType.ThingDef;

        public void Draw(Rect rect, EditorState currentState, Action<string> pushUndo)
        {
            Widgets.DrawMenuSection(rect);
            var inner = rect.ContractedBy(4f);

            // 1. Browser Header: Type Selector
            Rect typeBtnRect = new Rect(inner.x, inner.y, inner.width, 24f);
            if (Widgets.ButtonText(typeBtnRect, $"Browsing: {_browsingType}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (RimWorldDefType type in Enum.GetValues(typeof(RimWorldDefType)))
                {
                    options.Add(new FloatMenuOption(type.ToString(), () => {
                        _browsingType = type;
                        _cachedDefs = LoadDefsForType(_browsingType);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // 2. Search Bar
            var searchRect = new Rect(inner.x, typeBtnRect.yMax + 4f, inner.width, 24f);
            _search = Widgets.TextField(searchRect, _search);

            // 3. The List
            if (_cachedDefs == null) _cachedDefs = LoadDefsForType(_browsingType);

            var listRect = new Rect(inner.x, searchRect.yMax + 4f, inner.width, inner.height - (searchRect.yMax - inner.y + 4f));
            var filtered = string.IsNullOrWhiteSpace(_search)
                ? _cachedDefs
                : _cachedDefs.Where(d => d.defName.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            float rowH = 22f;
            var viewRect = new Rect(0, 0, listRect.width - 16f, filtered.Count * rowH);
            Widgets.BeginScrollView(listRect, ref _scroll, viewRect);

            for (int i = 0; i < filtered.Count; i++)
            {
                var def = filtered[i];
                var rowRect = new Rect(0, i * rowH, viewRect.width, rowH);
                
                if (currentState != null && currentState.SelectedObject?.DefName == def.defName) 
                    Widgets.DrawHighlight(rowRect);
                
                Widgets.Label(rowRect, def.defName);

                if (Widgets.ButtonInvisible(rowRect))
                {
                    // ── Routing rules ──────────────────────────────────────────
                    // PawnKindDef  → always a new scene object (composited layers)
                    // FleckDef     → always a new scene object (graphicData is
                    //                 unreliable on pawn layers; folder paths,
                    //                 Graphic_Flicker, null texPaths all crash)
                    // EffecterDef  → always a new scene object (same reasons)
                    // ThingDef     → assign texPath to the active layer when one
                    //                exists, otherwise open as a new scene object

                    bool alwaysNewScene = def is PawnKindDef
                                      || def is FleckDef
                                      || def is EffecterDef;

                    bool canAssignToLayer = !alwaysNewScene
                        && currentState?.SelectedObject?.ActiveLayer != null;

                    if (canAssignToLayer)
                    {
                        // ThingDef only — assign its texPath to the active layer.
                        var layer   = currentState.SelectedObject.ActiveLayer;
                        var thingDef = (ThingDef)def;

                        // Use only graphicData.texPath — never access ThingDef.graphic
                        // at runtime; it's lazily initialised and can throw outside
                        // the normal render pipeline.
                        string newPath = thingDef.graphicData?.texPath ?? "";

                        layer.TexturePath   = newPath;
                        layer.Name          = def.label ?? def.defName ?? "Base";
                        layer.IsDirectional = thingDef.graphicData?.graphicClass != null
                            && thingDef.graphicData.graphicClass.Name.Contains("Multi");
                        layer.CachedTexture = string.IsNullOrEmpty(newPath)
                            ? null
                            : ContentFinder<Texture2D>.Get(newPath, false);

                        string displayName = def.label ?? def.defName;
                        Messages.Message($"Assigned '{displayName}' → layer '{layer.Name}'",
                            MessageTypeDefOf.SilentInput, false);
                        pushUndo("Assign Texture to Layer");
                    }
                    else
                    {
                        // PawnKindDef / FleckDef / EffecterDef, or no active layer
                        // — open as its own scene object in a new studio tab.
                        HandleDefSelection(def, _browsingType, pushUndo);
                    }
                }
            }
            Widgets.EndScrollView();
        }

        /// <summary>
        /// Safely extracts a usable texture path from a FleckDef.
        /// Returns an empty string rather than throwing when graphicData is null,
        /// texPath is null, or the path points to an animated folder.
        /// </summary>
        private static string SafeFleckPath(FleckDef fleck)
        {
            try
            {
                string path = fleck?.graphicData?.texPath ?? "";
                if (string.IsNullOrWhiteSpace(path)) return "";

                // If the path resolves directly as a texture, use it.
                if (ContentFinder<UnityEngine.Texture2D>.Get(path, false) != null)
                    return path;

                // Animated / random graphics store a folder path — try appending
                // common frame suffixes to find the first usable frame.
                string[] frameSuffixes = { "_0", "_1", "_a", "" };
                foreach (var suffix in frameSuffixes)
                {
                    string candidate = path + suffix;
                    if (ContentFinder<UnityEngine.Texture2D>.Get(candidate, false) != null)
                        return candidate;
                }

                // Return the raw path anyway — PreviewPanel will show the
                // missing-texture checker so it's visible but won't crash.
                return path;
            }
            catch
            {
                return "";
            }
        }

        private List<Def> LoadDefsForType(RimWorldDefType type)
        {
            if (type == RimWorldDefType.FleckDef)
                return DefDatabase<FleckDef>.AllDefs.Cast<Def>().OrderBy(d => d.defName).ToList();
            if (type == RimWorldDefType.EffecterDef)
                return DefDatabase<EffecterDef>.AllDefs.Cast<Def>().OrderBy(d => d.defName).ToList();
            if (type == RimWorldDefType.PawnKindDef)
                return DefDatabase<PawnKindDef>.AllDefs.Cast<Def>().OrderBy(d => d.defName).ToList();
            
            return DefDatabase<ThingDef>.AllDefs.Where(d => d.graphic != null).Cast<Def>().OrderBy(d => d.defName).ToList();
        }

        private void HandleDefSelection(Def def, RimWorldDefType currentCategory, Action<string> pushUndo)
        {
            // Tab check: Don't open if already open
            int existingIndex = MainEditorWindow.OpenStates.FindIndex(s => s.ActiveScene.Any(o => o.DefName == def.defName));
            if (existingIndex >= 0)
            {
                MainEditorWindow.ActiveTabIndex = existingIndex;
                return;
            }
            SceneObject sceneObj = null;
            if (def is PawnKindDef pk) 
            {
                sceneObj = LayerFactory.CreateSceneObjectFromPawn(pk);
            }
            else if (def is ThingDef t) 
            {
                sceneObj = LayerFactory.CreateSceneObjectFromDef(t);
            }
            else
            {
                // FleckDef / EffecterDef — build a minimal scene object.
                sceneObj = new SceneObject { DefName = def.defName, DefType = currentCategory };

                if (def is FleckDef fleck)
                {
                    string path = SafeFleckPath(fleck);
                    sceneObj.Layers.Add(new LayerData
                    {
                        TexturePath   = path,
                        Name          = def.label ?? def.defName ?? "Base",
                        IsDirectional = false,
                        Visible       = true,
                        // Flecks are often animated strips; don't tint by default.
                        Color         = UnityEngine.Color.white,
                        Opacity       = 1f,
                    });

                    // Tag Flicker capability if the graphicClass is Graphic_Flicker.
                    if (fleck.graphicData?.graphicClass == typeof(Graphic_Flicker))
                        sceneObj.AnimCaps |= D4egon.RimRimModelEditor.Model.AnimCapability.Flicker;
                }
                // EffecterDefs have no single texture — add a blank placeholder layer
                // so the user can see the object exists and add textures manually.
                else
                {
                    sceneObj.Layers.Add(new LayerData
                    {
                        Name    = "Base",
                        Visible = true,
                        Color   = UnityEngine.Color.white,
                        Opacity = 1f,
                    });
                }
            }

            var newState = new EditorState();
            newState.AddObjectToScene(sceneObj);

            MainEditorWindow.OpenStates.Add(newState);
            MainEditorWindow.ActiveTabIndex = MainEditorWindow.OpenStates.Count - 1;
            pushUndo("Open " + def.defName);
        }
    }
}