#pragma warning disable CS8600, CS8604
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.UI.Dialogs
{
    public class Dialog_FilePicker : Window
    {
        private string _currentDir;
        private Vector2 _scrollPos;
        private readonly Action<string> _onFileSelected;
        private List<FileSystemInfo> _cachedItems;

        public override Vector2 InitialSize => new Vector2(600f, 700f);

        public Dialog_FilePicker(Action<string> onFileSelected)
        {
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            this._onFileSelected = onFileSelected;

            // Start in the mod's texture directory or user home
            _currentDir = GenFilePaths.ConfigFolderPath; 
            RefreshFiles();
        }

        private void RefreshFiles()
        {
            try
            {
                var dirInfo = new DirectoryInfo(_currentDir);
                _cachedItems = dirInfo.GetFileSystemInfos()
                    .Where(f => (f.Attributes & FileAttributes.Hidden) == 0)
                    .OrderBy(f => f is DirectoryInfo ? 0 : 1)
                    .ThenBy(f => f.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"[RimRim] Failed to read directory: {ex.Message}");
                _cachedItems = new List<FileSystemInfo>();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, 0, inRect.width, 30f), "Select Texture (.png)");
            Text.Font = GameFont.Small;

            // Breadcrumbs / Current Path
            Rect pathRect = new Rect(0, 35f, inRect.width, 25f);
            Widgets.Label(pathRect, _currentDir);
            if (Widgets.ButtonText(new Rect(inRect.width - 60f, 35f, 60f, 25f), "Up"))
            {
                var parent = Directory.GetParent(_currentDir);
                if (parent != null)
                {
                    _currentDir = parent.FullName;
                    RefreshFiles();
                }
            }

            // File List
            Rect scrollRect = new Rect(0, 70f, inRect.width, inRect.height - 110f);
            Rect viewRect = new Rect(0, 0, scrollRect.width - 16f, _cachedItems.Count * 28f);

            Widgets.BeginScrollView(scrollRect, ref _scrollPos, viewRect);
            float curY = 0f;

            foreach (var item in _cachedItems)
            {
                Rect rowRect = new Rect(0, curY, viewRect.width, 26f);
                bool isDir = item is DirectoryInfo;

                // Only show folders and PNGs
                if (!isDir && !item.Name.ToLower().EndsWith(".png")) continue;

                Widgets.DrawHighlightIfMouseover(rowRect);
                string label = isDir ? $"[DIR] {item.Name}" : item.Name;

                if (Widgets.ButtonInvisible(rowRect))
                {
                    if (isDir)
                    {
                        _currentDir = item.FullName;
                        RefreshFiles();
                        break;
                    }
                    else
                    {
                        _onFileSelected?.Invoke(item.FullName);
                        this.Close();
                    }
                }

                Widgets.Label(new Rect(rowRect.x + 5f, rowRect.y, rowRect.width, rowRect.height), label);
                curY += 28f;
            }

            Widgets.EndScrollView();
        }
    }
}