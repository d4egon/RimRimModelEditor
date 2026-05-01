using System;
using System.Collections.Generic;
using System.Linq;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;
using RimWorld;

namespace D4egon.RimRimModelEditor.Logic
{
    public static class LayerFactory
    {
        public static SceneObject CreateSceneObjectFromDef(ThingDef def)
        {
            if (def == null) return null;

            SceneObject obj = new SceneObject
            {
                DefName = def.defName,
                DefType = RimWorldDefType.ThingDef,
                BaseTemplate = def.graphicData?.texPath ?? ""
            };

            if (def.graphicData != null)
            {
                AddLayer(obj, "Base", def.graphicData.texPath, 0, def.graphicData.drawSize);
                obj.RootScale = def.graphicData.drawSize;
            }

            if (def.Verbs != null && def.Verbs.Count > 0)
            {
                var verb = def.Verbs[0];
                if (verb.muzzleFlashScale > 0)
                {
                    AddLayer(obj, "Muzzle Flash", "Things/Mote/MuzzleFlash", 50,
                             new Vector2(verb.muzzleFlashScale, verb.muzzleFlashScale));
                }
            }

            // ── Tag animation capabilities ────────────────────────────────────
            obj.AnimCaps = AnimCapability.None;

            // Fire: has any verb that launches a projectile
            if (def.Verbs != null)
                foreach (var v in def.Verbs)
                    if (v.defaultProjectile != null) { obj.AnimCaps |= AnimCapability.Fire; break; }

            // Flicker: graphicClass is Graphic_Flicker
            if (def.graphicData?.graphicClass == typeof(Graphic_Flicker))
                obj.AnimCaps |= AnimCapability.Flicker;

            // Power: has a CompProperties_Power comp
            if (def.comps != null)
                foreach (var c in def.comps)
                    if (c is CompProperties_Power) { obj.AnimCaps |= AnimCapability.Power; break; }

            return obj;
        }

        public static SceneObject CreateSceneObjectFromPawn(PawnKindDef kind)
        {
            if (kind == null) return null;

            var obj = new SceneObject
            {
                DefName   = kind.defName,
                DefType   = RimWorldDefType.PawnKindDef,
                AnimCaps  = AnimCapability.Walk,   // all pawns can walk
            };

            bool isHumanlike = kind.race?.race?.Humanlike == true;

            if (isHumanlike)
            {
                // Humanlike pawns use a composited body + head layer system.
                // Try to pull a more specific body texPath from the race def first.
                // NOTE: only assign to BodyPath — never HeadPath — even if the race
                //       graphicData happens to point at a body texture.
                var appearance = new PawnAppearanceData();
                string racePath = kind.race?.graphicData?.texPath;
                if (!string.IsNullOrEmpty(racePath)
                    && racePath.IndexOf("Bodies", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    appearance.BodyPath = racePath;

                PawnCompositor.RebuildPawnLayers(obj, appearance);
            }
            else
            {
                // Animal / creature — resolve from life-stage or race graphic.
                string resolvedPath = null;
                var lifeStage = kind.lifeStages?.LastOrDefault();
                if (lifeStage?.bodyGraphicData != null)
                    resolvedPath = lifeStage.bodyGraphicData.texPath;
                else if (!string.IsNullOrEmpty(kind.race?.graphicData?.texPath))
                    resolvedPath = kind.race.graphicData.texPath;

                if (!string.IsNullOrEmpty(resolvedPath))
                    AddLayer(obj, "Base", resolvedPath, 0, Vector2.one, true);
                else
                    // Last resort — a known-good humanlike body so something renders.
                    AddLayer(obj, "Base", "Things/Pawn/Humanlike/Bodies/Naked_Thin", 0, Vector2.one, true);
            }

            return obj;
        }

        public static void PopulateLayers(EditorState state, ThingDef def)
        {
            var obj = state.SelectedObject;
            if (obj == null || def == null) return;

            obj.Layers.Clear();
            obj.EquippedAngleOffset = def.equippedAngleOffset;

            if (def.graphicData != null)
            {
                string path = def.graphicData.texPath;

                // If it's a random graphic, the texPath is a folder — append "_a" to resolve the first variant.
                if (def.graphicData.graphicClass == typeof(Graphic_Random))
                    path += "_a";

                bool isDirectional = def.graphicData.graphicClass == typeof(Graphic_Multi) ||
                                     def.graphicData.graphicClass == typeof(Graphic_Flicker);

                AddLayer(obj, "Base", path, 0, def.graphicData.drawSize, isDirectional);
                obj.RootScale = def.graphicData.drawSize;
            }
            

            foreach (var comp in def.comps)
            {
                if (comp is CompProperties_FireOverlay || comp.GetType().Name.Contains("Flicker"))
                {
                    AddLayer(obj, "Overlay", "Things/Special/Fire", 100, Vector2.one, true);
                }
            }
        }

        private static void AddLayer(SceneObject obj, string name, string path, int priority, 
                                     Vector2? scale = null, bool isDirectional = false, Vector2? offset = null)
        {
            // SAFETY: If path is null, RimWorld UI will crash.
            if (string.IsNullOrEmpty(path)) return;

            obj.Layers.Add(new LayerData
            {
                Name = name ?? "Unknown Layer",
                TexturePath = path,
                Priority = priority,
                Scale = scale ?? Vector2.one,
                Position = offset ?? Vector2.zero,
                Visible = true,
                IsDirectional = isDirectional,
                Opacity = 1f,
                Color = Color.white
            });

            obj.Layers.Sort((Comparison<LayerData>)((a, b) => a.Priority.CompareTo(b.Priority)));
        }
    }
}