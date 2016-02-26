﻿using System.Windows;
using micdah.LrControl.Gui.Utils;
using micdah.LrControl.Mapping;
using micdah.LrControl.Mapping.Functions;

namespace micdah.LrControl.Gui
{
    /// <summary>
    ///     Interaction logic for ControllerFunctionView.xaml
    /// </summary>
    public partial class ControllerFunctionView
    {
        public static readonly DependencyProperty ControllerFunctionProperty = DependencyProperty.Register(
            "ControllerFunction", typeof (ControllerFunction), typeof (ControllerFunctionView),
            new PropertyMetadata(default(ControllerFunction)));

        public static readonly DependencyProperty HighlightProperty = DependencyProperty.Register(
            "Highlight", typeof (bool), typeof (ControllerFunctionView), new PropertyMetadata(default(bool)));

        public ControllerFunctionView()
        {
            InitializeComponent();
        }

        public ControllerFunction ControllerFunction
        {
            get { return (ControllerFunction) GetValue(ControllerFunctionProperty); }
            set { SetValue(ControllerFunctionProperty, value); }
        }

        public bool Highlight
        {
            get { return (bool) GetValue(HighlightProperty); }
            set { SetValue(HighlightProperty, value); }
        }

        private void ControllerFunctionView_OnDragEnter(object sender, DragEventArgs e)
        {
            if (ControllerFunction.Assignable)
            {
                Highlight = e.Data.GetDataPresent(typeof(FunctionFactory));
            }
        }

        private void ControllerFunctionView_OnDragLeave(object sender, DragEventArgs e)
        {
            Highlight = false;
        }

        private void ControllerFunctionView_OnDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof (FunctionFactory))) return;

            if (ControllerFunction.Assignable)
            {
                e.Effects = DragDropEffects.Move;
                Highlight = true;
            }
        }

        private void ControllerFunctionView_OnDrop(object sender, DragEventArgs e)
        {
            Highlight = false;

            // Verify drop object contains needed object
            if (!e.Data.GetDataPresent(typeof (FunctionFactory))) return;
            var functionFactory = (FunctionFactory) e.Data.GetData(typeof (FunctionFactory));

            // Verify we have all needed parameters
            var moduleGroup = this.FindParent<ModuleGroupView>()?.ModuleGroup;
            var functionGroup = this.FindParent<FunctionGroupView>()?.FunctionGroup;
            var controllerFunction = ControllerFunction;
            if (moduleGroup == null || functionGroup == null || controllerFunction == null) return;

            if (moduleGroup.CanAssignFunction(controllerFunction.Controller, functionGroup.IsGlobal))
            {
                ControllerFunction.Function = functionFactory.CreateFunction();
                moduleGroup.RecalculateControllerFunctionState();
            }
        }

        private void DeleteFunctionButton_OnClick(object sender, RoutedEventArgs e)
        {
            var moduleGroup = this.FindParent<ModuleGroupView>()?.ModuleGroup;
            if (moduleGroup != null)
            {
                ControllerFunction.Function = null;
                moduleGroup.RecalculateControllerFunctionState();
            }
        }
    }
}