using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
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
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.API.Project;
using System.Collections;
using Uniconta.ClientTools.DataModel;
using UnicontaAPI.Project.API;
using Uniconta.API.System;
using DevExpress.Xpf.Grid;
using System.Windows.Threading;
using UnicontaClient.Utilities;
using Uniconta.API.DebtorCreditor;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
#if !SILVERLIGHT
using Microsoft.Win32;
using FromXSDFile.OIOUBL.ExportImport;
using ubl_norway_uniconta;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectOnAccountInvoiceLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectOnAccountInvoiceLineClient); } }

        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
    }

    public partial class ProjectOnAccountInvoiceLinePage : GridBasePage
    {
        ProjectClient Project;
        public ProjectOnAccountInvoiceLinePage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public ProjectOnAccountInvoiceLinePage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectOnAccountInvoiceLineGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            var syncMaster = dgProjectOnAccountInvoiceLineGrid.masterRecord as Uniconta.DataModel.Project;
            if (syncMaster == null) return;
            string header = null;
            header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("OnAccountInvoicing"), syncMaster._Name);
            SetHeader(header);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            ((TableView)dgProjectOnAccountInvoiceLineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            dgProjectOnAccountInvoiceLineGrid.api = api;
            Project = master as ProjectClient;
            var masterRecord = master as Uniconta.DataModel.Project;
            if (masterRecord == null)
                throw new Exception("This page only supports master Project On Project Invoice Line");

            List<UnicontaBaseEntity> masterList = new List<UnicontaBaseEntity>();
            masterList.Add(master);
            dgProjectOnAccountInvoiceLineGrid.masterRecords = masterList;
            SetRibbonControl(localMenu, dgProjectOnAccountInvoiceLineGrid);
            dgProjectOnAccountInvoiceLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            ProjectOnAccountInvoiceLineClient selectedItem = dgProjectOnAccountInvoiceLineGrid.SelectedItem as ProjectOnAccountInvoiceLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgProjectOnAccountInvoiceLineGrid.AddRow();
                    break;
                case "CopyRow":
                    dgProjectOnAccountInvoiceLineGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgProjectOnAccountInvoiceLineGrid.DeleteRow();
                    break;
                case "GenerateInvoice":
                    if (Project != null)
                        GenerateInvoice(Project);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void GenerateInvoice(ProjectClient dbProject)
        {
            var Invapi = new Uniconta.API.Project.InvoiceAPI(api);
            var savetask = saveGrid();
            CWProjectGenerateInvoice GenrateInvoiceDialog = new CWProjectGenerateInvoice(api, GetSystemDefaultDate(), true, true, true, true, true);
#if SILVERLIGHT
            GenrateInvoiceDialog.Height = 210.0d;
#else
            GenrateInvoiceDialog.DialogTableId = 2000000047;
#endif
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    if (savetask != null)
                        await savetask;

                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingResult.SetUpInvoicePosting(dbProject, GenrateInvoiceDialog.GenrateDate, GenrateInvoiceDialog.IsSimulation, GenrateInvoiceDialog.InvoiceCategory, GenrateInvoiceDialog.ShowInvoice,
                        GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.NumberOfPages, GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.GenerateOIOUBLClicked, null, false);
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                    {
                        Task reloadTask = null;
                        if (!GenrateInvoiceDialog.IsSimulation)
                            reloadTask = Filter(null);
                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgProjectOnAccountInvoiceLineGrid);
                }
            };
            GenrateInvoiceDialog.Show();
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgProjectOnAccountInvoiceLineGrid.Filter(propValuePair);
        }


#if !SILVERLIGHT
        public static async void GenerateOIOXml(CrudAPI api, DCInvoice inv)
        {
            var Comp = api.CompanyEntity;
            var InvCache = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem));
            var VatCache = api.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLVat));

            //SystemInfo.Visible = true;

            int countErr = 0;
            SaveFileDialog saveDialog = null;
            Uniconta.API.DebtorCreditor.InvoiceAPI Invapi = new Uniconta.API.DebtorCreditor.InvoiceAPI(api);

            var listPropval = new List<PropValuePair>()
            {
                PropValuePair.GenereteWhereElements("InvoiceNumber", inv._InvoiceNumber, CompareOperator.Equal),
                PropValuePair.GenereteWhereElements("DCAccount", inv._DCAccount, CompareOperator.Equal),
                PropValuePair.GenereteWhereElements("Date", inv._Date, CompareOperator.Equal)
            };

            var FindDebInvLocal = await api.Query<DebtorInvoiceClient>(listPropval);
            var invClient = FindDebInvLocal.FirstOrDefault();
            if (invClient == null)
                return;

            var Debcache = Comp.GetCache(typeof(Debtor)) ?? await api.LoadCache(typeof(Debtor));
            var debtor = (Debtor)Debcache.Get(invClient._DCAccount);

            if (debtor == null || !debtor._InvoiceInXML || invClient.SendTime != DateTime.MinValue)
            {
                if (!debtor._InvoiceInXML)
                    UnicontaMessageBox.Show("Faktura i OIOUBL er ikke sat til denne debitor", Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            var Invoicemasters = new UnicontaBaseEntity[] { invClient };
            var invoiceLines = await api.Query<InvTransClient>(Invoicemasters, null);
            var layoutGroupCache = api.GetCache(typeof(DebtorLayoutGroup)) ?? await api.LoadCache(typeof(DebtorLayoutGroup));

            InvItemText[] invItemText = null;
            if (debtor._ItemNameGroup != null)
                invItemText = await api.Query<InvItemText>(new UnicontaBaseEntity[] { debtor }, null);

            Contact contactPerson = null;
            if (invClient._ContactRef != 0)
            {
                var Contacts = api.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Contact));
                foreach (var contact in (Uniconta.DataModel.Contact[])Contacts.GetRecords)
                    if (contact.RowId == invClient._ContactRef)
                    {
                        contactPerson = contact;
                        break;
                    }
            }

            DebtorOrders.SetDeliveryAdress(invClient, debtor, api);

            Debtor deliveryAccount;
            if (invClient._DeliveryAccount != null)
                deliveryAccount = (Debtor)Debcache.Get(invClient._DeliveryAccount);
            else
                deliveryAccount = null;

            CreationResult result;

            if (Comp._CountryId == CountryCode.Norway || Comp._CountryId == CountryCode.Netherlands)
                result = EHF.GenerateEHFXML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, invItemText, contactPerson);
            else
            {
                TableAddOnData[] attachments = await FromXSDFile.OIOUBL.ExportImport.Attachments.CollectInvoiceAttachments(invClient, api);
                result = Uniconta.API.DebtorCreditor.OIOUBL.GenerateOioXML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, invItemText, contactPerson, attachments, layoutGroupCache);
            }

            bool createXmlFile = true;

            var errorInfo = "";
            if (result.HasErrors)
            {
                countErr++;
                createXmlFile = false;
                //invClient._SystemInfo = string.Empty;
                foreach (FromXSDFile.OIOUBL.ExportImport.PrecheckError error in result.PrecheckErrors)
                {
                    errorInfo += error.ToString() + "\n";
                }
            }

            if (result.Document != null && createXmlFile)
            {
                string invoice = Uniconta.ClientTools.Localization.lookup("Invoice");
                saveDialog = Uniconta.ClientTools.Util.UtilDisplay.LoadSaveFileDialog;
                saveDialog.FileName = string.Format("{0}_{1}", invoice, invClient.InvoiceNumber);
                saveDialog.Filter = "XML-File | *.xml";
                bool? dialogResult = saveDialog.ShowDialog();
                if (dialogResult != true)
                    return;

                var filename = saveDialog.FileName;

                result.Document.Save(filename);
                await Invapi.MarkSendInvoice(invClient);
                invClient.SendTime = BasePage.GetSystemDefaultDate();
                //invClient._SystemInfo = "File created";
            }

            //invClient.NotifySystemInfoSet();

            //if (countErr != 0)
            //    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup(String.Format("Couldn't create file for {0} invoice(s). Please check System info column.", countErr)));

            if (countErr != 0 && !string.IsNullOrWhiteSpace(errorInfo))
                UnicontaMessageBox.Show(errorInfo, Uniconta.ClientTools.Localization.lookup("Error"));

        }
#endif

    }
}
