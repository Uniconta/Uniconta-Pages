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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorPaymentMandatePage2 : FormBasePage
    {
        DebtorPaymentMandateClient editrow;
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
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as DebtorPaymentMandateClient;
                editrow.SetMaster(master ?? api.CompanyEntity);
                var dc = master as Uniconta.DataModel.DCAccount;
                //if (dc != null)
                //    editrow._MandateId = dc._Account.ToString(); //TODO:Udfyld evt. MandateId med Kontonummer
            }
            lookupDCAccount.api = crudapi;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            
            if (editrow.MandateId == 0) //TODO:TEST DENNE
                liDCAccount.IsEnabled = true;
            else
                liDCAccount.IsEnabled = false;

            if (master == null)
            {
                liDCAccount.Visibility = Visibility.Visible;
            }
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
            if (ActionType == "Save" && editrow.MandateId == 0)
              editrow.StatusInfo = string.Format("({0}) Mandate created", Uniconta.DirectDebitPayment.Common.GetTimeStamp());
            
            frmRibbon_BaseActions(ActionType);
        }
    }
}
