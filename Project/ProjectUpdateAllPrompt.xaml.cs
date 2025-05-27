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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common.Utility;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Controls;

namespace Uniconta.Client.Pages
{
    public partial class ProjectUpdateAllPrompt :  ChildWindow
    {
        public int InvoiceNumber { get; set; }
        public string InvoiceCategory { get; set; }
        public bool IsSimulation;
        public bool SendByEmail;
        public DateTime GenrateDate;
        CrudAPI api;

        public ProjectUpdateAllPrompt(CrudAPI crudapi,bool IsSimulation = true, string title = "", bool showInputforInvNumber = false, bool askForEmail = false)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("UpdateAll");
            api = crudapi;
            SetItemSource();
            dpDate.DateTime = BasePage.GetSystemDefaultDate();
            if (!IsSimulation)
            {
                RowChk.Height = new GridLength(0);
                if (!string.IsNullOrEmpty(title))
                    this.Title = title;
            }
            if (!showInputforInvNumber)
            {
                RowInvNo.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
                txtInvNumber.Text = string.Empty;
            }
            if (!askForEmail)
            {
                RowSendByEmail.Height = new GridLength(0);
            }

            this.Loaded += CW_Loaded;
        }

        async void SetItemSource()
        {
            var data = await api.Query<PrCategoryClient>();
            cmbCategory.ItemsSource = data;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
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
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var str = txtInvNumber.Text;
            if (!string.IsNullOrEmpty(str) && str != "0")
            {
                var n = NumberConvert.ToInt(str);
                if (n != 0 && n <= int.MaxValue && n >= int.MinValue)
                    InvoiceNumber = (int)n;
                else
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NumberTooBig"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                    return;
                }
            }
            SendByEmail = chkSendEmail.IsChecked.GetValueOrDefault();
            IsSimulation = chkSimulation.IsChecked.GetValueOrDefault();
            GenrateDate = dpDate.DateTime;
            InvoiceCategory = cmbCategory.Text;   
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
