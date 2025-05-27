using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWUpdateBOMPrices : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "UpdateCost", ResourceType = typeof(InputFieldDataText))]
        public bool UpdateCost { get; set; }
        [InputFieldData]
        [Display(Name = "UpdateSales1", ResourceType = typeof(InputFieldDataText))]
        public bool UpdateSales1 { get; set; }
        [InputFieldData]
        [Display(Name = "UpdateSales2", ResourceType = typeof(InputFieldDataText))]
        public bool UpdateSales2 { get; set; }
        [InputFieldData]
        [Display(Name = "UpdateSales3", ResourceType = typeof(InputFieldDataText))]
        public bool UpdateSales3 { get; set; }

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
        public CWUpdateBOMPrices()
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("UpdateOBJ"), Uniconta.ClientTools.Localization.lookup("BOMPrices")) ;
            txtUpdateCost.Text = string.Format(Uniconta.ClientTools.Localization.lookup("UpdateOBJ"), Uniconta.ClientTools.Localization.lookup("Cost"));
            txtUpdateSales1.Text = string.Format(Uniconta.ClientTools.Localization.lookup("UpdateOBJ"), string.Format("{0} 1", Uniconta.ClientTools.Localization.lookup("SalesPrice")));
            txtUpdateSales2.Text = string.Format(Uniconta.ClientTools.Localization.lookup("UpdateOBJ"), string.Format("{0} 2", Uniconta.ClientTools.Localization.lookup("SalesPrice")));
            txtUpdateSales3.Text = string.Format(Uniconta.ClientTools.Localization.lookup("UpdateOBJ"), string.Format("{0} 3", Uniconta.ClientTools.Localization.lookup("SalesPrice")));
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }
    }
}
