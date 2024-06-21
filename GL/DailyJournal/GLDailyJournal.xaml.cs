using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using UnicontaClient.Pages.Maintenance;
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using Uniconta.API.Service;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLDailyJournalGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(GLDailyJournalClient); }
        }
    }
    public partial class GLDailyJournal : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.GL_DailyJournal.ToString(); }
        }
        public GLDailyJournal(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public GLDailyJournal(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgGldailyJournal);
            dgGldailyJournal.api = api;
            dgGldailyJournal.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgGldailyJournal.RowDoubleClick += dgGldailyJournal_RowDoubleClick;
            // dgGldailyJournal.PreviewMouseDown += DgGldailyJournal_PreviewMouseDown;
            RemoveMenuItem();
        }

        //private void DgGldailyJournal_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (dgGldailyJournal.CurrentColumn == dgGldailyJournal.Columns["Name"])
        //    {
        //        var selectedItem = dgGldailyJournal.SelectedItem as GLDailyJournalClient;
        //        if (selectedItem != null)
        //            AddDockItem(TabControls.GL_DailyJournalLine, selectedItem);
        //    }
        //}

        void RemoveMenuItem()
        {
            var Comp = api.CompanyEntity;
            var rb = (RibbonBase)localMenu.DataContext;
            if (Comp._CountryId != CountryCode.Iceland)
                UtilDisplay.RemoveMenuCommand(rb, "Iceland_PSPSettlements");
        }

        void dgGldailyJournal_RowDoubleClick()
        {
            var selectedItem = dgGldailyJournal.SelectedItem as GLDailyJournalClient;
            if (selectedItem != null)
                AddDockItem(TabControls.GL_DailyJournalLine, selectedItem);
        }

        protected override void LoadCacheInBackGround()
        {
            var lst = new List<Type>(15) { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.GLTransType), typeof(Uniconta.DataModel.NumberSerie), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.GLTransType), typeof(Uniconta.DataModel.GLChargeGroup), typeof(Uniconta.DataModel.PaymentTerm) };
            var noofDimensions = api.CompanyEntity.NumberOfDimensions;
            if (noofDimensions >= 1)
                lst.Add(typeof(Uniconta.DataModel.GLDimType1));
            if (noofDimensions >= 2)
                lst.Add(typeof(Uniconta.DataModel.GLDimType2));
            if (noofDimensions >= 3)
                lst.Add(typeof(Uniconta.DataModel.GLDimType3));
            if (noofDimensions >= 4)
                lst.Add(typeof(Uniconta.DataModel.GLDimType4));
            if (noofDimensions >= 5)
                lst.Add(typeof(Uniconta.DataModel.GLDimType5));
            if (api.CompanyEntity.Project)
            {
                lst.Add(typeof(Uniconta.DataModel.PrCategory));
                lst.Add(typeof(Uniconta.DataModel.PrWorkSpace));
                lst.Add(typeof(Uniconta.DataModel.Project));
            }
            LoadType(lst);
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.GLDailyJournalPage2)
                dgGldailyJournal.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGldailyJournal.SelectedItem as GLDailyJournalClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.GLDailyJournalPage2, api, Localization.lookup("Posting"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLDailyJournalPage2, selectedItem);
                    break;
                case "GLDailyJournalLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GL_DailyJournalLine, selectedItem);
                    break;
                case "GLDailyJournalPosted":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PostedJournals, selectedItem);
                    break;
                case "MatchVoucherToJournalLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.MatchPhysicalVoucherToGLDailyJournalLines, selectedItem);
                    break;
                case "DeleteAllJournalLines":
                    if (selectedItem == null)
                        return;
                    var text = string.Format(Localization.lookup("JournalContainsLines"), selectedItem._Name);
                    EraseYearWindow EraseYearWindowDialog = new EraseYearWindow(text, false);
                    EraseYearWindowDialog.Closed += async delegate
                    {
                        if (EraseYearWindowDialog.DialogResult == true)
                        {
                            CloseDockItem(TabControls.GL_DailyJournalLine, selectedItem);
                            PostingAPI postApi = new PostingAPI(api);
                            var res = await postApi.DeleteJournalLines(selectedItem);
                            UtilDisplay.ShowErrorCode(res);
                            selectedItem.NumberOfLines = 0;
                        }
                    };
                    EraseYearWindowDialog.Show();
                    break;
                case "ImportBankStatement":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ImportGLDailyJournal, selectedItem, string.Concat(string.Format(Localization.lookup("ImportOBJ"),
                            Localization.lookup("BankStatement")), " : ", selectedItem._Journal), null, true);
                    break;
                case "TransType":
                    AddDockItem(TabControls.GLTransTypePage, null, Localization.lookup("TransTypes"));
                    break;
                case "AppendTransType":
                    CWAppendEnumeratedLabel dialog = new CWAppendEnumeratedLabel(api);
                    dialog.Show();
                    break;
                case "ImportData":
                    if (selectedItem != null)
                        OpenImportDataPage(selectedItem);
                    break;
                case "GLOffSetAccountTemplate":
                    AddDockItem(TabControls.GLOffsetAccountTemplate, null, Localization.lookup("OffsetAccountTemplates"));
                    break;
                case "AccountActivity":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLTransLogPage, selectedItem, string.Format("{0}: {1}", Localization.lookup("GLTransLog"), selectedItem._Journal));
                    break;
                case "MoveJournalLines":
                    if (selectedItem != null)
                        MoveJournalLines(selectedItem);
                    break;
                case "Iceland_PSPSettlements":
                    if (selectedItem != null)
                        AddDockItem(TabControls.Iceland_ImportPSPSettlements, selectedItem, string.Concat("Sækja uppgjör færsluhirðis", " : ", selectedItem._Journal), null, true);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void MoveJournalLines(GLDailyJournalClient journal)
        {
            CwMoveJournalLines cwWin = new CwMoveJournalLines(api);
            cwWin.Closed += async delegate
            {
                if (cwWin.DialogResult == true && cwWin.GLDailyJournal != null)
                {
                    busyIndicator.IsBusy = true;
                    var result = await (new Uniconta.API.GeneralLedger.PostingAPI(api)).MoveJournalLines(journal, cwWin.GLDailyJournal);
                    busyIndicator.IsBusy = false;
                    UtilDisplay.ShowErrorCode(result);
                    if (result == 0)
                    {
                        foreach (var lin in (IEnumerable<GLDailyJournalClient>)dgGldailyJournal.ItemsSource)
                            if (lin.RowId == cwWin.GLDailyJournal.RowId)
                            {
                                lin.NumberOfLines = lin._NumberOfLines + journal._NumberOfLines;
                                journal.NumberOfLines = 0;
                                break;
                            }
                    }
                }
            };
            cwWin.Show();
        }

        void OpenImportDataPage(GLDailyJournalClient selectedItem)
        {
            var header = selectedItem._Journal;
            var baseEntityArray = new UnicontaBaseEntity[2] { api.CompanyEntity.CreateUserType<GLDailyJournalLineClient>(), selectedItem };
            AddDockItem(TabControls.ImportPage, new object[] { baseEntityArray, header }, string.Format("{0}: {1}", Localization.lookup("Import"), header));
        }

        private void TextBlock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            busyIndicator.IsBusy = true;
            var selectedItem = dgGldailyJournal.SelectedItem as GLDailyJournalClient;
            if (selectedItem != null)
                AddDockItem(TabControls.GL_DailyJournalLine, selectedItem);
            busyIndicator.IsBusy = false;
        }
    }

    internal enum BilagscanVoucherType
    {
        Invoice,
        Creditnote,
        Receipt
    }
}
