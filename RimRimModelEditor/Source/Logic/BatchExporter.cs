using System;
using System.IO;
using System.Collections.Generic;
using D4egon.RimRimModelEditor.Model;
using Verse;
using RimWorld;

namespace D4egon.RimRimModelEditor.Logic
{
    public static class BatchExporter
    {
        public static void Run(EditorState currentState)
        {
            // 1. Resolve path
            string baseDir = RimRimMod.Settings.DefaultExportPath;
            if (string.IsNullOrEmpty(baseDir))
            {
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "RimRimBatchExport");
            }

            try
            {
                // 2. Ensure Root Directory exists
                if (!Directory.Exists(baseDir))
                    Directory.CreateDirectory(baseDir);

                int count = 0;
                List<string> savedPresets = PresetIO.GetSavedPresetNames();

                if (savedPresets.NullOrEmpty())
                {
                    Messages.Message("No saved presets found to export.", MessageTypeDefOf.RejectInput, false);
                    return;
                }

                foreach (var name in savedPresets)
                {
                    var preset = PresetIO.Load(name);
                    if (preset == null) continue;

                    // Build a temporary EditorState around this preset's SceneObject
                    var state = new EditorState();
                    state.AddObjectToScene(preset.ToSceneObject());
                    string defDir = Path.Combine(baseDir, preset.DefName);

                    // 3. Ensure sub-directory for the specific Def exists
                    if (!Directory.Exists(defDir))
                        Directory.CreateDirectory(defDir);

                    var errors = XmlExporter.Export(state, defDir, includeTextures: false, overwrite: true);

                    if (errors.NullOrEmpty())
                    {
                        count++;
                    }
                    else
                    {
                        foreach (var e in errors)
                            Log.Warning($"[RimRim] Batch export warning ({name}): {e}");
                    }
                }

                Messages.Message($"Batch export complete — {count} defs exported to {baseDir}",
                    MessageTypeDefOf.TaskCompletion, false);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimRim] Batch export failed: {ex.Message}");
                Messages.Message("Batch export failed. Check logs.", MessageTypeDefOf.CautionInput, false);
            }
        }
    }
}