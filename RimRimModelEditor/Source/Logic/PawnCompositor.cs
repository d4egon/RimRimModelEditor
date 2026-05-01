using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    public static class PawnCompositor
    {
        // Priority / Z-order — lower number = drawn first (underneath).
        // Matches RimWorld's actual draw order as closely as possible.
        private const int LAYER_BODY            = 0;
        private const int LAYER_APPAREL_BODY    = 1;   // pants, shirts, dusters, etc.
        private const int LAYER_HEAD            = 2;
        private const int LAYER_BEARD           = 3;   // rendered on the face (below hair)
        private const int LAYER_HAIR            = 4;
        private const int LAYER_HEAD_ATTACHMENT = 5;   // eye patches, horns, etc.
        private const int LAYER_HAT             = 6;
        private const int LAYER_WEAPON          = 7;

        // ── Y-OFFSET NOTE ───────────────────────────────────────────────────
        // The preview renders in GUI space where positive Y = DOWN the screen.
        // RimWorld's world-space has positive Y = UP.
        // To move the head ABOVE the body in the preview, use NEGATIVE Y.
        // Public so that LayerPanel can reuse the same offsets for new layers.
        public const float HEAD_Y_OFFSET    = -0.35f;   // above body centre
        public const float HAIR_Y_OFFSET    = -0.38f;   // just above head
        private const float WEAPON_X_OFFSET =  0.30f;   // right of body
        private const float WEAPON_Y_OFFSET =  0.00f;

        public static void RebuildPawnLayers(SceneObject pawn, PawnAppearanceData data)
        {
            pawn.Layers.Clear();

            // 1. BODY
            pawn.Layers.Add(new LayerData
            {
                Name = "Body",
                TexturePath = data.BodyPath,
                IsDirectional = true,
                Position = Vector2.zero,
                Scale = Vector2.one,
                Opacity = 1f,
                Color = data.SkinColor,
                Priority = LAYER_BODY,
                Visible = true
            });

            // 2. ON-SKIN APPAREL (e.g. slave bodystrap, undershirt)
            if (!data.ApparelPath.NullOrEmpty())
            {
                pawn.Layers.Add(new LayerData
                {
                    Name = "Apparel (Body)",
                    TexturePath = data.ApparelPath,
                    IsDirectional = true,
                    Position = Vector2.zero,
                    Scale = Vector2.one,
                    Opacity = 1f,
                    Color = Color.white,
                    Priority = LAYER_APPAREL_BODY,
                    Visible = true
                });
            }

            // 3. HEAD — negative Y to appear ABOVE the body in preview space.
            pawn.Layers.Add(new LayerData
            {
                Name = "Head",
                TexturePath = data.HeadPath,
                IsDirectional = true,
                Position = new Vector2(0f, HEAD_Y_OFFSET),
                Scale = Vector2.one,
                Opacity = 1f,
                Color = data.SkinColor,
                Priority = LAYER_HEAD,
                Visible = true
            });

            // 4. HAIR (optional — only added when a path is provided)
            if (!data.HairPath.NullOrEmpty())
            {
                pawn.Layers.Add(new LayerData
                {
                    Name = "Hair",
                    TexturePath = data.HairPath,
                    IsDirectional = true,
                    Position = new Vector2(0f, HAIR_Y_OFFSET),
                    Scale = Vector2.one,
                    Opacity = 1f,
                    Color = data.HairColor,
                    Priority = LAYER_HAIR,
                    Visible = true
                });
            }

            // 5. WEAPON (optional — right-hand side, no directional variants needed)
            if (!data.WeaponPath.NullOrEmpty())
            {
                pawn.Layers.Add(new LayerData
                {
                    Name = "Weapon",
                    TexturePath = data.WeaponPath,
                    IsDirectional = false,
                    Position = new Vector2(WEAPON_X_OFFSET, WEAPON_Y_OFFSET),
                    Scale = Vector2.one,
                    Rotation = pawn.EquippedAngleOffset,
                    Opacity = 1f,
                    Color = Color.white,
                    Priority = LAYER_WEAPON,
                    Visible = true
                });
            }
        }
    }

    public class PawnAppearanceData
    {
        // Body texture — resolved at construction time from PawnTextureScanner so
        // that the correct path for this RimWorld install is always used.
        public string BodyPath;

        // Head texture — likewise discovered at runtime; falls back to the
        // pre-1.5 conventional path if nothing is found in ContentFinder.
        public string HeadPath;

        public string HairPath    = "";
        public string ApparelPath = "";
        public string WeaponPath  = "";

        // Tint colours — applied as layer Color (white = no tint)
        public Color SkinColor = Color.white;
        public Color HairColor = Color.white;

        public PawnAppearanceData()
        {
            // Defer to scanner — runs once and caches; safe to call every time.
            BodyPath = PawnTextureScanner.FirstBody;
            HeadPath = PawnTextureScanner.FirstMaleHead;

            // Safety net: if scanner returned null or the same path as the body
            // (which would make both layers identical), fall back to the canonical
            // pre-1.5 head path so at least *something* is attempted.
            if (string.IsNullOrEmpty(HeadPath) || HeadPath == BodyPath)
                HeadPath = "Things/Pawn/Humanlike/Heads/Male/Male_Average_Normal";
        }

        /// <summary>Convenience factory for a female appearance preset.</summary>
        public static PawnAppearanceData Female()
        {
            return new PawnAppearanceData
            {
                BodyPath = PawnTextureScanner.FirstBody,
                HeadPath = PawnTextureScanner.FirstFemaleHead,
            };
        }
    }
}
