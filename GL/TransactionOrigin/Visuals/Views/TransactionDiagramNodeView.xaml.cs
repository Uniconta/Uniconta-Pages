using DevExpress.XtraSpreadsheet.Model;
using System.Windows;
using System.Windows.Controls;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public partial class TransactionDiagramNodeView : System.Windows.Controls.UserControl
    {
        public TransactionDiagramNodeView()
        {
            InitializeComponent();
        }

        private void Account_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is FrameworkElement element
               && element.DataContext is TransactionDiagramNodeViewModel vm)
            {
                PostingTraceDiagramView.JumpToMainTable(new Uniconta.ClientTools.Controls.LookUpTable
                {
                    TableType = typeof(GLAccountClient),
                    KeyStr = vm.GLAccount?.Account
                }, title: vm.AccountText);
            }
        }
    }
}
