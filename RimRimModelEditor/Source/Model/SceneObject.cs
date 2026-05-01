#pragma warning disable CS8600, CS8604, CS8618
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.Model
{
    public enum RimWorldDefType { ThingDef, FleckDef, EffecterDef, HediffDef, PawnKindDef }
    public enum AnimationPreset { Idle, Aiming, Firing, MeleeSwing, Flickering, Pulsing }

    /// <summary>
    /// Bit-flags describing which animation capabilities a scene object has.
    /// Tagged at object-creation time by LayerFactory / DefListPanel.
    /// </summary>
    [Flags]
    public enum AnimCapability
    {
        None    = 0,
        Walk    = 1,    // pawn — supports directional walk bob
        Fire    = 2,    // weapon with projectile verb — muzzle flash + burst
        Flicker = 4,    // Graphic_Flicker graphicClass — frame-cycling
        Power   = 8,    // has CompProperties_Power — power-on / power-off state
    }

    public class SceneObject
    {
        public string DefName = "";
        public RimWorldDefType DefType = RimWorldDefType.ThingDef;
        public List<LayerData> Layers = new List<LayerData>();
        public int ActiveLayerIndex = 0;

        public Vector2 RootPosition = Vector2.zero;
        public float RootRotation = 0f;
        public Vector2 RootScale = Vector2.one;

        public string BaseTemplate = "";
        public List<string> SelectedComps = new List<string>();

        /// <summary>Animation capabilities detected at load time (Walk, Fire, Flicker, Power).</summary>
        public AnimCapability AnimCaps = AnimCapability.None;
        
        public float WeaponRange = 1.42f;
        public float WarmupTime = 0f;
        public int BurstShotCount = 1;
        public string ProjectileDef = "Bullet_Small";
        public float EquippedAngleOffset = 0f;
        public float MuzzleFlashScale = 1f;

        public float FadeInTime = 0.1f;
        public float SolidTime = 1f;
        public float FadeOutTime = 0.5f;
        public float GrowthRate = 0f;
        public Vector3 Acceleration = Vector3.zero;

        public LayerData ActiveLayer =>
            (Layers.Count > 0 && ActiveLayerIndex >= 0 && ActiveLayerIndex < Layers.Count)
                ? Layers[ActiveLayerIndex] : null;

        public SceneObject Clone()
        {
            return new SceneObject
            {
                DefName = DefName,
                DefType = DefType,
                Layers = Layers.Select(l => l.Clone()).ToList(),
                ActiveLayerIndex = ActiveLayerIndex,
                RootPosition = RootPosition,
                RootRotation = RootRotation,
                RootScale = RootScale,
                BaseTemplate = BaseTemplate,
                SelectedComps = new List<string>(SelectedComps),
                AnimCaps = AnimCaps,
                WeaponRange = WeaponRange,
                WarmupTime = WarmupTime,
                BurstShotCount = BurstShotCount,
                ProjectileDef = ProjectileDef,
                EquippedAngleOffset = EquippedAngleOffset,
                MuzzleFlashScale = MuzzleFlashScale,
                FadeInTime = FadeInTime,
                SolidTime = SolidTime,
                FadeOutTime = FadeOutTime,
                GrowthRate = GrowthRate,
                Acceleration = Acceleration
            };
        }
    }
}