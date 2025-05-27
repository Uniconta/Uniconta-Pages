using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools;
using UnicontaClient.Controls;
using DevExpress.Xpf.WindowsUI.Navigation;
using Uniconta.ClientTools.Controls;
using Uniconta.Common;
using NPOI.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLTransAmountClient: GLTransClient
    {
       public double NewAmount { get; set; }
    }
    public partial class CwChangeMultipleAmount : ChildWindow
    {
        List<GLTransAmountClient> gLTransAmounts;
        IEnumerable<GLTransClient> glTrans;
        public List<double> NewAmounts = null;
        public CwChangeMultipleAmount(IEnumerable<GLTransClient> glTrans)
        {
            gLTransAmounts = new List<GLTransAmountClient>();
            this.glTrans = glTrans;
            InitializeComponent();
            this.DataContext = this;
            this.Title = String.Format(Uniconta.ClientTools.Localization.lookup("ChangeOBJ"),
                Uniconta.ClientTools.Localization.lookup("Amount"));
            colNewAmount.Header = string.Format(Uniconta.ClientTools.Localization.lookup("NewOBJ"), Uniconta.ClientTools.Localization.lookup("Amount"));
            CreateSource(glTrans);
        }

        void CreateSource(IEnumerable<GLTransClient> glTrans)
        {
            foreach (var tr in glTrans)
            {
                var gLTransAmount = new GLTransAmountClient();
                StreamingManager.Copy(tr, gLTransAmount);
                gLTransAmount.NewAmount = tr.Amount;
                gLTransAmounts.Add(gLTransAmount);
            }
            dgTrans.ItemsSource = gLTransAmounts;
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            NewAmounts = new List<double>();
            foreach (var tr in gLTransAmounts)
            {
                NewAmounts.Add(tr.NewAmount);
            }
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
