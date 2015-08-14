﻿using System.Composition;
using System.Threading.Tasks;
using BCad.Services;

namespace BCad.Commands
{
    [ExportCadCommand("File.New", "NEW", ModifierKeys.Control, Key.N, "new", "n")]
    public class NewCommand : ICadCommand
    {
        [Import]
        public IWorkspace Workspace { get; set; }

        [Import]
        public IUndoRedoService UndoRedoService { get; set; }

        public async Task<bool> Execute(object arg)
        {
            var unsaved = await Workspace.PromptForUnsavedChanges();
            if (unsaved == UnsavedChangesResult.Cancel)
            {
                return false;
            }

            Workspace.Update(drawing: new Drawing(), activeViewPort: ViewPort.CreateDefaultViewPort(), isDirty: false);
            UndoRedoService.ClearHistory();
            return true;
        }
    }
}
