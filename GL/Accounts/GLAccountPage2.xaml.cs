using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using DevExpress.Xpf.Editors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml;
using System.Windows;
using Uniconta.ClientTools.Util;
using UnicontaClient.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class GLAccountPage2 : FormBasePage
    {
        GLAccountClient editrow;
        bool UseVatOperation;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.GLAccountPage2.ToString(); } }
        public override Type TableType { get { return typeof(GLAccountClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (GLAccountClient)value; } }
        bool isCopiedRow = false;
        public GLAccountPage2(UnicontaBaseEntity sourcedata, bool IsEdit)
            : base(sourcedata, IsEdit)
        {
            InitializeComponent();
            isCopiedRow = !IsEdit;
            InitPage(api);
        }
        //use in Insert new record
        public GLAccountPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtnumber, txtnumber);
#endif
        }
        //Initialize page
        void InitPage(CrudAPI crudapi)
        {
            var Comp = crudapi.CompanyEntity;
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                if (!isCopiedRow)
                    editrow = CreateNew() as GLAccountClient;
                for (int n = Comp.NumberOfDimensions; (n > 0); n--)
                    editrow.SetDimUsed(n, true);
            }
            layoutItems.DataContext = editrow;
            PrimoAccountLookupEditor.api = leVat.api = Withholdinglookupeditior.api = VatOprlookupeditior.api = OffsetAccountLookupEditor.api =
            lbPrCategory.api = dim1lookupeditior.api = dim2lookupeditior.api = dim3lookupeditior.api = dim4lookupeditior.api =
            dim5lookupeditior.api = StandardAccountLookupEditor.api = crudapi;
            UseVatOperation = Comp._UseVatOperation;
            if (!UseVatOperation)
                VatOprlookupeditiorItem.Visibility = Visibility.Collapsed;
            if (!Comp._HasWithholding)
                WithholdinglookupeditiorItem.Visibility = Visibility.Collapsed;
            if (!Comp.Project)
                grpProject.Visibility = Visibility.Collapsed;
            if (Comp.NumberOfDimensions == 0)
            {
                usedim.Visibility = Visibility.Collapsed;
                useNewdim.Visibility = Visibility.Collapsed;
            }
            if (Comp._CountryId != CountryCode.Germany)
                liDATEVAuto.Visibility = Visibility.Collapsed;

            SetAccountTotals();
            SetOffsetAccountSource();
            Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            Utility.SetDimensions(crudapi, lblNewdim1, lblNewdim2, lblNewdim3, lblNewdim4, lblNewdim5, dim1lookupeditior, dim2lookupeditior, dim3lookupeditior, dim4lookupeditior, dim5lookupeditior, useNewdim);

            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
            {
                var acc = this.editrow;
                if (acc.RowId == 0)
                {
                    acc._CurBalance = 0;
                    acc._PrevBalance = 0;
                    if (acc._Vat == null && acc.AccountTypeEnum >= GLAccountTypes.BalanceSheet && acc._MandatoryTax == VatOptions.Optional)
                        acc._MandatoryTax = VatOptions.NoVat;
                    if (!UseVatOperation)
                        acc._VatOperation = null;
                }
            }
            else if (ActionType == "Script")
            {
                var cwAddScript = new CWAddScript(api, editrow, editrow.CalculationExpression);
                cwAddScript.Closed += delegate
                {
                    if (cwAddScript.DialogResult == true)
                        editrow.CalculationExpression = cwAddScript.txtScript.Text;
                };
                cwAddScript.Show();
            }
            frmRibbon_BaseActions(ActionType);
        }

        private void cbAccountType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            SetAccountTotals();           
        }

        void EnableScriptButton(bool enable)
        {
            if (enable)
                frmRibbon.EnableButtons("Script");
            else
                frmRibbon.DisableButtons("Script");
        }
        private void SetAccountTotals()
        {
            EnableScriptButton(false);
            if (editrow._AccountType > (byte)GLAccountTypes.CalculationExpression)
            {
                if (editrow._IsDCAccount)
                {
                    grpCustomer.Visibility = Visibility.Visible;
                    editrow._MandatoryTax = VatOptions.NoVat;
                }
                else
                {
                    grpCustomer.Visibility = Visibility.Collapsed;
                }
                grpVat.Visibility = Visibility.Visible;
                liCurrency.Visibility = Visibility.Visible;
            }
            else
            {
                liCurrency.Visibility = grpVat.Visibility = grpCustomer.Visibility = Visibility.Collapsed;
            }

            switch (editrow.AccountTypeEnum)
            {
                case GLAccountTypes.Header:
                case GLAccountTypes.Sum:
                    gpAcTotal.Width = 350;
                    itemSum.Visibility = Visibility.Visible;
                    itemExpression.Visibility = itemPercentage.Visibility = Visibility.Collapsed;
                    PrimoAccountLookupEditor.Visibility = Visibility.Collapsed;
                    break;
                case GLAccountTypes.CalculationExpression:
                    gpAcTotal.Width = 350;
                    itemSum.Visibility = Visibility.Collapsed;
                    itemExpression.Visibility = itemPercentage.Visibility = Visibility.Visible;
                    PrimoAccountLookupEditor.Visibility = Visibility.Collapsed;
                    EnableScriptButton(true);
                    break;
                default:
                    gpAcTotal.Width = 250;
                    itemSum.Visibility = itemExpression.Visibility = itemPercentage.Visibility = Visibility.Collapsed;
                    PrimoAccountLookupEditor.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void cboffsetAccount_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            SetOffsetAccountSource();
        }

        async private void SetOffsetAccountSource()
        {
            Type t = null;
            switch (editrow._DefaultOffsetAccountType)
            {
                case GLJournalAccountType.Finans:
                    t = typeof(GLAccount);
                    break;
                case GLJournalAccountType.Debtor:
                    t = typeof(Debtor);
                    break;
                case GLJournalAccountType.Creditor:
                    t = typeof(Uniconta.DataModel.Creditor);
                    break;
            }
            var api = this.api;
            var Comp = api.CompanyEntity;
            var Cache = Comp.GetCache(t);
            if (Cache == null)
                Cache = await Comp.LoadCache(t, api);

            OffsetAccountLookupEditor.ItemsSource = Cache;
        }
    }
}
