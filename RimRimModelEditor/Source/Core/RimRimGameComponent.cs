using D4egon.RimRimModelEditor.Logic;
using Verse;

namespace D4egon.RimRimModelEditor
{
    public class RimRimGameComponent : GameComponent
    {
        // No need for a tick counter if we want high-responsiveness 
        // during editor sessions.

        public RimRimGameComponent(Game game) { }

        public override void GameComponentUpdate()
        {
            // Update() runs every frame regardless of pause state.
            // Since Flush() only does work if the queue has items,
            // it's very performance-cheap to call here.
            TextureWatcher.Instance.Flush();

            // Screenshot capture must be triggered from the main thread
            // after the frame has finished rendering.
            ScreenshotCapture.DoCapture();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Log.Message("[RimRimModelEditor] RimRimGameComponent initialized and watching.");
        }
    }
}