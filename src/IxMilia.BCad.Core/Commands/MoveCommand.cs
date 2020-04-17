using System.Collections.Generic;
using IxMilia.BCad.Entities;
using IxMilia.BCad.Utilities;

namespace IxMilia.BCad.Commands
{
    [ExportCadCommand("Edit.Move", "MOVE", "move", "mov", "m")]
    internal class MoveCommand : AbstractCopyMoveCommand
    {
        protected override Drawing DoEdit(Drawing drawing, IEnumerable<Entity> entities, Vector delta)
        {
            foreach (var ent in entities)
            {
                drawing = drawing.Replace(ent, EditUtilities.Move(ent, delta));
            }

            return drawing;
        }
    }
}
