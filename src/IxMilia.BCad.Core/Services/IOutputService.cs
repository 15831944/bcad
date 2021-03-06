using IxMilia.BCad.EventArguments;

namespace IxMilia.BCad.Services
{
    public delegate void WriteLineEventHandler(object sender, WriteLineEventArgs e);

    public interface IOutputService : IWorkspaceService
    {
        event WriteLineEventHandler LineWritten;
        void WriteLine(string text, params object[] args);
    }
}
