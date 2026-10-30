using System;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public class MoveThumb : Thumb
    {
        private bool _isDragging;

        public MoveThumb()
        {
            DragDelta += MoveThumb_DragDelta;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _isDragging = false;
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            var vm = DataContext as DiagramNodeViewModel;
            if (vm == null || !vm.HasAction || _isDragging)
                return;

            vm.ExecuteAction();
            e.Handled = true;
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var vm = DataContext as DiagramNodeViewModel;

            if (vm == null)
                return;

            _isDragging = true;

            vm.X = Math.Max(0, vm.X + e.HorizontalChange);
            vm.Y = Math.Max(0, vm.Y + e.VerticalChange);
        }
    }
}
