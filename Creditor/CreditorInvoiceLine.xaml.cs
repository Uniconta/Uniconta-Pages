using UnicontaClient.Models;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Util;
using System.Collections;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using DevExpress.Data;
using Uniconta.API.Service;
using Uniconta.Client.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorInvTransClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorInvoiceLines); } }
        public override IComparer GridSorting { get { return new InvTransInvoiceSort(); } }
    }
    public partial class CreditorInvoiceLine : GridBasePage
    {
        private SynchronizeEntity syncEntity;
        public CreditorInvoiceLine(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage(null);
        }

        public CreditorInvoiceLine(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }
        public CreditorInvoiceLine(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgCrdInvLines.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var syncMaster = dgCrdInvLines.masterRecord as CreditorInvoiceClient;
            if (syncMaster == null)
                return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), syncMaster.InvoiceNumber);
            SetHeader(header);
        }

        UnicontaBaseEntity master = null;
        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            this.master = master;
            dgCrdInvLines.UpdateMaster(master);
            SetRibbonControl(localMenu, dgCrdInvLines);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCrdInvLines.api = api;
            dgCrdInvLines.BusyIndicator = busyIndicator;
            LoadNow(typeof(Uniconta.DataModel.InvItem));
            dgCrdInvLines.CustomSummary += DgCrdInvLines_CustomSummary;
            dgCrdInvLines.ShowTotalSummary();
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void DgCrdInvLines_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as CreditorInvoiceLines;
                    sumSales += row.SalesPrice;
                    sumMargin += row.Margin;
                    break;
                case CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                    {
                        sumMarginRatio = 100 * sumMargin / sumSales;
                        e.TotalValue = sumMarginRatio;
                    }
                    break;
            }
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = api.CompanyEntity;
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            if (!company.Project)
                Project.Visible = Project.ShowInColumnChooser = false;
            if (!company.ProjectTask)
                Task.Visible = Task.ShowInColumnChooser = false;

            if (this.master != null)
                colAccount.Visible = AccountName.Visible = false;

            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

       
        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCrdInvLines.SelectedItem as InvTransClient;
            switch (ActionType)
            {
                case "ChangeVariant":
                    if (selectedItem == null)
                        return;
                    var cwChangeVaraints = new CWModifyVariants(api, selectedItem);
                    cwChangeVaraints.Closing += delegate
                    {
                        if (cwChangeVaraints.DialogResult == true)
                            gridRibbon_BaseActions("RefreshGrid");
                    };
                    cwChangeVaraints.Show();
                    break;
                case "ChangeStorage":
                    if (selectedItem == null)
                        return;
                    var cwchangeStorage = new CWModiyStorage(api, selectedItem);
                    cwchangeStorage.Closing += delegate
                    {
                        if (cwchangeStorage.DialogResult == true)
                            gridRibbon_BaseActions("RefreshGrid");
                    };
                    cwchangeStorage.Show();
                    break;
                case "SeriesBatch":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.InvSeriesBatch, selectedItem, string.Format("{0}:{1}", Localization.lookup("SerialBatchNumbers"), selectedItem._InvoiceRowId));
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void JournalPosted(InvTransClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        public override string NameOfControl { get { return TabControls.CreditorInvoiceLine.ToString(); } }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var inv = dg.SelectedItem as InvTransClient;
            if (inv == null)
                return lookup;
            var currentCol = dg.CurrentColumn;
            if (currentCol != null)
            {
                if (currentCol.FieldName == "Item")
                    lookup.TableType = typeof(Uniconta.DataModel.InvItem);
                else if (currentCol.FieldName == "DCAccount")
                    lookup.TableType = typeof(Uniconta.DataModel.Creditor);
            }
            return lookup;
        }
    }
}
