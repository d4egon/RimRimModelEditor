#pragma warning disable CS8600, CS8604
using D4egon.RimRimModelEditor.Model;
using D4egon.RimRimModelEditor.Logic;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace D4egon.RimRimModelEditor.UI.Panels
{
    public class SceneListPanel
    {
        private Vector2 _scrollPosition = Vector2.zero;

        public void Draw(Rect rect, EditorState state)
        {
            Widgets.DrawMenuSection(rect);
            var inner = rect.ContractedBy(4f);

            // 1. HEADER
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 30f), "Studio Scene");
            Text.Font = GameFont.Small;

            // 2. ADD BUTTONS
            Rect btnRect = new Rect(inner.x, inner.y + 35f, (inner.width / 2) - 2f, 24f);
            if (Widgets.ButtonText(btnRect, "Add Thing"))
            {
                // In a real scenario, this would open a Def searcher. 
                // For now, it adds a placeholder to keep the studio moving.
                state.AddObjectToScene(new SceneObject { DefName = "NewThing", DefType = RimWorldDefType.ThingDef });
            }

            Rect btnPawnRect = new Rect(btnRect.xMax + 4f, btnRect.y, btnRect.width, 24f);
            if (Widgets.ButtonText(btnPawnRect, "Add Pawn"))
            {
                state.AddObjectToScene(new SceneObject { DefName = "NewPawn", DefType = RimWorldDefType.PawnKindDef, BaseTemplate = "BasePawn" });
            }

            // 3. THE LIST
            Rect listRect = new Rect(inner.x, btnRect.yMax + 10f, inner.width, inner.height - 80f);
            Rect viewRect = new Rect(0, 0, listRect.width - 16f, state.ActiveScene.Count * 32f);

            Widgets.BeginScrollView(listRect, ref _scrollPosition, viewRect);
            float curY = 0f;

            for (int i = 0; i < state.ActiveScene.Count; i++)
            {
                var obj = state.ActiveScene[i];
                Rect rowRect = new Rect(0, curY, viewRect.width, 30f);

                // Selection Highlight
                if (state.SelectedObjectIndex == i)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else
                {
                    Widgets.DrawHighlightIfMouseover(rowRect);
                }

                // Interaction
                if (Widgets.ButtonInvisible(rowRect))
                {
                    state.SelectedObjectIndex = i;
                }

                // Label & Icon Logic
                string label = $"[{obj.DefType.ToString().Substring(0, 1)}] {obj.DefName}";
                Widgets.Label(new Rect(rowRect.x + 5f, rowRect.y + 2f, rowRect.width - 40f, rowRect.height), label);

                // Delete Button
                Rect delRect = new Rect(rowRect.xMax - 25f, rowRect.y + 4f, 20f, 20f);
                if (Widgets.ButtonImage(delRect, Widgets.CheckboxOffTex))
                {
                    state.ActiveScene.RemoveAt(i);
                    state.SelectedObjectIndex = state.ActiveScene.Count - 1;
                    break; 
                }

                curY += 32f;
            }

            Widgets.EndScrollView();

            // 4. CLEAR SCENE
            if (Widgets.ButtonText(new Rect(inner.x, inner.yMax - 25f, inner.width, 24f), "Clear Studio"))
            {
                state.ClearScene();
            }
        }
    }
}