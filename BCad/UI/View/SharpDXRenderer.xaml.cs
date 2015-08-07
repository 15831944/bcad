﻿using System;
using System.ComponentModel;
using System.Windows.Controls;
using BCad.EventArguments;
using BCad.Services;

namespace BCad.UI.View
{
    /// <summary>
    /// Interaction logic for SharpDXRenderer.xaml
    /// </summary>
    public partial class SharpDXRenderer : UserControl, IRenderer
    {
        private IWorkspace workspace;
        private IViewControl viewControl;
        private CadGame game;
        private RenderCanvasViewModel viewModel = new RenderCanvasViewModel();

        public SharpDXRenderer()
        {
            InitializeComponent();
        }

        public SharpDXRenderer(IViewControl viewControl, IWorkspace workspace, IInputService inputService)
            : this()
        {
            this.workspace = workspace;
            this.viewControl = viewControl;
            game = new CadGame(workspace, viewControl);
            game.Run(surface);

            viewModel = new RenderCanvasViewModel();
            DataContext = viewModel;
            workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
            workspace.SettingsManager.PropertyChanged += SettingsManager_PropertyChanged;
            workspace.SelectedEntities.CollectionChanged += SelectedEntities_CollectionChanged;
            workspace.RubberBandGeneratorChanged += RubberBandGeneratorChanged;

            this.Loaded += (_, __) =>
            {
                foreach (var setting in new[] { Constants.BackgroundColorString })
                    SettingsManager_PropertyChanged(workspace.SettingsManager, new PropertyChangedEventArgs(setting));
                Workspace_WorkspaceChanged(workspace, WorkspaceChangeEventArgs.Reset());
            };

            this.SizeChanged += (_, e) => game.Resize((int)e.NewSize.Width, (int)e.NewSize.Height);
        }

        private void RubberBandGeneratorChanged(object sender, EventArgs e)
        {
            viewModel.RubberBandGenerator = workspace.RubberBandGenerator;
        }

        public void UpdateRubberBandLines()
        {
            game.UpdateRubberBandLines();
            viewModel.CursorPoint = viewControl.GetCursorPoint().Result;
        }

        private void SettingsManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case Constants.BackgroundColorString:
                    viewModel.BackgroundColor = workspace.SettingsManager.BackgroundColor;
                    break;
            }
        }

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.IsActiveViewPortChange)
            {
                viewModel.ViewPort = workspace.ActiveViewPort;
            }
            if (e.IsDrawingChange)
            {
                viewModel.Drawing = workspace.Drawing;
            }
        }

        private void SelectedEntities_CollectionChanged(object sender, EventArgs e)
        {
            viewModel.SelectedEntities = workspace.SelectedEntities;
        }
    }
}
