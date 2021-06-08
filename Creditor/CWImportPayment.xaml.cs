using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWImportPayment : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.CreditorPaymentFormat))]
        public CreditorPaymentFormatClient PaymentFormat { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.BankStatement))]
        public BankStatement BankAccount { get; set; }

        public ExportFormatType FormatType { get; set; }
        public bool includeAllBlankFields;

        public Uniconta.DataModel.UserPlugin userPlugin = null;
        public string FileOption { get; set; }
        CrudAPI Capi;



        public CWImportPayment(CrudAPI api)
        {
            Capi = api;
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("CreatePaymentsFile");
            lePaymentFormat.api = api;
            //leBankAccount.api = api;
            SetExportFormat();
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }
        void SetExportFormat()
        {
            List<string> formatOption = Enum.GetNames(typeof(ExportFormatType)).ToList();
#if !SILVERLIGHT
            plugins = Plugin.pluginList as Uniconta.DataModel.UserPlugin[];
            if (plugins != null)
            {
                foreach(var item in plugins.Where(p => p._Control == "Payments"))
                {
                    if(string.IsNullOrEmpty(item._Prompt))
                    formatOption.Add(item._Control);
                    else
                    formatOption.Add(item._Prompt);
                }
            }
#endif
            //cmbExportFormat.ItemsSource = formatOption;
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                if (string.IsNullOrWhiteSpace(lePaymentFormat.Text))
                    lePaymentFormat.Focus();
                else
                    OKButton.Focus();
            }));
            SetDefaultPaymentFormat();
        }
        async void SetDefaultPaymentFormat()
        {
            var cache = Capi.CompanyEntity.GetCache(typeof(CreditorPaymentFormat));
            if (cache == null)
                cache = await Capi.CompanyEntity.LoadCache(typeof(CreditorPaymentFormat), Capi);
            foreach (var r in cache.GetRecords)
            {
                var rec = r as CreditorPaymentFormat;
                if (rec != null && rec._Default)
                {
                    lePaymentFormat.SelectedItem = rec;
                    break;
                }
            }
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }
        Uniconta.DataModel.UserPlugin[] plugins;

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentFormat = lePaymentFormat.SelectedItem as CreditorPaymentFormatClient;
            if (PaymentFormat != null)
            {
                if (PaymentFormat._ExportFormat == (byte)ExportFormatType.NETS_Norge)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NotActive"), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }

                FormatType = PaymentFormat.PaymentMethod;
            }
            else
            {
                UnicontaMessageBox.Show("Please Select a format for your file", Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
           }
#if !SILVERLIGHT
                if (plugins != null && FileOption != null)
                userPlugin = plugins.FirstOrDefault(p => p._Prompt == FileOption);
#endif
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

    }
}

