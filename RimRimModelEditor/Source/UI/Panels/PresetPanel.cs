#pragma warning disable CS8600, CS8604, 
using System.Collections.Generic;
using D4egon.RimRimModelEditor.Logic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;
using RimWorld;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    /// <summary>
    /// Preset tab — browse saved presets, load, delete.
    /// </summary>
    public class PresetPanel
    {
        private Vector2 _scroll;
        private List<string> _presetNames;

        public void Draw(Rect rect, EditorState state, System.Action<string> pushUndo)
        {
            var inner = rect.ContractedBy(4f);

            if (Widgets.ButtonText(new Rect(inner.x, inner.y, 100f, 24f), "Refresh"))
                _presetNames = null;

            if (_presetNames == null)
                _presetNames = PresetIO.GetSavedPresetNames();

            var listRect = new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f);

            if (_presetNames.Count == 0)
            {
                Widgets.Label(listRect, "No presets saved yet.");
                return;
            }

            float rowH = 26f;
            var viewRect = new Rect(0, 0, listRect.width - 16f, _presetNames.Count * rowH);
            Widgets.BeginScrollView(listRect, ref _scroll, viewRect);

            for (int i = 0; i < _presetNames.Count; i++)
            {
                var name = _presetNames[i];
                var row = new Rect(0, i * rowH, viewRect.width, rowH);

                Widgets.Label(new Rect(row.x, row.y, row.width - 140f, rowH), name);

                if (Widgets.ButtonText(new Rect(row.xMax - 138f, row.y, 66f, rowH - 2f), "Load"))
                {
                    pushUndo("Load preset");
                    var loaded = PresetIO.Load(name);
                    if (loaded != null)
                    {
                        var newObj = loaded.ToSceneObject();

                        if (state.SelectedObject != null)
                        {
                            // Overwrite the selected scene object in-place
                            var sel = state.SelectedObject;
                            sel.DefName = newObj.DefName;
                            sel.DefType = newObj.DefType;
                            sel.BaseTemplate = newObj.BaseTemplate;
                            sel.RootScale = newObj.RootScale;
                            sel.RootPosition = newObj.RootPosition;
                            sel.RootRotation = newObj.RootRotation;
                            sel.EquippedAngleOffset = newObj.EquippedAngleOffset;
                            sel.GrowthRate = newObj.GrowthRate;
                            sel.Layers.Clear();
                            foreach (var l in newObj.Layers) sel.Layers.Add(l);
                        }
                        else
                        {
                            // No selected object — add preset as a new scene entry
                            state.AddObjectToScene(newObj);
                        }

                        Messages.Message($"Preset loaded: {name}", MessageTypeDefOf.PositiveEvent, false);
                    }
                }

                if (Widgets.ButtonText(new Rect(row.xMax - 68f, row.y, 66f, rowH - 2f), "Delete"))
                {
                    PresetIO.Delete(name);
                    _presetNames = null;
                }
            }

            Widgets.EndScrollView();
        }
    }
}
