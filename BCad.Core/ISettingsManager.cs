﻿using System.ComponentModel;
using Media = System.Windows.Media;

namespace BCad
{
    public interface ISettingsManager : INotifyPropertyChanged
    {
        string LayerDialogId { get; set; }
        string ViewControlId { get; set; }
        string ConsoleControlId { get; set; }
        double SnapPointDistance { get; set; }
        double SnapPointSize { get; set; }
        double ObjectSelectionRadius { get; set; }
        bool PointSnap { get; set; }
        bool AngleSnap { get; set; }
        bool Ortho { get; set; }
        double SnapAngleDistance { get; set; }
        double[] SnapAngles { get; set; }
        KeyboardShortcut AngleSnapShortcut { get; set; }
        KeyboardShortcut PointSnapShortcut { get; set; }
        KeyboardShortcut OrthoShortcut { get; set; }
        Media.Color BackgroundColor { get; set; }
        Media.Color SnapPointColor { get; set; }
    }
}
