using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.Common.Utility;
using System.ComponentModel.DataAnnotations;
using System.Security.AccessControl;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWSearchGLTrans : ChildWindow
    {
        CrudAPI api;

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string Account { get; set; }
        public int Voucher { get; set; }
        public int JournalPostedId { get; set; }
        public string Text { get; set; }

        [Display(Name = "FromDate", ResourceType = typeof(InputFieldDataText))]
        public static DateTime FromDate { get; set; } = DateTime.Now.AddDays(-14);

        [InputFieldData]
        [Display(Name = "ToDate", ResourceType = typeof(InputFieldDataText))]
        public static DateTime ToDate { get; set; } = DateTime.Now;
        public static bool includeWithoutAttachment { get; set; } = true;
        static double windowWidth = 1050;

        public GLTransClient SelectedRow;
        public CWSearchGLTrans(CrudAPI api)
        {
            this.api = api;
            this.DataContext = this;
            InitializeComponent();
            leAccount.api = api;
            dgGLTrans.api = api;
            this.Title = Uniconta.ClientTools.Localization.lookup("LedgerTransactions");
            txtJournalPostedId.Validate += Txt_Validate;
            txtVoucher.Validate += Txt_Validate;
            this.Width = windowWidth;

        }

        private void Txt_Validate(object sender, DevExpress.Xpf.Editors.ValidationEventArgs e)
        {
            var txtBox = sender as TextEditor;
            if (txtBox.Text == string.Empty)
                txtBox.Text = "0";
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
                if (btnSelect.IsFocused)
                    btnSelect_Click(null, null);
                else if (btnCancel.IsFocused)
                    SetDialogResult(false);
            }
        }
        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            SelectedRow = dgGLTrans.SelectedItem as GLTransClient;
            if (SelectedRow != null)
                SetDialogResult(true);
            else
                SetDialogResult(false);
            windowWidth = this.ActualWidth;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            var filters = new List<PropValuePair>();
            var dateString = string.Concat(dateFrm.DateTime == DateTime.MinValue ? "" : dateFrm.DateTime.ToShortDateString(), "..", dateTo.DateTime == DateTime.MinValue ? "" : dateTo.DateTime.ToShortDateString());
            var dateFilter = PropValuePair.GenereteWhereElements("Date", typeof(DateTime), dateString);
            filters.Add(dateFilter);

            if (Voucher > 0)
            {
                var voucherFilter = PropValuePair.GenereteWhereElements("Voucher", typeof(int), NumberConvert.ToString(Voucher));
                filters.Add(voucherFilter);
            }
            if (JournalPostedId > 0)
            {
                var JournalPostedIdFilter = PropValuePair.GenereteWhereElements("JournalPostedId", typeof(int), NumberConvert.ToString(JournalPostedId));
                filters.Add(JournalPostedIdFilter);
            }
            if (!string.IsNullOrEmpty(Account))
            {
                var accountFilter = PropValuePair.GenereteWhereElements("Account", typeof(string), Account);
                filters.Add(accountFilter);
            }
            if (!string.IsNullOrEmpty(Text))
            {
                var textFilter = PropValuePair.GenereteWhereElements("Text", typeof(string), "*" + Text + "*");
                filters.Add(textFilter);
            }
            if (chkWithoutAttachment.IsChecked == true)
            {
                var docRefFilter = PropValuePair.GenereteWhereElements("DocumentRef", "null", CompareOperator.Equal);
                filters.Add(docRefFilter);
            }
            // var trans = await api.Query<GLTransClient>(new GLTransClient(), null, filters);
            dgGLTrans.Filter(filters);
        }
    }
}

