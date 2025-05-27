using UnicontaClient.Models;
using DevExpress.Xpf.Bars;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraRichEdit.Services;
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using DevExpress.Xpf.CodeView;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class TextInHtmlPage : ControlBasePage
    {
        public ObservableCollection<string> ExternTypes { get { return new ObservableCollection<string> { "Debtor", "DebtorInvoice", "Creditor", "CreditorInvoice", "Employee", "Contact" }; } }

        ObservableCollection<string> properties;
        public ObservableCollection<string> Properties { get { return properties; } set { value = properties; } }

        public override string NameOfControl { get { return TabControls.TextInHtmlPage; } }

        private string _htmlContent;
        public string HtmlContent { get { return _htmlContent; } set { if (_htmlContent != value) { _htmlContent = value; } } }

        public TextInHtmlPage(string html) : base(null)
        {
            InitializeComponent();
            MainControl = txtHtmlControl;
            txtHtmlControl.Loaded += TxtHtmlControl_Loaded;
            HtmlContent = html;
            properties = new ObservableCollection<string>();
            this.DataContext = this;
        }

        private void TxtHtmlControl_Loaded(object sender, RoutedEventArgs e)
        {
            var service = txtHtmlControl.GetService<IRichEditCommandFactoryService>();
            if (service == null)
            {
                CloseDockItem();
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("HtmlEditorError"), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDockItem();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_htmlContent) || string.IsNullOrEmpty(_htmlContent))
                return;

            var htmlScript = Encoding.UTF8.GetBytes(_htmlContent);
            globalEvents.OnRefresh(TabControls.TextInHtmlPage, htmlScript);
            CloseDockItem();
        }

        private void txtHtmlControl_AutoCorrect(object sender, DevExpress.XtraRichEdit.AutoCorrectEventArgs e)
        {
            AutoCorrectInfo info = e.AutoCorrectInfo;
            e.AutoCorrectInfo = null;
            if (info.Text.Length <= 0 || !info.Text.Contains(">"))
                return;
            if (info.Text[0] == '>')
            {
                for (; ; )
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

        private void cmbExternType_EditValueChanged(object sender, RoutedEventArgs e)
        {
            if (sender is BarEditItem item)
            {
                var name = item.EditValue;
                properties.Clear();
                Type type;

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

                properties.AddRange(UtilFunctions.GetAllDisplayPropertyNames(type, api.CompanyEntity, false, false));
            }
        }

        private void btnAdd_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (cmbExternType.EditValue == null || cmbProperties.EditValue == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("PleaseSelectOBJ"), cmbExternType.EditValue == null ? Uniconta.ClientTools.Localization.lookup("Type") :
                    Uniconta.ClientTools.Localization.lookup("Field")), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }

            var pos = txtHtmlControl.Document.CaretPosition;
            SubDocument doc = pos.BeginUpdateDocument();

            string propName = string.Concat("{", cmbExternType.EditValue.ToString(), ".", cmbProperties.EditValue.ToString(), "}");
            if (string.IsNullOrEmpty(_htmlContent))
                _htmlContent = propName + "    ";
            else
            {
                doc.InsertText(pos, propName + "    ");
                pos.EndUpdateDocument(doc);
                var position = txtHtmlControl.Document.CreatePosition(pos.ToInt() + propName.Length);
                txtHtmlControl.Document.CaretPosition = pos;
            }
            txtHtmlControl.Focus();
        }
    }
}
