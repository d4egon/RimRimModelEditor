#pragma warning disable CS8600, CS8604, 
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using D4egon.RimRimModelEditor.Model;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    /// <summary>
    /// Reads and writes TweakPreset XML files from/to the mod's Presets folder.
    /// </summary>
    public static class PresetIO
    {
        private static string PresetsDir =>
            Path.Combine(GenFilePaths.ConfigFolderPath, "RimRimModelEditor", "Presets");

        private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(TweakPreset));

        public static void Save(string name, EditorState state)
        {
            if (state?.SelectedObject == null)
            {
                Log.Warning("[RimRimModelEditor] Cannot save preset: no scene object selected.");
                return;
            }
            try
            {
                Directory.CreateDirectory(PresetsDir);
                string path = PresetPath(name);
                var preset = TweakPreset.FromSceneObject(name, state.SelectedObject);
                using var writer = new StreamWriter(path);
                _serializer.Serialize(writer, preset);
                Log.Message($"[RimRimModelEditor] Preset saved: {path}");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimRimModelEditor] Failed to save preset '{name}': {ex}");
            }
        }

        public static TweakPreset Load(string name)
        {
            string path = PresetPath(name);
            if (!File.Exists(path)) return null;
            try
            {
                using var reader = new StreamReader(path);
                return (TweakPreset)_serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimRimModelEditor] Failed to load preset '{name}': {ex}");
                return null;
            }
        }

        public static void Delete(string name)
        {
            string path = PresetPath(name);
            if (File.Exists(path)) File.Delete(path);
        }

        public static List<string> GetSavedPresetNames()
        {
            var names = new List<string>();
            if (!Directory.Exists(PresetsDir)) return names;
            foreach (var file in Directory.GetFiles(PresetsDir, "*.xml"))
                names.Add(Path.GetFileNameWithoutExtension(file));
            return names;
        }

        public static void SaveDialog(EditorState state) =>
            Find.WindowStack.Add(new UI.Dialogs.PresetDialog(state));

        private static string PresetPath(string name) =>
            Path.Combine(PresetsDir, $"{SanitiseName(name)}.xml");

        private static string SanitiseName(string name) =>
            string.Concat(name.Split(Path.GetInvalidFileNameChars()));
    }
}
