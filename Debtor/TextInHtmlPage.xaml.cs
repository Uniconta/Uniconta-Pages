using UnicontaClient.Models;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.RichEdit;
using DevExpress.Xpf.RichEdit.Localization;
using DevExpress.XtraReports.Design.CodeCompletion;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraRichEdit.Localization;
using DevExpress.XtraRichEdit.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class TextInHtmlPage : ControlBasePage
    {
        ObservableCollection<String> externTypes;
        public ObservableCollection<String> ExternTypes { get { return externTypes; } set { value = externTypes; } }

        ObservableCollection<String> properties;
        public ObservableCollection<String> Properties { get { return properties; } set { value = properties; } }

        public override string NameOfControl { get { return TabControls.TextInHtmlPage; } }
        public TextInHtmlPage(string html) : base(null)
        {
           // XtraRichEditLocalizer.Active = new UnicontaRichEditLocalizer();
           // XpfRichEditLocalizer.Active = new UnicontaXPfRichEditLocalizer();
            InitializeComponent();
            MainControl = txtHtmlControl;
            BusyIndicator = busyIndicator;
            txtHtmlControl.Loaded += TxtHtmlControl_Loaded;
            txtHtmlControl.HtmlText = html;
            externTypes = new ObservableCollection<string> { "Debtor", "DebtorInvoice", "Creditor", "CreditorInvoice", "Employee", "Contact" };
            properties = new ObservableCollection<string>(UtilFunctions.GetAllDisplayPropertyNames(typeof(DebtorClient), api.CompanyEntity, false, false));
            this.DataContext = this;
        }

        private void TxtHtmlControl_Loaded(object sender, RoutedEventArgs e)
        {
            var service = txtHtmlControl.GetService<IRichEditCommandFactoryService>();
            if (service == null)
            {
                dockCtrl.CloseDockItem();
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("HtmlEditorError"), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }
            CustomRichEditCommandFactoryService commandFactory = new CustomRichEditCommandFactoryService(txtHtmlControl, service);
            txtHtmlControl.RemoveService(typeof(IRichEditCommandFactoryService));
            txtHtmlControl.AddService(typeof(IRichEditCommandFactoryService), commandFactory);
            txtHtmlControl.ReplaceService<ISyntaxHighlightService>(new CustomHtmlSyntaxHighLightService(txtHtmlControl));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            dockCtrl?.CloseDockItem();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHtmlControl.Text) || string.IsNullOrEmpty(txtHtmlControl.Text))
                return;
            var htmlScript = Encoding.UTF8.GetBytes(txtHtmlControl.HtmlText);
            globalEvents.OnRefresh(TabControls.TextInHtmlPage, htmlScript);
            dockCtrl?.CloseDockItem();
        }

        private void txtHtmlControl_AutoCorrect(object sender, DevExpress.XtraRichEdit.AutoCorrectEventArgs e)
        {
            AutoCorrectInfo info = e.AutoCorrectInfo;
            e.AutoCorrectInfo = null;
            if (info.Text.Length <= 0 || !info.Text.Contains(">"))
                return;
            if (info.Text[0] == '>')
            {   
                for (;;)
                {
                    if (!info.DecrementStartPosition())
                        return;
                    if (info.Text[0] == '<')
                    {
                        string replaceString = info.Text + info.Text.Insert(1, "/");
                        if (!String.IsNullOrEmpty(replaceString))
                        {
                            info.ReplaceWith = replaceString;
                            e.AutoCorrectInfo = info;
                        }
                        return;
                    }
                }
            }
        }

        private void txtHtmlControl_UnhandledException(object sender, RichEditUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DevExpress.XtraRichEdit.API.Native.DocumentPosition pos = txtHtmlControl.Document.CaretPosition;
            SubDocument doc = pos.BeginUpdateDocument();
            var selectedText = property;
            string propName = string.Concat("{", externType, ".", selectedText, "}");
            if (string.IsNullOrEmpty(txtHtmlControl.Text))
                txtHtmlControl.Text = propName + "    ";
            else
            {
                doc.InsertText(pos, propName + "    ");
                pos.EndUpdateDocument(doc);
                var position = txtHtmlControl.Document.CreatePosition(pos.ToInt() + propName.Length);
                txtHtmlControl.Document.CaretPosition = pos;
            }
            txtHtmlControl.Focus();
        }

        string externType;
        private void cmbExternType_EditValueChanged(object sender, RoutedEventArgs e)
        {
            Type type;
            var item = sender as BarEditItem;
            if (item != null)
            {
                var name = item.EditValue;
                externType = name.ToString();
                properties.Clear();
                if (name.ToString() == "Debtor")
                    type = typeof(DebtorClient);
                else if (name.ToString() == "DebtorInvoice")
                    type = typeof(DebtorInvoiceClient);
                else if (name.ToString() == "Creditor")
                    type = typeof(CreditorClient);
                else if (name.ToString() == "CreditorInvoice")
                    type = typeof(CreditorInvoiceClient);
                else if (name.ToString() == "Employee")
                    type = typeof(EmployeeClient);
                else 
                    type = typeof(ContactClient);

                foreach (var field in UtilFunctions.GetAllDisplayPropertyNames(type, api.CompanyEntity, false, false))
                    properties.Add(field);
            }
        }

        string property;
        private void cmbProperties_EditValueChanged(object sender, RoutedEventArgs e)
        {
            var item = sender as BarEditItem;
            if (item != null)
            {
                var name = item.EditValue;
                property = name.ToString();
            }
        }
    }
}
