// PawnFactory — thin façade. All real pawn-compositing lives in PawnCompositor.
// This class exists only so that older code referencing this API still compiles.
using D4egon.RimRimModelEditor.Model;
using Verse;

namespace D4egon.RimRimModelEditor.Logic
{
    public static class PawnFactory
    {
        /// <summary>
        /// Creates a studio pawn scene-object from explicit body and head texture paths.
        /// Prefer using LayerFactory.CreateSceneObjectFromPawn(PawnKindDef) when a real
        /// def is available — this method is for manual / custom assemblies.
        /// </summary>
        public static SceneObject CreateStudioPawn(
            string bodyPath, string headPath, string defName = "NewPawn")
        {
            var obj = new SceneObject
            {
                DefName      = defName,
                DefType      = RimWorldDefType.PawnKindDef,
                BaseTemplate = "BasePawn"
            };

            var appearance = new PawnAppearanceData
            {
                BodyPath = !string.IsNullOrEmpty(bodyPath) ? bodyPath : PawnTextureScanner.FirstBody,
                HeadPath = !string.IsNullOrEmpty(headPath) ? headPath : PawnTextureScanner.FirstMaleHead,
            };

            PawnCompositor.RebuildPawnLayers(obj, appearance);
            return obj;
        }

        /// <summary>Generates a minimal PawnKindDef XML snippet for the given scene object.</summary>
        public static string GeneratePawnKindXml(SceneObject obj)
            => XmlExporter.GeneratePawnKindXmlString(obj);
    }
}
