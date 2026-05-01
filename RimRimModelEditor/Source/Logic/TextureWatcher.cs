#pragma warning disable CS8600, CS8604, 
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    /// <summary>
    /// Watches the mod's Textures folder on a background thread.
    /// Changed files are queued and flushed on the main thread via Update().
    /// </summary>
    public class TextureWatcher
    {
        public static readonly TextureWatcher Instance = new TextureWatcher();

        private FileSystemWatcher _watcher;
        private readonly ConcurrentQueue<string> _changedPaths = new ConcurrentQueue<string>();
        private Thread _flushThread;
        private bool _running;

        private TextureWatcher() { }

        public void Start(string modRoot)
        {
            string texDir = Path.Combine(modRoot, "Textures");
            if (!Directory.Exists(texDir)) return;

            _watcher = new FileSystemWatcher(texDir, "*.png")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };
            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.EnableRaisingEvents = true;
            _running = true;

            // Flush thread — wakes every 500 ms and processes the queue on the Unity main thread via Log.Message trick
            // In a real mod you'd hook into a GameComponent.GameComponentTick, but this demonstrates the pattern.
            Log.Message("[RimRimModelEditor] TextureWatcher started on: " + texDir);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            _changedPaths.Enqueue(e.FullPath);
        }

        /// <summary>Call from a GameComponent tick or similar main-thread hook.</summary>
        public void Flush()
        {
            var state = UI.MainEditorWindow.CurrentState;
            if (state == null) return;

            while (_changedPaths.TryDequeue(out string path))
            {
                bool foundMatch = false;

                // Iterate every object in the full scene, not just the selected one's layers,
                // so hot-reload works for all objects regardless of which is active.
                foreach (var sceneObj in state.ActiveScene)
                {
                    foreach (var layer in sceneObj.Layers)
                    {
                        // RimWorld TexturePaths are relative (Things/Pawn/...) but 'path' is
                        // absolute — use Contains with a normalised separator.
                        if (!layer.TexturePath.NullOrEmpty() &&
                            path.Contains(layer.TexturePath.Replace('/', Path.DirectorySeparatorChar)))
                        {
                            layer.CachedTexture = null;
                            foundMatch = true;
                            Log.Message($"[RimRim] Hot-Reloaded: {layer.TexturePath}");
                        }
                    }
                }

                if (!foundMatch)
                    Log.Warning($"[RimRim] File changed at {path}, but no scene layer is using this path.");

                if (foundMatch && RimRimMod.Settings.AutoHotReload)
                    HotReloader.Reload(state.SelectedDefName);
            }
        }

        public void Stop()
        {
            _running = false;
            _watcher?.Dispose();
        }
    }
}
