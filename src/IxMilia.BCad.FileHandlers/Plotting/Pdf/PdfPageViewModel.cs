using System;
using System.Collections.Generic;

namespace IxMilia.BCad.Plotting.Pdf
{
    public class PdfPageViewModel : ViewPortViewModel
    {
        private int _pageNumber;
        public int PageNumber
        {
            get => _pageNumber;
            set
            {
                SetValue(ref _pageNumber, value);
                OnPropertyChanged(nameof(PageName));
            }
        }

        public string PageName => $"Page {PageNumber}";

        private PdfPageSize _pageSize;
        public PdfPageSize PageSize
        {
            get => _pageSize;
            set
            {
                SetValue(ref _pageSize, value);
                OnPropertyChanged(nameof(ViewWidth));
                OnPropertyChanged(nameof(ViewHeight));
                OnPropertyChanged(nameof(PreviewWidth));
                OnPropertyChanged(nameof(PreviewHeight));
                OnPropertyChanged(nameof(ViewPort));
            }
        }

        public IEnumerable<PdfPageSize> AvailablePageSizes => new[] { PdfPageSize.Portrait, PdfPageSize.Landscape };

        public override double ViewWidth => PageSize == PdfPageSize.Portrait ? 8.5 : 11.0;

        public override double ViewHeight => PageSize == PdfPageSize.Portrait ? 11.0 : 8.5;

        public double MaxPreviewSize => 400.0;

        public double PreviewWidth => (ViewWidth / Math.Max(ViewWidth, ViewHeight)) * MaxPreviewSize;

        public double PreviewHeight=> (ViewHeight / Math.Max(ViewWidth, ViewHeight)) * MaxPreviewSize;

        public PdfPageViewModel(IWorkspace workspace)
            : base(workspace)
        {
            PageSize = PdfPageSize.Portrait;
        }
    }
}
