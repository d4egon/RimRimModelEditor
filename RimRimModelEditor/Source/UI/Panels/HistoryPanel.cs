using System;
using System.Collections.Generic;
using D4egon.RimRimModelEditor.Model;
using UnityEngine;
using Verse;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    /// <summary>
    /// Version history tab — lists all named snapshots and allows one-click restore.
    /// </summary>
    public class HistoryPanel
    {
        private Vector2 _scroll;

        public void Draw(Rect rect, EditorState state,
                         List<VersionEntry> history,
                         Action<VersionEntry> restore)
        {
            var inner = rect.ContractedBy(4f);

            if (history.Count == 0)
            {
                Widgets.Label(inner, "No version history yet.");
                return;
            }

            float rowH = 24f;
            var viewRect = new Rect(0, 0, inner.width - 16f, history.Count * rowH);
            Widgets.BeginScrollView(inner, ref _scroll, viewRect);

            for (int i = history.Count - 1; i >= 0; i--)   // most recent first
            {
                var entry = history[i];
                float y = (history.Count - 1 - i) * rowH;
                var row = new Rect(0, y, viewRect.width, rowH);

                Widgets.Label(new Rect(row.x, row.y, row.width - 70f, rowH), entry.ToString());

                if (Widgets.ButtonText(new Rect(row.xMax - 68f, row.y, 66f, rowH - 2f), "Restore"))
                    restore(entry);
            }

            Widgets.EndScrollView();
        }
    }
}
