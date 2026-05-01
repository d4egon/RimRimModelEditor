#pragma warning disable CS8600, CS8604, CS8618
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.Model
{
    // [CS0101 Fix]: RimWorldDefType enum removed from here. 
    // It must only exist in Model/SceneObject.cs or Model/EditorState.cs.

    [XmlRoot("TweakPreset")]
    public class TweakPreset
    {
        [XmlAttribute] public string Name = "Unnamed Preset";
        [XmlAttribute] public string DefName = "";
        [XmlAttribute] public RimWorldDefType DefType = RimWorldDefType.ThingDef;
        [XmlAttribute] public string CreatedAt = "";

        // Common Core
        public string BaseTemplate = "";
        public List<string> Comps = new List<string>();

        // Verb Data
        public float WeaponRange = 1.42f;
        public float WarmupTime = 0f;
        public int BurstShotCount = 1;
        public string ProjectileDef = "Bullet_Small";
        public float EquippedAngleOffset = 0f;

        // Fleck Data
        public float GrowthRate = 0f;

        // Visual Data
        public float DrawSizeX = 1f;
        public float DrawSizeY = 1f;
        public float DrawOffsetX = 0f;
        public float DrawOffsetY = 0f;
        public float GlobalRotation = 0f;

        [XmlArray("Layers")]
        [XmlArrayItem("Layer")]
        public List<PresetLayer> Layers = new List<PresetLayer>();

        public static TweakPreset FromSceneObject(string presetName, SceneObject obj)
        {
            var preset = new TweakPreset
            {
                Name = presetName,
                DefName = obj.DefName,
                DefType = obj.DefType,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                
                BaseTemplate = obj.BaseTemplate,
                EquippedAngleOffset = obj.EquippedAngleOffset,
                GrowthRate = obj.GrowthRate,

                WeaponRange = obj.WeaponRange,
                WarmupTime = obj.WarmupTime,
                BurstShotCount = obj.BurstShotCount,
                ProjectileDef = obj.ProjectileDef,

                DrawSizeX = obj.RootScale.x,
                DrawSizeY = obj.RootScale.y,
                DrawOffsetX = obj.RootPosition.x,
                DrawOffsetY = obj.RootPosition.y,
                GlobalRotation = obj.RootRotation
            };

            foreach (var l in obj.Layers)
            {
                preset.Layers.Add(new PresetLayer {
                    Name = l.Name, TexturePath = l.TexturePath,
                    PosX = l.Position.x, PosY = l.Position.y,
                    ScaleX = l.Scale.x, ScaleY = l.Scale.y,
                    Rotation = l.Rotation, Opacity = l.Opacity,
                    ColorR = l.Color.r, ColorG = l.Color.g, ColorB = l.Color.b,
                    Visible = l.Visible, Locked = l.Locked
                });
            }
            return preset;
        }

        public SceneObject ToSceneObject()
        {
            var obj = new SceneObject
            {
                DefName = this.DefName,
                DefType = this.DefType,
                BaseTemplate = this.BaseTemplate,
                EquippedAngleOffset = this.EquippedAngleOffset,
                GrowthRate = this.GrowthRate,
                WeaponRange = this.WeaponRange,
                WarmupTime = this.WarmupTime,
                BurstShotCount = this.BurstShotCount,
                ProjectileDef = this.ProjectileDef,
                RootScale = new Vector2(DrawSizeX, DrawSizeY),
                RootPosition = new Vector2(DrawOffsetX, DrawOffsetY),
                RootRotation = GlobalRotation
            };

            foreach (var pl in Layers)
            {
                obj.Layers.Add(new LayerData {
                    Name = pl.Name, TexturePath = pl.TexturePath,
                    Position = new Vector2(pl.PosX, pl.PosY),
                    Scale = new Vector2(pl.ScaleX, pl.ScaleY),
                    Rotation = pl.Rotation,
                    Color = new Color(pl.ColorR, pl.ColorG, pl.ColorB, pl.Opacity),
                    Opacity = pl.Opacity, Visible = pl.Visible, Locked = pl.Locked
                });
            }
            return obj;
        }
    }

    public class PresetLayer
    {
        [XmlAttribute] public string Name = "Layer";
        public string TexturePath = "";
        public float PosX, PosY, ScaleX = 1f, ScaleY = 1f, Rotation;
        public float ColorR = 1f, ColorG = 1f, ColorB = 1f, Opacity = 1f;
        public bool Visible = true, Locked = false;
    }
}