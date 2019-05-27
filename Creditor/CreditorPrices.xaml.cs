using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorPriceGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorPriceClient); } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }

        public override bool AddRowOnPageDown()
        {
            var selectedItem = (CreditorPriceClient)this.SelectedItem;
            return (selectedItem?._PrCategory != null);
        }
    }

    public partial class CreditorPrices : GridBasePage
    {
        public CreditorPrices(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public CreditorPrices(SynchronizeEntity syncEntity ): base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dbCreditorPricesGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dbCreditorPricesGrid.masterRecord);

            if (string.IsNullOrEmpty(key)) return;

            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("CreditorPrices"), key);
            SetHeader(header);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            dbCreditorPricesGrid.api = api;
            dbCreditorPricesGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dbCreditorPricesGrid);
            dbCreditorPricesGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        protected override void OnLayoutLoaded()
        {
            //setDim();

            //var master = dbCreditorPricesGrid.masterRecords?.First();
            //if (master is Uniconta.DataModel.EmpPayrollCategory)
            //{
            //    colPayrolCat.Visible = false;
            //    colPayrolCatName.Visible = false;
            //}

            //if (master is Uniconta.DataModel.Employee)
            //{
            //    colEmployee.Visible = false;
            //    colEmployeeName.Visible = false;
            //}
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            CreditorPriceClient selectedItem = dbCreditorPricesGrid.SelectedItem as CreditorPriceClient;
            switch (ActionType)
            {
                case "AddRow":
                    dbCreditorPricesGrid.AddRow();
                    break;
                case "CopyRow":
                    dbCreditorPricesGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dbCreditorPricesGrid.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dbCreditorPricesGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
