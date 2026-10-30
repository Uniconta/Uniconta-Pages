using System;
using System.Windows;
using System.Windows.Controls;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public partial class VoucherDiagramNodeView : System.Windows.Controls.UserControl
    {
        public VoucherDiagramNodeView()
        {
            InitializeComponent();
        }

        private void DCAccountInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element 
            && element.DataContext is VoucherDiagramNodeViewModel vm)
            {
                Type tableType = null;

                if(vm.DCType == Uniconta.DataModel.GLTransRefType.Debtor)
                {
                    tableType = typeof(DebtorClient);
                }

                if (vm.DCType == Uniconta.DataModel.GLTransRefType.Creditor)
                {
                    tableType = typeof(CreditorClient);
                }

                if (tableType != null)
                {
                    PostingTraceDiagramView.JumpToMainTable(new Uniconta.ClientTools.Controls.LookUpTable
                    {
                        TableType = tableType,
                        KeyStr = vm.DCAccount
                    }, title: vm.DCAccountInfo);
                }
            }
        }
    }
}
