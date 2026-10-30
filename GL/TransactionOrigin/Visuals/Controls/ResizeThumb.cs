using System;
using System.Windows.Controls.Primitives;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public class ResizeThumb : Thumb
    {
        public ResizeThumb()
        {
            DragDelta += ResizeThumb_DragDelta;
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var vm = DataContext as DiagramNodeViewModel;

            if (vm == null)
                return;

            var newWidth = vm.Width + e.HorizontalChange;
            var newHeight = vm.Height + e.VerticalChange;

            vm.Width = Math.Max(vm.MinWidth, newWidth);
            vm.Height = Math.Max(vm.MinHeight, newHeight);
        }
    }
}
