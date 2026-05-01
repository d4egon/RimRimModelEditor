using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    /// <summary>
    /// Lazily discovers which pawn texture paths actually exist in the current RimWorld install.
    /// Handles path-format differences across 1.4 / 1.5 / 1.6 without hard-coding version checks.
    /// All results are cached after the first scan — call Reset() to force a re-scan.
    /// </summary>
    public static class PawnTextureScanner
    {
        // ── BODY ─────────────────────────────────────────────────────────────

        private static readonly string[] BodyCandidates =
        {
            "Things/Pawn/Humanlike/Bodies/Naked_Thin",
            "Things/Pawn/Humanlike/Bodies/Naked_Fat",
            "Things/Pawn/Humanlike/Bodies/Naked_Hulk",
            "Things/Pawn/Humanlike/Bodies/Naked_Male",
            "Things/Pawn/Humanlike/Bodies/Naked_Female",
            // 1.5+ alternate casing
            "Things/Pawn/Humanlike/Bodies/Thin",
            "Things/Pawn/Humanlike/Bodies/Fat",
            "Things/Pawn/Humanlike/Bodies/Hulk",
            "Things/Pawn/Humanlike/Bodies/Male",
            "Things/Pawn/Humanlike/Bodies/Female",
        };

        // ── HEAD ─────────────────────────────────────────────────────────────
        // RimWorld 1.6: heads live in subfolders  Heads/Male/  and  Heads/Female/
        //   e.g. Things/Pawn/Humanlike/Heads/Male/Male_Average_Normal
        // Skull types: Average, Narrow, HeavyJaw  (HeavyJaw added in 1.5+)
        // Face types:  Normal, Pointy, Wide
        // Gender-neutral heads sit directly in Heads/:  None_Average_Skull, None_Average_Stump
        // Pre-1.5 flat format (no subfolder) kept as fallback for older installs.
        // Directional textures append _south / _north / _east.

        private static readonly string[] HeadCandidates =
        {
            // ── 1.6 Male subfolder ───────────────────────────────────────────
            "Things/Pawn/Humanlike/Heads/Male/Male_Average_Normal",
            "Things/Pawn/Humanlike/Heads/Male/Male_Average_Pointy",
            "Things/Pawn/Humanlike/Heads/Male/Male_Average_Wide",
            "Things/Pawn/Humanlike/Heads/Male/Male_Narrow_Normal",
            "Things/Pawn/Humanlike/Heads/Male/Male_Narrow_Pointy",
            "Things/Pawn/Humanlike/Heads/Male/Male_Narrow_Wide",
            "Things/Pawn/Humanlike/Heads/Male/Male_HeavyJaw_Normal",
            "Things/Pawn/Humanlike/Heads/Male/Male_HeavyJaw_Pointy",
            "Things/Pawn/Humanlike/Heads/Male/Male_HeavyJaw_Wide",
            // ── 1.6 Female subfolder ─────────────────────────────────────────
            "Things/Pawn/Humanlike/Heads/Female/Female_Average_Normal",
            "Things/Pawn/Humanlike/Heads/Female/Female_Average_Pointy",
            "Things/Pawn/Humanlike/Heads/Female/Female_Average_Wide",
            "Things/Pawn/Humanlike/Heads/Female/Female_Narrow_Normal",
            "Things/Pawn/Humanlike/Heads/Female/Female_Narrow_Pointy",
            "Things/Pawn/Humanlike/Heads/Female/Female_Narrow_Wide",
            "Things/Pawn/Humanlike/Heads/Female/Female_HeavyJaw_Normal",
            "Things/Pawn/Humanlike/Heads/Female/Female_HeavyJaw_Pointy",
            "Things/Pawn/Humanlike/Heads/Female/Female_HeavyJaw_Wide",
            // ── 1.6 gender-neutral (directly in Heads/) ─────────────────────
            "Things/Pawn/Humanlike/Heads/None_Average_Skull",
            "Things/Pawn/Humanlike/Heads/None_Average_Stump",
            // ── pre-1.5 flat format (fallback for older installs) ────────────
            "Things/Pawn/Humanlike/Heads/Male_Average_Normal",
            "Things/Pawn/Humanlike/Heads/Male_Average_Pointy",
            "Things/Pawn/Humanlike/Heads/Male_Average_Wide",
            "Things/Pawn/Humanlike/Heads/Male_Narrow_Normal",
            "Things/Pawn/Humanlike/Heads/Male_Narrow_Pointy",
            "Things/Pawn/Humanlike/Heads/Male_Narrow_Wide",
            "Things/Pawn/Humanlike/Heads/Female_Average_Normal",
            "Things/Pawn/Humanlike/Heads/Female_Average_Pointy",
            "Things/Pawn/Humanlike/Heads/Female_Average_Wide",
            "Things/Pawn/Humanlike/Heads/Female_Narrow_Normal",
            "Things/Pawn/Humanlike/Heads/Female_Narrow_Pointy",
            "Things/Pawn/Humanlike/Heads/Female_Narrow_Wide",
        };

        // ── HAIR ─────────────────────────────────────────────────────────────

        private static readonly string[] HairCandidates =
        {
            // 1.6 — hair lives directly in Hairs/
            "Things/Pawn/Humanlike/Hairs/Bald",
            "Things/Pawn/Humanlike/Hairs/Short",
            "Things/Pawn/Humanlike/Hairs/Shaved",
            "Things/Pawn/Humanlike/Hairs/Medium",
            "Things/Pawn/Humanlike/Hairs/Long",
            "Things/Pawn/Humanlike/Hairs/Messy",
            "Things/Pawn/Humanlike/Hairs/Wavy",
            "Things/Pawn/Humanlike/Hairs/Pony",
            "Things/Pawn/Humanlike/Hairs/Pigtails",
            // alternate folder name used in some versions
            "Things/Pawn/Humanlike/Hair/Bald",
            "Things/Pawn/Humanlike/Hair/Short",
        };

        // ── BEARD ─────────────────────────────────────────────────────────────

        private static readonly string[] BeardCandidates =
        {
            "Things/Pawn/Humanlike/Beards/NoBeard",
            "Things/Pawn/Humanlike/Beards/Beard_Stubble",
            "Things/Pawn/Humanlike/Beards/Beard_Full",
            "Things/Pawn/Humanlike/Beards/Beard_Goatee",
            "Things/Pawn/Humanlike/Beards/Beard_Bandito",
            "Things/Pawn/Humanlike/Beards/Beard_Sideburns",
            "Things/Pawn/Humanlike/Beards/Beard_Mutton",
            "Things/Pawn/Humanlike/Beards/Beard_Chinstrap",
            "Things/Pawn/Humanlike/Beards/Beard_Handlebar",
        };

        // ── HEAD ATTACHMENTS ──────────────────────────────────────────────────
        // Things like eye patches, horns, scars, special modded overlays.

        private static readonly string[] HeadAttachmentCandidates =
        {
            "Things/Pawn/Humanlike/HeadAttachments/Blindfold",
            "Things/Pawn/Humanlike/HeadAttachments/EyePatch",
            "Things/Pawn/Humanlike/HeadAttachments/Scars",
        };

        // ── Cache ─────────────────────────────────────────────────────────────

        private static List<string> _bodies;
        private static List<string> _heads;
        private static List<string> _hairs;
        private static List<string> _beards;
        private static List<string> _headAttachments;

        // ── Public API ────────────────────────────────────────────────────────

        public static List<string> AvailableBodies         => _bodies          ?? (_bodies          = Scan(BodyCandidates,            false));
        public static List<string> AvailableHeads          => _heads           ?? (_heads           = Scan(HeadCandidates,            true));
        public static List<string> AvailableHairs          => _hairs           ?? (_hairs           = Scan(HairCandidates,            true));
        public static List<string> AvailableBeards         => _beards          ?? (_beards          = Scan(BeardCandidates,           true));
        public static List<string> AvailableHeadAttachments => _headAttachments ?? (_headAttachments = Scan(HeadAttachmentCandidates,  true));

        /// <summary>First available body path, or the thin fallback.</summary>
        public static string FirstBody =>
            AvailableBodies.Count > 0
                ? AvailableBodies[0]
                : "Things/Pawn/Humanlike/Bodies/Naked_Thin";

        /// <summary>First available male head path, or a best-guess fallback.</summary>
        public static string FirstMaleHead =>
            // Prefer the 1.6 subfolder format first, then flat pre-1.5 format.
            FirstMatchContaining(AvailableHeads, "Heads/Male/") ??
            FirstMatchContaining(AvailableHeads, "Male") ??
            (AvailableHeads.Count > 0
                ? AvailableHeads[0]
                : "Things/Pawn/Humanlike/Heads/Male/Male_Average_Normal");

        /// <summary>First available female head path, or falls back to the male one.</summary>
        public static string FirstFemaleHead =>
            FirstMatchContaining(AvailableHeads, "Heads/Female/") ??
            FirstMatchContaining(AvailableHeads, "Female") ??
            FirstMaleHead;

        /// <summary>Clears all cached results so the next access triggers a fresh scan.</summary>
        public static void Reset()
        {
            _bodies          = null;
            _heads           = null;
            _hairs           = null;
            _beards          = null;
            _headAttachments = null;
        }

        /// <summary>
        /// Manually adds a head path to the discovered list (e.g. one the user found themselves).
        /// Ensures the scanner cache is initialised before adding.
        /// </summary>
        public static void RegisterCustomHeadPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (_heads == null) _heads = new System.Collections.Generic.List<string>();
            if (!_heads.Contains(path)) _heads.Add(path);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the subset of <paramref name="candidates"/> whose textures actually exist.
        /// For directional paths, checks the _south variant first (the default preview facing).
        /// </summary>
        private static List<string> Scan(string[] candidates, bool checkDirectional)
        {
            var found = new List<string>();
            foreach (var path in candidates)
            {
                bool exists = false;
                if (checkDirectional)
                {
                    // Try the most common directional suffix first; fall back to bare path.
                    exists = ContentFinder<Texture2D>.Get(path + "_south", false) != null
                          || ContentFinder<Texture2D>.Get(path + "_east",  false) != null
                          || ContentFinder<Texture2D>.Get(path,            false) != null;
                }
                else
                {
                    // Body textures are also directional (_south etc.) in RimWorld.
                    exists = ContentFinder<Texture2D>.Get(path + "_south", false) != null
                          || ContentFinder<Texture2D>.Get(path,            false) != null;
                }

                if (exists && !found.Contains(path))
                    found.Add(path);
            }
            return found;
        }

        private static string FirstMatchContaining(List<string> list, string keyword)
        {
            foreach (var s in list)
                if (s.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return s;
            return null;
        }
    }
}
