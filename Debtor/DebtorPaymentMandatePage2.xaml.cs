using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorPaymentMandatePage2 : FormBasePage
    {
        DebtorPaymentMandateClient editrow;
        SQLCache debtorCache, paymentFormatCache;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override Type TableType { get { return typeof(DebtorPaymentMandateClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorPaymentMandateClient)value; } }

        public DebtorPaymentMandatePage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api, null);
        }

        public DebtorPaymentMandatePage2(UnicontaBaseEntity sourcedata, bool isEdit, UnicontaBaseEntity master = null): base(sourcedata , isEdit)
        {
            InitializeComponent();
            InitPage(api, master);
        }

        public DebtorPaymentMandatePage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi, null);
        }

       

        void InitPage(CrudAPI crudapi, UnicontaBaseEntity master)
        {
            ribbonControl = frmRibbon;
            var Comp = api.CompanyEntity;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as DebtorPaymentMandateClient;
                editrow.SetMaster(master ?? api.CompanyEntity);
                var dc = master as Uniconta.DataModel.DCAccount;
            }
            lookupDCAccount.api = crudapi;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            
            if (editrow.DCAccount == null) 
                liDCAccount.IsEnabled = true;
            else
                liDCAccount.IsEnabled = false;

            if (editrow._Status == MandateStatus.None || editrow._Status == MandateStatus.Unregistered || editrow._Status == MandateStatus.Error)
                liAltMandateId.IsEnabled = true;
            else
                liAltMandateId.IsEnabled = false;


            if (master == null)
            {
                liDCAccount.Visibility = Visibility.Visible;
            }

            SetFields();
        }

        private async void SetFields()
        {
            if (debtorCache == null)
               debtorCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.Debtor), api);

            if (paymentFormatCache == null)
                paymentFormatCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.DebtorPaymentFormat)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.DebtorPaymentFormat), api);

            var debtor = (DebtorClient)debtorCache.Get(editrow._DCAccount);
            var paymentFormat = (DebtorPaymentFormatClient)paymentFormatCache.Get(debtor?.PaymentFormat);

            if (paymentFormat != null && paymentFormat._ExportFormat == (byte)Uniconta.DataModel.DebtorPaymFormatType.SEPA)
            {
                cmbMandateStatus.IsReadOnly = false;
                cmbMandateStatus.IsEnabled = true;
                deActivationDate.IsReadOnly = false;
                deActivationDate.IsEnabled = true;
                deCancellationDate.IsReadOnly = false;
                deCancellationDate.IsEnabled = true;

                grpSEPA.Visibility = Visibility.Visible;
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            debtorCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api).ConfigureAwait(false);
            paymentFormatCache = Comp.GetCache(typeof(Uniconta.DataModel.DebtorPaymentFormat)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.DebtorPaymentFormat), api).ConfigureAwait(false);
        }

        protected override void OnLayoutCtrlLoaded()
        {
            //CreateUserField();
        }
        //void CreateUserField()
        //{
        //    var UserFieldDef = editrow.UserFieldDef();
        //    if (UserFieldDef != null)
        //    {
        //        UserFieldControl.CreateUserFieldOnPage2(layoutItems, UserFieldDef, (RowIndexConverter)this.Resources["RowIndexConverter"], this.api, false);
        //        Layout._SubId = api.CompanyEntity.CompanyId; // User field forms, need to have SubId set to CompanyId
        //    }
        //}

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
            {
                var statusInfoTxt = editrow.StatusInfo;

                if (editrow.MandateId == "0")
                {
                    editrow.StatusInfo = string.Format("({0}) Mandate created", Uniconta.DirectDebitPayment.Common.GetTimeStamp());
                }
                else if (editrow.AltMandateId == null && txtAltMandateId.Text != null)
                {
                    statusInfoTxt = editrow.StatusInfo;
                    statusInfoTxt = string.Format("({0}) Mandate ID changed from {1} -> {2}\n{3}", Uniconta.DirectDebitPayment.Common.GetTimeStamp(), editrow.RowId, txtAltMandateId.Text, statusInfoTxt);
                 }
                else if (editrow.AltMandateId != null && (txtAltMandateId.Text == null || txtAltMandateId.Text == string.Empty))
                {
                    statusInfoTxt = editrow.StatusInfo;
                    statusInfoTxt = string.Format("({0}) Mandate ID changed from {1} -> {2}\n{3}", Uniconta.DirectDebitPayment.Common.GetTimeStamp(), editrow.AltMandateId, editrow.RowId, statusInfoTxt);
                }
                else if (editrow.AltMandateId != txtAltMandateId.Text)
                {
                    statusInfoTxt = editrow.StatusInfo;
                    statusInfoTxt = string.Format("({0}) Mandate ID changed from {1} -> {2}\n{3}", Uniconta.DirectDebitPayment.Common.GetTimeStamp(), editrow.AltMandateId, txtAltMandateId.Text, statusInfoTxt);
                }

                if (statusInfoTxt != null)
                    editrow.StatusInfo = Uniconta.DirectDebitPayment.Common.StatusInfoTruncate(statusInfoTxt);
            }

            frmRibbon_BaseActions(ActionType);
        }
    }
}
