using RimWorld;
using Verse;

namespace D4egon.RimRimModelEditor.UI
{
    // This name MUST match the XML <workerClass> exactly
    public class MainButtonWorkerOpenRimRimEditor : MainButtonWorker
    {
        public override void Activate()
        {
            // Toggle logic: If window is open, close it. If closed, open it.
            if (Find.WindowStack.IsOpen<MainEditorWindow>())
            {
                Find.WindowStack.TryRemove(typeof(MainEditorWindow));
            }
            else
            {
                Find.WindowStack.Add(new MainEditorWindow());
            }
        }
    }
}