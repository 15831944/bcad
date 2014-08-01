﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BCad.Collections;
using BCad.Entities;
using BCad.EventArguments;

#if BCAD_METRO
using Windows.UI.Xaml.Controls;
#endif

#if BCAD_WPF
using System.Windows.Controls;
#endif

namespace BCad.UI.View
{
    public partial class XamlRenderer : UserControl
    {
        private IWorkspace Workspace;
        private RenderCanvasViewModel viewModel = new RenderCanvasViewModel();

        public void Initialize(IWorkspace workspace, IViewControl viewControl)
        {
            this.Workspace = workspace;
            this.ViewControl = viewControl;

            viewModel.SelectedEntities = workspace.SelectedEntities;
            DataContext = viewModel;
            Workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
            Workspace.SettingsManager.PropertyChanged += SettingsManager_PropertyChanged;
            Workspace.SelectedEntities.CollectionChanged += SelectedEntities_CollectionChanged;
            Workspace.RubberBandGeneratorChanged += Workspace_RubberBandGeneratorChanged;

            this.Loaded += (_, __) =>
                {
                    foreach (var setting in new[] { Constants.BackgroundColorString })
                        SettingsManager_PropertyChanged(Workspace.SettingsManager, new PropertyChangedEventArgs(setting));
                    Workspace_WorkspaceChanged(Workspace, WorkspaceChangeEventArgs.Reset());
                };
        }

        void Workspace_RubberBandGeneratorChanged(object sender, EventArgs e)
        {
            viewModel.RubberBandGenerator = Workspace.RubberBandGenerator;
        }

        public void UpdateRubberBandLines()
        {
            viewModel.CursorPoint = ViewControl.GetCursorPoint();
        }

        private void SettingsManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case Constants.BackgroundColorString:
                    viewModel.BackgroundColor = Workspace.SettingsManager.BackgroundColor;
                    break;
                case Constants.ColorMapString:
                    viewModel.ColorMap = Workspace.SettingsManager.ColorMap;
                    break;
            }
        }

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.IsActiveViewPortChange)
            {
                viewModel.ViewPort = Workspace.ActiveViewPort;
            }
            if (e.IsDrawingChange)
            {
                viewModel.Drawing = Workspace.Drawing;
            }
        }

        private void SelectedEntities_CollectionChanged(object sender, EventArgs e)
        {
            viewModel.SelectedEntities = Workspace.SelectedEntities;
        }
    }
}