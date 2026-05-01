#pragma warning disable CS8600, CS8604, CS8618
using System.Reflection;
using HarmonyLib;
using Verse;

namespace D4egon.RimRimModelEditor
{
    /// <summary>
    /// Entry point. Bootstraps Harmony patches and the folder watcher.
    /// </summary>
    public class RimRimMod : Mod
    {
        public override void WriteSettings()
        {
            base.WriteSettings();
        }
        public static RimRimMod Instance { get; private set; }
        public static RimRimSettings Settings { get; private set; }

        private static readonly Harmony Harmony = new Harmony("D4egon.RimRimModelEditor");

        public RimRimMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<RimRimSettings>();

            Harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logic.TextureWatcher.Instance.Start(content.RootDir);

            Log.Message("[RimRimModelEditor] Loaded — Harmony patches applied.");
        }

        public override string SettingsCategory() => "RimRim Model Editor";

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }
    }
}
