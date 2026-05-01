#pragma warning disable CS8600, CS8604
using System;
using System.IO;
using System.IO.Compression;
using D4egon.RimRimModelEditor.Model;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    public static class ZipPackager
    {
        public static string Package(EditorState state, string packageName, string outputDir)
        {
            try
            {
                Directory.CreateDirectory(outputDir);
                string zipPath = Path.Combine(outputDir, $"{SanitiseName(packageName)}_{DateTime.Now:yyyyMMdd}.zip");

                // Temp workspace
                string tempDir = Path.Combine(Path.GetTempPath(), "RimRimExport_" + Guid.NewGuid().ToString("N"));
                string modRoot = Path.Combine(tempDir, packageName);
                
                // --- 1. ABOUT FOLDER ---
                string aboutDir = Path.Combine(modRoot, "About");
                Directory.CreateDirectory(aboutDir);
                File.WriteAllText(Path.Combine(aboutDir, "About.xml"), 
                    GetAboutXml(packageName));

                // --- 2. DEFS & TEXTURES ---
                // Raw ThingDef / FleckDef XML goes into Defs/, not Patches/.
                // Use Patches/ only if wrapping in PatchOperation XML.
                string defsDir = Path.Combine(modRoot, "Defs");
                Directory.CreateDirectory(defsDir);

                var errors = XmlExporter.Export(state, defsDir, includeTextures: true, overwrite: true);
                
                foreach (var e in errors)
                    Log.Warning($"[ZipPackager] {e}");

                // --- 3. COMPRESS ---
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(modRoot, zipPath);

                Log.Message($"[RimRimModelEditor] Mod Package ZIP created at: {zipPath}");
                return zipPath;
            }
            catch (Exception ex)
            {
                Log.Error($"[ZipPackager] Failed to create package: {ex.Message}");
                return null;
            }
        }

        private static string GetAboutXml(string name) =>
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ModMetaData>
  <name>{name}</name>
  <packageId>Author.RimRim.{SanitiseName(name)}</packageId>
  <author>RimRim Studio User</author>
  <supportedVersions>
    <li>1.6</li>
  </supportedVersions>
  <description>Scene-based animation and offset adjustments exported via RimRim Model Editor.</description>
</ModMetaData>";

        private static string SanitiseName(string name) =>
            string.Concat(name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "");
    }
}