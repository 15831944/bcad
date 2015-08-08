﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace BCad.UI.Controls
{
    public enum PlotType
    {
        None = 0,
        File,
        Print
    }

    public enum ViewportType
    {
        Extents,
        Window
    }

    public enum PageSize
    {
        Letter,
        Legal,
        Landscape
    }

    public enum ScalingType
    {
        Absolute,
        ToFit
    }

    public enum ColorMapType
    {
        DrawingDefault,
        AllBlack
    }

    public class PlotDialogViewModel : INotifyPropertyChanged
    {
        private const string ViewPortProperty = "ViewPort";
        private IWorkspace workspace;

        public IEnumerable<PlotType> AvailablePlotTypes
        {
            get { return new[] { PlotType.File, PlotType.Print }; }
        }

        private Drawing drawing;
        private CadColor defaultColor;
        private PlotType plotType;
        private string fileName;
        private ViewportType viewportType;
        private ScalingType scalingType;
        private ColorMapType colorMapType;
        private Point bottomLeft;
        private Point topRight;
        private double scaleA;
        private double scaleB;
        private double dpi;
        private PageSize pageSize;
        private Visibility printOptVis;
        private Visibility fileOptVis;
        private int pixelWidth;
        private int pixelHeight;
        private double previewWidth;
        private double previewHeight;
        private double maxPreviewWidth;
        private double maxPreviewHeight;
        private ViewPort activeViewPort;

        public PlotType PlotType
        {
            get { return this.plotType; }
            set
            {
                if (this.plotType == value)
                    return;
                this.plotType = value;
                OnPropertyChanged();
                switch (this.plotType)
                {
                    case PlotType.File:
                        FileOptionsVisibility = Visibility.Visible;
                        PrintOptionsVisibility = Visibility.Hidden;
                        break;
                    case PlotType.Print:
                        FileOptionsVisibility = Visibility.Hidden;
                        PrintOptionsVisibility = Visibility.Visible;
                        break;
                    default:
                        throw new InvalidOperationException("unsupported plot type");
                }

                UpdatePreviewSize();
                OnPropertyChangedDirect(ViewPortProperty);
            }
        }

        public Drawing Drawing
        {
            get { return drawing; }
            set
            {
                if (drawing == value)
                    return;
                drawing = value;
                OnPropertyChanged();
            }
        }

        public CadColor DefaultColor
        {
            get { return defaultColor; }
            set
            {
                if (defaultColor == value)
                    return;
                defaultColor = value;
                OnPropertyChanged();
            }
        }

        public string FileName
        {
            get { return this.fileName; }
            set
            {
                if (this.fileName == value)
                    return;
                this.fileName = value;
                OnPropertyChanged();
            }
        }

        public ViewportType ViewportType
        {
            get { return this.viewportType; }
            set
            {
                if (this.viewportType == value)
                    return;
                this.viewportType = value;
                OnPropertyChanged();
                OnPropertyChangedDirect(ViewPortProperty);
            }
        }

        public ScalingType ScalingType
        {
            get { return this.scalingType; }
            set
            {
                if (this.scalingType == value)
                    return;
                this.scalingType = value;
                OnPropertyChanged();
                OnPropertyChangedDirect(ViewPortProperty);
            }
        }

        public ColorMapType ColorMapType
        {
            get { return this.colorMapType; }
            set
            {
                if (this.colorMapType == value)
                    return;
                this.colorMapType = value;
                OnPropertyChanged();
            }
        }

        public Point BottomLeft
        {
            get { return this.bottomLeft; }
            set
            {
                if (this.bottomLeft == value)
                    return;
                this.bottomLeft = value;
                OnPropertyChanged();
                OnPropertyChangedDirect(ViewPortProperty);
            }
        }

        public Point TopRight
        {
            get { return this.topRight; }
            set
            {
                if (this.topRight == value)
                    return;
                this.topRight = value;
                OnPropertyChanged();
                OnPropertyChangedDirect(ViewPortProperty);
            }
        }

        public double ScaleA
        {
            get { return this.scaleA; }
            set
            {
                if (this.scaleA == value)
                    return;
                this.scaleA = value;
                OnPropertyChanged();
                if (ScalingType == ScalingType.Absolute)
                    OnPropertyChangedDirect(ViewPortProperty);
            }
        }

        public double ScaleB
        {
            get { return this.scaleB; }
            set
            {
                if (this.scaleB == value)
                    return;
                this.scaleB = value;
                OnPropertyChanged();
                if (ScalingType == ScalingType.Absolute)
                    OnPropertyChangedDirect(ViewPortProperty);
            }
        }

        public double Dpi
        {
            get { return this.dpi; }
            set
            {
                if (this.dpi == value)
                    return;
                this.dpi = value;
                OnPropertyChanged();
                if (PlotType == PlotType.File && ScalingType == ScalingType.Absolute)
                    OnPropertyChangedDirect(ViewPortProperty);
            }
        }

        public PageSize PageSize
        {
            get { return this.pageSize; }
            set
            {
                if (this.pageSize == value)
                    return;
                this.pageSize = value;
                OnPropertyChanged();
                OnPropertyChangedDirect(ViewPortProperty);
                UpdatePreviewSize();
            }
        }

        public Visibility PrintOptionsVisibility
        {
            get { return this.printOptVis; }
            private set
            {
                if (this.printOptVis == value)
                    return;
                this.printOptVis = value;
                OnPropertyChanged();
                UpdatePreviewSize();
            }
        }

        public Visibility FileOptionsVisibility
        {
            get { return this.fileOptVis; }
            private set
            {
                if (this.fileOptVis == value)
                    return;
                this.fileOptVis = value;
                OnPropertyChanged();
                UpdatePreviewSize();
            }
        }

        public int PixelWidth
        {
            get { return this.pixelWidth; }
            set
            {
                if (this.pixelWidth == value)
                    return;
                this.pixelWidth = value;
                OnPropertyChanged();
                OnPropertyChangedDirect(ViewPortProperty);
                UpdatePreviewSize();
            }
        }

        public int PixelHeight
        {
            get { return this.pixelHeight; }
            set
            {
                if (this.pixelHeight == value)
                    return;
                this.pixelHeight = value;
                OnPropertyChanged();
                OnPropertyChangedDirect(ViewPortProperty);
                UpdatePreviewSize();
            }
        }

        public ViewPort ViewPort
        {
            get
            {
                ViewPort vp;
                double desiredHeight, desiredWidth;
                switch (PlotType)
                {
                    case PlotType.File:
                        desiredHeight = PixelHeight;
                        desiredWidth = PixelWidth;
                        break;
                    case PlotType.Print:
                        desiredHeight = PlotDialog.GetHeight(PageSize);
                        desiredWidth = PlotDialog.GetWidth(PageSize);
                        break;
                    default:
                        throw new InvalidOperationException("unsupported plot type");
                }

                switch (ViewportType)
                {
                    case ViewportType.Extents:
                        vp = Drawing.ShowAllViewPort(
                            ActiveViewPort.Sight,
                            ActiveViewPort.Up,
                            (int)desiredWidth,
                            (int)desiredHeight,
                            pixelBuffer: 0);
                        break;
                    case ViewportType.Window:
                        vp = new ViewPort(BottomLeft, ActiveViewPort.Sight, ActiveViewPort.Up, TopRight.Y - BottomLeft.Y);
                        break;
                    default:
                        throw new InvalidOperationException("unsupported viewport type");
                }

                switch (ScalingType)
                {
                    case ScalingType.Absolute:
                        var dpi = PlotType == PlotType.File ? Dpi : 1.0;
                        vp = vp.Update(viewHeight: desiredHeight * ScaleB / ScaleA / dpi);
                        break;
                    case ScalingType.ToFit:
                        break;
                    default:
                        throw new InvalidOperationException("unsupported scaling type");
                }

                return vp;
            }
        }

        public double PreviewWidth
        {
            get { return previewWidth; }
            set
            {
                if (previewWidth == value)
                    return;
                previewWidth = value;
                OnPropertyChanged();
            }
        }

        public double PreviewHeight
        {
            get { return previewHeight; }
            set
            {
                if (previewHeight == value)
                    return;
                previewHeight = value;
                OnPropertyChanged();
            }
        }

        public double MaxPreviewWidth
        {
            get { return this.maxPreviewWidth; }
            set
            {
                if (this.maxPreviewWidth == value)
                    return;
                this.maxPreviewWidth = value;
                OnPropertyChanged();
                UpdatePreviewSize();
            }
        }

        public double MaxPreviewHeight
        {
            get { return this.maxPreviewHeight; }
            set
            {
                if (this.maxPreviewHeight == value)
                    return;
                this.maxPreviewHeight = value;
                OnPropertyChanged();
                UpdatePreviewSize();
            }
        }

        public ViewPort ActiveViewPort
        {
            get { return activeViewPort; }
            set
            {
                if (activeViewPort == value)
                    return;
                activeViewPort = value;
                OnPropertyChanged();
                OnPropertyChangedDirect(ViewPortProperty);
            }
        }

        public PageSize[] AvailablePageSizes
        {
            get { return new[] { PageSize.Letter, PageSize.Landscape, PageSize.Legal }; }
        }

        public PlotDialogViewModel(IWorkspace workspace)
        {
            this.workspace = workspace;
            Drawing = new Drawing();
            DefaultColor = CadColor.White;
            PlotType = PlotType.File;
            FileName = string.Empty;
            ViewportType = ViewportType.Extents;
            ScalingType = ScalingType.ToFit;
            ColorMapType = ColorMapType.DrawingDefault;
            BottomLeft = Point.Origin;
            TopRight = Point.Origin;
            ScaleA = 1.0;
            ScaleB = 1.0;
            Dpi = 300.0;
            PageSize = PageSize.Letter;
            PixelWidth = 800;
            PixelHeight = 600;
            MaxPreviewWidth = 300;
            MaxPreviewHeight = 300;

            workspace.SettingsManager.PropertyChanged += SettingsManager_PropertyChanged;
            SettingsManager_PropertyChanged(this, new PropertyChangedEventArgs(nameof(ISettingsManager.BackgroundColor)));
        }

        private void SettingsManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISettingsManager.BackgroundColor))
            {
                DefaultColor = workspace.SettingsManager.BackgroundColor.GetAutoContrastingColor();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string property = "")
        {
            OnPropertyChangedDirect(property);
        }

        protected void OnPropertyChangedDirect(string property)
        {
            var changed = PropertyChanged;
            if (changed != null)
                changed(this, new PropertyChangedEventArgs(property));
        }

        private void UpdatePreviewSize()
        {
            double width, height;
            if (PlotType == PlotType.Print)
            {
                width = PlotDialog.GetWidth(PageSize);
                height = PlotDialog.GetHeight(PageSize);
            }
            else
            {
                width = PixelWidth;
                height = PixelHeight;
            }

            if (width > height)
            {
                PreviewWidth = MaxPreviewWidth;
                PreviewHeight = (height / width) * MaxPreviewHeight;
            }
            else
            {
                PreviewHeight = MaxPreviewHeight;
                PreviewWidth = (width / height) * MaxPreviewWidth;
            }
        }
    }
}
