using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Xpf.Editors;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.ClientTools.Util;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Controls;
using Uniconta.Common.Utility;
using DevExpress.Data.TreeList;
using NPOI.OpenXmlFormats.Spreadsheet;
using UnicontaClient.Pages.Creditor.Payments;
using Uniconta.API.DebtorCreditor;
using System.Text.RegularExpressions;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreditorPaymentAccountPage : FormBasePage
    {
        CreditorPaymentAccountClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
        }
        public override Type TableType { get { return typeof(CreditorPaymentAccountClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CreditorPaymentAccountClient)value; } }
        bool lookupZipCode = true;
        SQLCache bankRegisterCache;

        public CreditorPaymentAccountPage(UnicontaBaseEntity sourcedata) 
           : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public CreditorPaymentAccountPage(CrudAPI crudApi, UnicontaBaseEntity master)
            : base(crudApi, "")
        {
            InitializeComponent();
            if (master != null)
            {
                editrow = new CreditorPaymentAccountClient();
                editrow.SetMaster(master);
            }
            InitPage(api);
        }
        void InitPage(CrudAPI crudapi)
        {
            layoutControl = layoutItems;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            RibbonBase rb = (RibbonBase)frmRibbon.DataContext;
            editrow.PropertyChanged += Editrow_PropertyChanged;

            var comp = crudapi.CompanyEntity;
            if (LoadedRow == null || !comp.AllowApproval)
                frmRibbon.DisableButtons("Approve");

            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Log");

                if ((int)editrow.Creditor._PaymentMethod >= 2 && (int)editrow.Creditor._PaymentMethod <= 5)
                {
                    StandardPaymentFunctions.ParseOcr(editrow.Creditor._PaymentId, out string fiCreditor, out string fiMask);
                    editrow.FICreditorNumber = fiCreditor;
                    editrow._FIKMask = fiMask;
                }
                else if (editrow.Creditor._PaymentMethod == PaymentTypes.IBAN)
                    editrow.IBAN = editrow.Creditor._PaymentId;
                else if (editrow.Creditor._PaymentMethod == PaymentTypes.VendorBankAccount)
                    editrow.BankAccount = editrow.Creditor._PaymentId;

                editrow.SWIFT = editrow.Creditor.SWIFT;
            }
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Save":
                    if (!string.IsNullOrEmpty(txtIBAN.Text) && !Uniconta.DirectDebitPayment.Common.ValidateIBAN(txtIBAN.Text))
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("InvalidOBJ"),  string.Concat("IBAN-", Uniconta.ClientTools.Localization.lookup("Number"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                        return;
                    }
                    if (api.CompanyEntity.AllowApproval)
                    {
                        editrow._Approved = true;
                        editrow.NotifyPropertyChanged("Status");
                    }
                    frmRibbon_BaseActions(ActionType);
                    break;
                case "Log":
                    AddDockItem(TabControls.CreditorFieldLogPage, editrow, true,"Log",null, new System.Windows.Point(425, 250));
                    break;
                case "Approve":
                    Approve();
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ZipCode")
            {
                if (lookupZipCode)
                {
                    var city = await UtilDisplay.GetCityAndAddress(editrow.ZipCode, editrow.Country);
                    if (city != null)
                    {
                        editrow.City = city[0];
                        var add1 = city[1];
                        if (!string.IsNullOrEmpty(add1))
                            editrow.Address1 = add1;
                        var zip = city[2];
                        if (!string.IsNullOrEmpty(zip) && editrow.ZipCode != zip)
                        {
                            lookupZipCode = false;
                            editrow.ZipCode = zip;
                        }
                    }
                }
                else
                    lookupZipCode = true;
            }
            else if (e.PropertyName == "BankAccount")
            {
                if (editrow.Country != CountryCode.Denmark || editrow.BankAccount == null)
                    return;
                var bankAccount = Regex.Replace(editrow.BankAccount, @"[^0-9]", "");
                if (bankAccount.Length > 0)
                {
                    var banknumber = bankAccount.Substring(0, 4);
                    if (banknumber.Length > 0)
                    {
                        if (bankRegisterCache == null)
                            bankRegisterCache = api.LoadCache(typeof(BankRegister)).Result;

                        var bank = bankRegisterCache.Get(banknumber) as BankRegister;
                        if (bank != null)
                        {
                            editrow.BankName = bank._Name;
                            editrow.Address1 = bank._Address1;
                            editrow.Address2 = bank._Address2;
                            editrow.ZipCode = bank._ZipCode;
                        }
                    }
                }
            }
        }

        private void LiZipCode_OnButtonClicked(object sender)
        {
            var location = editrow.Address1 + "+"  + editrow.Address2 + "+" + editrow.ZipCode + "+" + editrow.City + "+" + editrow.Country;
            Utility.OpenGoogleMap(location);
        }

        private async void Approve()
        {
            if (editrow != null && !editrow._Approved)
            {
                TransactionAPI tranApi = new TransactionAPI(this.api);
                var err = await tranApi.ApprovePaymentAccount(editrow.Account);
                if (err != 0)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup(err.ToString()), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                else
                {
                    editrow._Approved = true;
                    editrow.NotifyPropertyChanged("Status");
                    closePageOnSave = false;
                    var res = await saveForm(false);
                    closePageOnSave = true;
                }
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            bankRegisterCache = Comp.GetCache(typeof(Uniconta.DataModel.BankRegister)) ?? await api.LoadCache(typeof(Uniconta.DataModel.BankRegister)).ConfigureAwait(false);
        }

    }
}
