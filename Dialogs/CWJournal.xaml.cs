using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
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
using System.Windows;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;

namespace UnicontaClient.Pages
{
    public partial class CWJournal : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        [InputFieldData]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        public string Journal { get { return _Journal; } set { _Journal = value; } }
        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime Date { get { return _Date; } set { _Date = value; } }
        [InputFieldData]
        [Display(Name = "AssignVoucherNumber", ResourceType = typeof(InputFieldDataText))]
        public bool AddVoucherNumber { get; set; }

        [InputFieldData]
        [Display(Name = "Simulation", ResourceType = typeof(InputFieldDataText))]
        public bool IsSimulation { get { return !post; } set { post = !value; } }
        [InputFieldData]
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public string comments { get { return _comment; } set { _comment = value; } }
        [InputFieldData]
        [Display(Name = "Transfer", ResourceType = typeof(InputFieldDataText))]
        public int lastTransfer { get { return _lastTransfer; } set { _lastTransfer = value; } }

        [InputFieldData]
        [Display(Name = "Credit", ResourceType = typeof(InputFieldDataText))]
        public bool IsCreditAmount { get { return _IsCreditAmount; } set { _IsCreditAmount = value; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrWorkSpace))]
        [InputFieldData]
        [Display(Name = "WorkSpace", ResourceType = typeof(ProjectTransClientText))]
        public string Workspace { get { return _Workspace; } set { _Workspace = value; } }

        public bool OnlyApproved;
        public bool OnlyCurrentRecord;
        bool isDateTime;

        bool post;

        static string _Journal, _comment, _Workspace;
        static DateTime _Date;
        static bool _IsCreditAmount, _useStaticValues;
        static int _lastTransfer;

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
        protected override bool UseStaticValues { get { return _useStaticValues; } }

        public CWJournal(CrudAPI api, bool showDateTime = false)
        {
            post = false;
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("Journal");
            this.SizeToContent = SizeToContent.Height;
            lookupWorkspace.api = lookupJournal.api = api;
            isDateTime = showDateTime;
            cbtype.ItemsSource = AppEnums.DebitCreditType.Values;
            string[] strTransfer = new string[]{
                Uniconta.ClientTools.Localization.lookup("All"),
                Uniconta.ClientTools.Localization.lookup("Approved"),
                Uniconta.ClientTools.Localization.lookup("CurrentRecord")
            };
            cbTransfer.ItemsSource = strTransfer;
            this.Loaded += CW_Loaded;
            ShowComments(false);
        }

        public void ShowComments(bool show)
        {
            if (show)
            {
                cbkAssignVouNo.Visibility = Visibility.Collapsed;
                txtAssignVoucherNumber.Visibility = Visibility.Collapsed;
                chkSimulation.Visibility = tblComments.Visibility = txtComments.Visibility = txtSimulation.Visibility = Visibility.Visible;
                rowComment.Height = new GridLength(30d);
            }
            else
            {
                cbkAssignVouNo.Visibility = Visibility.Visible;
                txtAssignVoucherNumber.Visibility = Visibility.Visible;
                tblComments.Visibility = txtComments.Visibility = chkSimulation.Visibility =
                txtSimulation.Visibility = Visibility.Collapsed;
                rowComment.Height = new GridLength(0d);
            }
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            if (isDateTime)
            {
                this.txtDate.Visibility = Visibility.Visible;
                this.dpDate.Visibility = Visibility.Visible;
            }

            cbTransfer.SelectedIndex = lastTransfer;
            cbtype.SelectedIndex = IsCreditAmount ? 1 : 0;
            Dispatcher.BeginInvoke(new Action(() => { lookupJournal.Focus(); }));
            if (!lookupWorkspace.api.CompanyEntity.Project)
            {
                lookupWorkspace.Visibility = tblWorkspace.Visibility = Visibility.Collapsed;
                rowWorkspace.Height = new GridLength(0d);
            }
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
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            lastTransfer = cbTransfer.SelectedIndex;
            if (isDateTime)
                Date = dpDate.DateTime;

            _useStaticValues = true;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void cbtype_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            IsCreditAmount = cbtype.SelectedIndex == 1;
        }

        private void cbTransfer_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var SelectedIndex = cbTransfer.SelectedIndex;
            if (SelectedIndex == -1) return;
            OnlyApproved = SelectedIndex == 1;
            OnlyCurrentRecord = SelectedIndex == 2;
        }
    }
}

