#pragma warning disable CS8600, CS8604,
using UnityEngine;

namespace D4egon.RimRimModelEditor.Model
{
    public class LayerData
    {
        public string TexturePath = "";
        public Vector2 Position = Vector2.zero;
        public Vector2 Scale = Vector2.one;
        public float Rotation = 0f;
        public Color Color = Color.white;
        public float Opacity = 1f;
        public bool Visible = true;
        public bool Locked = false;
        public string Name = "Layer";

        // ── Extended fields ─────────────────────────────────────────────────
        public bool IsDirectional = false;   // Resolves _north/_east/_south textures
        public int Priority = 0;             // Sort order within a SceneObject

        // ── Body-type variant ────────────────────────────────────────────────
        // For apparel that ships separate textures per body type (e.g. Duster):
        //   resolved path = TexturePath + "_" + BodyTypeVariant + "_south" etc.
        // Leave empty ("") for layers that have a single texture for all bodies.
        public string BodyTypeVariant = "";

        // Backward-compat alias used by PawnCompositor / PawnFactory
        [System.Xml.Serialization.XmlIgnore]
        public string Label
        {
            get => Name;
            set => Name = value;
        }

        [System.Xml.Serialization.XmlIgnore]
        public Texture2D CachedTexture = null;

        public LayerData Clone()
        {
            return new LayerData
            {
                TexturePath     = TexturePath,
                Position        = Position,
                Scale           = Scale,
                Rotation        = Rotation,
                Color           = Color,
                Opacity         = Opacity,
                Visible         = Visible,
                Locked          = Locked,
                Name            = Name,
                IsDirectional   = IsDirectional,
                Priority        = Priority,
                BodyTypeVariant = BodyTypeVariant,
                CachedTexture   = CachedTexture
            };
        }
    }
}
