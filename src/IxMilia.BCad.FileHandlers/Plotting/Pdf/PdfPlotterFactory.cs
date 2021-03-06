using System.ComponentModel;
using System.Composition;

namespace IxMilia.BCad.Plotting.Pdf
{
    [ExportPlotterFactory("PDF", ViewTypeName = "IxMilia.BCad.UI.Controls.PdfPlotterControl")]
    public class PdfPlotterFactory : IPlotterFactory
    {
        [Import]
        public IWorkspace Workspace { get; set; }

        public PlotterBase CreatePlotter(INotifyPropertyChanged viewModel)
        {
            return new PdfPlotter((PdfPlotterViewModel)viewModel);
        }

        public INotifyPropertyChanged CreatePlotterViewModel()
        {
            return new PdfPlotterViewModel(Workspace);
        }
    }
}
