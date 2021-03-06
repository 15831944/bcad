using System.Collections.Generic;
using IxMilia.BCad.Entities;
using IxMilia.BCad.Utilities;

namespace IxMilia.BCad.Commands
{
    [ExportCadCommand("Edit.Trim", "TRIM", "trim", "tr")]
    public class TrimCommand : AbstractTrimExtendCommand
    {
        protected override string GetBoundsText()
        {
            return "Select cutting edges";
        }

        protected override string GetTrimExtendText()
        {
            return "Entity to trim";
        }

        protected override void DoTrimExtend(SelectedEntity selectedEntity, IEnumerable<Primitives.IPrimitive> boundaryPrimitives, out IEnumerable<Entity> removed, out IEnumerable<Entity> added)
        {
            EditUtilities.Trim(selectedEntity, boundaryPrimitives, out removed, out added);
        }
    }
}
