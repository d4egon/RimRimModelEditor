#pragma warning disable CS8600, CS8604
using System;
using System.IO;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    /// <summary>
    /// All pixel-reads must happen inside an OnGUI Repaint pass, never in Update().
    /// Call TryExecutePending() at the VERY END of DoWindowContents so the preview
    /// has already been fully drawn before we sample the framebuffer.
    /// </summary>
    public static class ScreenshotCapture
    {
        // ── Full-window screenshot ─────────────────────────────────────────────
        private static bool   _pendingFullscreen;
        private static string _pendingName = "Screenshot";

        public static void Capture(string baseName)
        {
            _pendingName       = baseName;
            _pendingFullscreen = true;
        }

        // ── Preview-panel snapshot ─────────────────────────────────────────────
        // windowRect : the editor Window's screen-space rect.
        // previewRect: the preview panel rect in window-local coordinates.
        // callback   : receives the Texture2D — caller owns it, destroy when done.
        private static bool              _pendingSnapshot;
        private static Rect              _snapshotWindowRect;
        private static Rect              _snapshotPanelRect;
        private static Action<Texture2D> _snapshotCallback;

        public static void RequestSnapshot(Rect windowRect, Rect previewRect, Action<Texture2D> callback)
        {
            _snapshotWindowRect = windowRect;
            _snapshotPanelRect  = previewRect;
            _snapshotCallback   = callback;
            _pendingSnapshot    = true;
        }

        // ── Must be called at the END of DoWindowContents, every OnGUI pass ───
        public static void TryExecutePending()
        {
            if (Event.current.type != EventType.Repaint) return;

            if (_pendingSnapshot)
            {
                _pendingSnapshot = false;
                ExecuteSnapshot();
            }

            if (_pendingFullscreen)
            {
                _pendingFullscreen = false;
                ExecuteFullscreen();
            }
        }

        // Back-compat stub — timing now handled by TryExecutePending.
        public static void DoCapture() { }

        // ── Internals ─────────────────────────────────────────────────────────

        private static void ExecuteSnapshot()
        {
            try
            {
                // Convert window-local panel rect → screen pixels.
                // Unity screen space: Y=0 at BOTTOM; GUI: Y=0 at TOP — so flip Y.
                float sx = _snapshotWindowRect.x + _snapshotPanelRect.x;
                float sy = Screen.height - (_snapshotWindowRect.y + _snapshotPanelRect.yMax);
                int   w  = Mathf.Max(1, (int)_snapshotPanelRect.width);
                int   h  = Mathf.Max(1, (int)_snapshotPanelRect.height);

                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(sx, sy, w, h), 0, 0);
                tex.Apply();

                _snapshotCallback?.Invoke(tex);
            }
            catch (Exception ex) { Log.Error("[RimRim] Snapshot failed: " + ex); }
        }

        private static void ExecuteFullscreen()
        {
            try
            {
                var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                tex.Apply();

                byte[] bytes = EncodePNG(tex);
                UnityEngine.Object.Destroy(tex);

                if (bytes == null) { Log.Error("[RimRim] EncodeToPNG unavailable."); return; }

                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "RimRimScreenshots");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, $"{_pendingName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                File.WriteAllBytes(file, bytes);
                Messages.Message("Screenshot saved: " + file, MessageTypeDefOf.TaskCompletion, false);
            }
            catch (Exception ex) { Log.Error("[RimRim] Screenshot failed: " + ex); }
        }

        public static byte[] EncodePNG(Texture2D tex)
        {
            var m = typeof(Texture2D).GetMethod("EncodeToPNG");
            if (m != null) return (byte[])m.Invoke(tex, null);

            var ic = AccessTools.TypeByName("UnityEngine.ImageConversion");
            if (ic != null)
            {
                var sm = ic.GetMethod("EncodeToPNG", new[] { typeof(Texture2D) });
                if (sm != null) return (byte[])sm.Invoke(null, new object[] { tex });
            }
            return null;
        }
    }
}
