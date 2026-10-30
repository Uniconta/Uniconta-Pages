using System;
using System.Windows;
using System.Windows.Controls;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public partial class ReasonDiagramNodeView : System.Windows.Controls.UserControl
    {
        public ReasonDiagramNodeView()
        {
            InitializeComponent();
        }

        private void Source_Click(object sender, System.Windows.RoutedEventArgs e)
        {
                if (sender is FrameworkElement element
                && element.DataContext is ReasonDiagramNodeViewModel vm)
                {
                    if (vm.SourceTable != null)
                    {
                        PostingTraceDiagramView.JumpToMainTable(new Uniconta.ClientTools.Controls.LookUpTable
                        {
                            TableType = vm.SourceTable,
                            KeyStr = vm.SourceTableKey
                        }, title: vm.SourceRecordInfo);
                    }
                }
        }
    }
}
