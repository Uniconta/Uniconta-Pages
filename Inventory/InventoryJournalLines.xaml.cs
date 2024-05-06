using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using System.Threading.Tasks;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.API.GeneralLedger;
using System.Windows.Threading;
using UnicontaClient.Utilities;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using System.Collections;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvJournalLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvJournalLineGridClient); } }
        public override bool Readonly { get { return false; } }
        public override IComparer GridSorting { get { return new InvJournalLineSort(); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort { get { return false; } }
        public override bool IsAutoSave { get { return _AutoSave; } }
        public bool _AutoSave;

        public override bool AddRowOnPageDown()
        {
            var selectedItem = (InvJournalLineGridClient)this.SelectedItem;
            return (selectedItem?._Item != null);
        }
        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            var newRow = (Uniconta.DataModel.InvJournalLine)dataEntity;
            var header = this.masterRecord as Uniconta.DataModel.InvJournal;
            newRow.SetMaster(header);
            newRow._Dim1 = header._Dim1;
            newRow._Dim2 = header._Dim2;
            newRow._Dim3 = header._Dim3;
            newRow._Dim4 = header._Dim4;
            newRow._Dim5 = header._Dim5;
            newRow._MovementType = header._FixedMovement;
            var lst = (IList)this.ItemsSource;
            if (lst != null && lst.Count > 0)
            {
                var castItem = lst as IEnumerable<InvJournalLineGridClient>;
                InvJournalLineGridClient last = castItem.Last();
                newRow._MovementType = last._MovementType;
                newRow._Date = last.Date;
            }
            else
                newRow._Date = BasePage.GetSystemDefaultDate().Date;
        }
        protected override bool RenderAllColumns { get { return true; } }
    }
    public partial class InventoryJournalLines : GridBasePage
    {
        SQLCache items, warehouse, debtors, creditors, standardVariants, variants1, variants2;
        public override string NameOfControl { get { return TabControls.InventoryJournalLines; } }
        InvJournal journal;

        public InventoryJournalLines(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        public InventoryJournalLines(UnicontaBaseEntity master) : base(master)
        {
            InitPage();
            journal = master as InvJournal;
            if (journal != null)
            {
                dgInvJournalLine._AutoSave = journal._AutoSave;
                dgInvJournalLine.UpdateMaster(master);
            }
        }

        private void InitPage()
        {
            this.DataContext = this;
            InitializeComponent();
            SetRibbonControl(localMenu, dgInvJournalLine);
            dgInvJournalLine.api = api;
            dgInvJournalLine.BusyIndicator = busyIndicator;

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInvJournalLine.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            this.BeforeClose += JournalLine_BeforeClose;

            var Comp = api.CompanyEntity;
            this.items = Comp.GetCache(typeof(InvItem));
            this.warehouse = Comp.GetCache(typeof(InvWarehouse));
            this.variants1 = Comp.GetCache(typeof(InvVariant1));
            this.variants2 = Comp.GetCache(typeof(InvVariant2));
            this.standardVariants = Comp.GetCache(typeof(InvStandardVariant));
            this.debtors = Comp.GetCache(typeof(Debtor));
            this.creditors = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));
            localInvSerieBatchList = new List<InvSerieBatchClient>();
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "Journal", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    var cache = api.GetCache(typeof(Uniconta.DataModel.InvJournal)) ?? api.LoadCache(typeof(Uniconta.DataModel.InvJournal)).GetAwaiter().GetResult();
                    journal = (Uniconta.DataModel.InvJournal)cache.Get(rec.Value);
                    if (journal != null)
                    {
                        dgInvJournalLine._AutoSave = journal._AutoSave;
                        dgInvJournalLine.UpdateMaster(journal);
                    }
                }
            }
            base.SetParameter(Parameters);
        }

        private void JournalLine_BeforeClose()
        {
            var lines = dgInvJournalLine.ItemsSource as IList;
            int cnt = lines != null ? lines.Count : 0;
            var mClient = journal as InvJournalClient;
            if (mClient != null)
                mClient.NumberOfLines = cnt;
            else
                journal._NumberOfLines = cnt;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();

            var company = api.CompanyEntity;
            //Variants

            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);

            if (!company.Location || !company.Warehouse)
                LocationTo.Visible = LocationTo.ShowInColumnChooser = Location.Visible = Location.ShowInColumnChooser = false;
            else
                LocationTo.ShowInColumnChooser = Location.ShowInColumnChooser = true;
            if (!company.Warehouse)
                WarehouseTo.Visible = WarehouseTo.ShowInColumnChooser = Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                WarehouseTo.ShowInColumnChooser = Warehouse.ShowInColumnChooser = true;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            if (!company.InvBOM)
            {
                ReportAsFinished.Visible = ReportAsFinished.ShowInColumnChooser = false;
                ReportAsFinishedDeep.Visible = ReportAsFinishedDeep.ShowInColumnChooser = false;
            }
            else
                ReportAsFinished.ShowInColumnChooser = ReportAsFinishedDeep.ShowInColumnChooser = true;
            if (dgInvJournalLine.IsLoadedFromLayoutSaved)
            {
                dgInvJournalLine.ClearSorting();
                dgInvJournalLine.ClearFilter();
                dgInvJournalLine.IsLoadedFromLayoutSaved = false;
            }
        }

        bool DataChanged;
        public override bool IsDataChaged
        {
            get
            {
                return DataChanged || base.IsDataChaged;
            }
        }

        public override void Utility_Refresh(string screenName, object argument)
        {
            var param = argument as object[];
            if (param == null)
                return;

            if (screenName == TabControls.ItemVariantAddPage)
            {
                var Journal = Convert.ToString(param[1]);
                if (Journal == journal._Journal)
                {
                    var invItems = param[0] as List<UnicontaBaseEntity>;
                    dgInvJournalLine.PasteRows(invItems);
                }
            }
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            InvJournalLineGridClient oldselectedItem = e.OldItem as InvJournalLineGridClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= Journal_changed;

            InvJournalLineGridClient selectedItem = e.NewItem as InvJournalLineGridClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += Journal_changed;
                if (selectedItem.Variant1Source == null)
                    setVariant(selectedItem, false);
                if (selectedItem.Variant2Source == null)
                    setVariant(selectedItem, true);
                if (selectedItem.accountSource == null)
                    SetAccountSource(selectedItem);
            }
        }

        List<InvSerieBatchClient> localInvSerieBatchList = null;
        async void setSerieBatchSource(InvItem master, InvJournalLineGridClient rec)
        {
            if (master != null && master._UseSerialBatch)
            {
                var serie = new InvSerieBatchOpen() { _Item = rec._Item };
                var lst = await api.Query<InvSerieBatchClient>(serie);
                if (lst != null)
                {
                    localInvSerieBatchList.AddRange(lst);
                    rec.serieBatchSource = lst.Select(x => x.Number).ToList();
                }
            }
            else
            {
                rec.serieBatchSource = null;
                rec.SerieBatch = null;
            }
            rec.NotifyPropertyChanged("SerieBatchSource");
        }

        async void setVariant(InvJournalLineGridClient rec, bool SetVariant2)
        {
            if (items == null || variants1 == null || variants2 == null)
                return;

            //Check for Variant2 Exist
            if (string.IsNullOrEmpty(api?.CompanyEntity?._Variant2))
                SetVariant2 = false;

            var item = (InvItem)items.Get(rec._Item);
            if (item != null && item._StandardVariant != null)
            {
                rec.standardVariant = item._StandardVariant;
                var master = (InvStandardVariant)standardVariants?.Get(item._StandardVariant);
                if (master == null)
                    return;
                if (master._AllowAllCombinations)
                {
                    rec.Variant1Source = this.variants1?.GetKeyStrRecords.Cast<InvVariant1>();
                    rec.Variant2Source = this.variants2?.GetKeyStrRecords.Cast<InvVariant2>();
                }
                else
                {
                    var Combinations = master.Combinations ?? await master.LoadCombinations(api);
                    if (Combinations == null)
                        return;

                    List<InvVariant1> invs1 = null;
                    List<InvVariant2> invs2 = null;
                    string vr1 = null;
                    if (SetVariant2)
                    {
                        vr1 = rec._Variant1;
                        invs2 = new List<InvVariant2>();
                    }
                    else
                        invs1 = new List<InvVariant1>();

                    string LastVariant = null;
                    var var2Value = rec._Variant2;
                    bool hasVariantValue = (var2Value == null);
                    foreach (var cmb in Combinations)
                    {
                        if (SetVariant2)
                        {
                            if (cmb._Variant1 == vr1 && cmb._Variant2 != null)
                            {
                                var v2 = (InvVariant2)variants2.Get(cmb._Variant2);
                                if (v2 != null)
                                {
                                    invs2.Add(v2);
                                    if (var2Value == v2._Variant)
                                        hasVariantValue = true;
                                }
                            }
                        }
                        else if (LastVariant != cmb._Variant1)
                        {
                            LastVariant = cmb._Variant1;
                            var v1 = (InvVariant1)variants1.Get(cmb._Variant1);
                            if (v1 != null)
                                invs1.Add(v1);
                        }
                    }
                    if (SetVariant2)
                    {
                        rec.variant2Source = invs2;
                        //if (!hasVariantValue)
                        //    rec.Variant2 = null;
                    }
                    else
                        rec.variant1Source = invs1;
                }
            }
            else
            {
                rec.variant1Source = null;
                rec.variant2Source = null;
            }
            if (SetVariant2)
                rec.NotifyPropertyChanged("Variant2Source");
            else
                rec.NotifyPropertyChanged("Variant1Source");
        }
        async void setLocation(InvWarehouse master, InvJournalLineGridClient rec)
        {
            if (api.CompanyEntity.Location)
            {
                if (master != null)
                    rec.locationSource = master.Locations ?? await master.LoadLocations(api);
                else
                {
                    rec.locationSource = null;
                    rec.Location = null;
                }
                rec.NotifyPropertyChanged("LocationSource");
            }
        }
        async void setLocationTo(InvWarehouse master, InvJournalLineGridClient rec)
        {
            if (api.CompanyEntity.Location)
            {
                if (master != null)
                    rec.locationToSource = master.Locations ?? await master.LoadLocations(api);
                else
                {
                    rec.locationToSource = null;
                    rec.LocationTo = null;
                }
                rec.NotifyPropertyChanged("LocationToSource");
            }
        }
        private void Journal_changed(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (InvJournalLineGridClient)sender;
            switch (e.PropertyName)
            {
                case "Item":
                    var selectedItem = (InvItem)items.Get(rec._Item);
                    if (selectedItem != null)
                    {
                        if (rec._MovementType != 0 && selectedItem._CostPrice != 0d)
                            rec.CostPrice = selectedItem._CostPrice;
                        rec.SetItemValues(selectedItem);
                        if (selectedItem._StandardVariant != rec.standardVariant)
                        {
                            rec.Variant1 = null;
                            rec.Variant2 = null;
                            rec.variant2Source = null;
                            rec.NotifyPropertyChanged("Variant2Source");
                        }
                        setVariant(rec, false);
                        globalEvents?.NotifyRefreshViewer(NameOfControl, selectedItem);
                    }
                    break;
                case "Warehouse":
                    if (warehouse != null && rec._Warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._Warehouse);
                        setLocation(selected, rec);
                    }
                    break;
                case "WarehouseTo":
                    if (warehouse != null && rec._WarehouseTo != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._WarehouseTo);
                        setLocationTo(selected, rec);
                    }
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
                case "LocationTo":
                    if (string.IsNullOrEmpty(rec._WarehouseTo))
                        rec._Location = null;
                    break;
                case "Variant1":
                    if (rec._Variant1 != null)
                        setVariant(rec, true);
                    break;
                case "MovementType":
                    SetAccountSource(rec);
                    break;
                case "EAN":
                    FindOnEAN(rec);
                    break;
                case "SerieBatch":
                    if (items != null)
                    {
                        if (rec._Item == null || rec._Item == string.Empty)
                            GetItemFromSerailNumber(rec);
                        var selectedSerieBatch = localInvSerieBatchList.Where(x => x.Number == rec.SerieBatch).FirstOrDefault();
                        if (selectedSerieBatch != null && api.CompanyEntity.Warehouse)
                        {
                            rec.Warehouse = selectedSerieBatch.Warehouse;
                            if (api.CompanyEntity.Location)
                                rec.Location = selectedSerieBatch.Location;
                        }
                    }
                    break;
            }
        }

        async void GetItemFromSerailNumber(InvJournalLineGridClient rec)
        {
            var reportApi = new Uniconta.API.Inventory.ReportAPI(api);
            busyIndicator.IsBusy = true;
            var rowId = await reportApi.GetItemFromSerialNumber(rec.SerieBatch);
            busyIndicator.IsBusy = false;
            if (rowId == 0) return;
            var item = (InvItem)items.Get((int)rowId);
            if (item != null)
            {
                rec.Item = item._Item;
                rec.NotifyPropertyChanged("Item");
            }
        }

        void FindOnEAN(InvJournalLineGridClient rec)
        {
            var EAN = rec._EAN;
            if (string.IsNullOrWhiteSpace(EAN))
                return;
            var found = (from item in (InvItem[])items.GetNotNullArray where string.Compare(item._EAN, EAN, StringComparison.CurrentCultureIgnoreCase) == 0 select item).FirstOrDefault();
            if (found != null)
            {
                rec._EAN = found._EAN;
                rec.Item = found._Item;
            }
            else
                FindOnEANVariant(rec);
        }

        async void FindOnEANVariant(InvJournalLineGridClient rec)
        {
            var ap = new Uniconta.API.Inventory.ReportAPI(api);
            var variant = await ap.GetInvVariantDetail(rec._EAN);
            if (variant != null)
            {
                rec.Item = variant._Item;
                rec.Variant1 = variant._Variant1;
                rec.Variant2 = variant._Variant2;
                rec.Variant3 = variant._Variant3;
                rec.Variant4 = variant._Variant4;
                rec.Variant5 = variant._Variant5;
                rec._EAN = variant._EAN;
                if (variant._CostPrice != 0d)
                    rec.CostPrice = variant._CostPrice;
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;

            if (debtors == null)
                debtors = await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (creditors == null)
                creditors = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);

            if (this.items == null)
                this.items = await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);

            if (api.CompanyEntity.Warehouse && this.warehouse == null)
                this.warehouse = await api.LoadCache(typeof(Uniconta.DataModel.InvWarehouse));

            if (api.CompanyEntity.ItemVariants)
            {
                if (this.standardVariants == null)
                    this.standardVariants = await api.LoadCache(typeof(Uniconta.DataModel.InvStandardVariant)).ConfigureAwait(false);
                if (this.variants1 == null)
                    this.variants1 = await api.LoadCache(typeof(Uniconta.DataModel.InvVariant1)).ConfigureAwait(false);
                if (this.variants2 == null)
                    this.variants2 = await api.LoadCache(typeof(Uniconta.DataModel.InvVariant2)).ConfigureAwait(false);
            }
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var selectedItem = dgInvJournalLine.SelectedItem as InvJournalLineGridClient;
                if (selectedItem != null)
                {
                    if (selectedItem.Variant1Source == null)
                        setVariant(selectedItem, false);
                    if (selectedItem.Variant2Source == null)
                        setVariant(selectedItem, true);
                }
            }));
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvJournalLine.SelectedItem as InvJournalLineGridClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgInvJournalLine.AddRow();
                    break;
                case "CopyRow":
                    dgInvJournalLine.CopyRow();
                    break;
                case "DeleteRow":
                    dgInvJournalLine.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "PostJournal":
                    PostJournal();
                    break;
                case "Storage":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvItemStoragePage, dgInvJournalLine.syncEntity, true, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OnHand"), selectedItem.Journal));
                    break;
                case "UnfoldBOM":
                    if (selectedItem != null)
                        UnfoldBOM(selectedItem);
                    break;
                case "AddVariants":
                    var itm = selectedItem?.InvItem;
                    if (itm?._StandardVariant != null)
                    {
                        var paramItem = new object[] { selectedItem, journal };
                        dgInvJournalLine.SetLoadedRow(selectedItem);
                        AddDockItem(TabControls.ItemVariantAddPage, paramItem, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Variants")), null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "UndoDelete":
                    dgInvJournalLine.UndoDeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void UnfoldBOM(InvJournalLineGridClient selectedItem)
        {
            var items = this.items;
            var item = (InvItem)items.Get(selectedItem._Item);
            if (item == null || item._ItemType != (byte)ItemType.ProductionBOM)
                return;

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            var list = await api.Query<InvBOM>(selectedItem);
            if (list != null && list.Length > 0)
            {
                Array.Sort(list, new InvBOMSort());
                var lst = new List<UnicontaBaseEntity>(list.Length);
                foreach (var bom in list)
                {
                    var invJournalLine = new InvJournalLineGridClient();
                    invJournalLine._Item = bom._ItemPart;
                    invJournalLine._Date = selectedItem._Date;
                    invJournalLine._Dim1 = selectedItem._Dim1;
                    invJournalLine._Dim2 = selectedItem._Dim2;
                    invJournalLine._Dim3 = selectedItem._Dim3;
                    invJournalLine._Dim4 = selectedItem._Dim4;
                    invJournalLine._Dim5 = selectedItem._Dim5;
                    invJournalLine._MovementType = InvMovementType.IncludedInBOM;
                    invJournalLine._Variant1 = bom._Variant1;
                    invJournalLine._Variant2 = bom._Variant2;
                    invJournalLine._Variant3 = bom._Variant3;
                    invJournalLine._Variant4 = bom._Variant4;
                    invJournalLine._Variant5 = bom._Variant5;
                    item = (InvItem)items.Get(bom._ItemPart);
                    if (item != null)
                    {
                        invJournalLine.SetItemValues(item);
                        invJournalLine._Warehouse = bom._Warehouse ?? item._Warehouse ?? selectedItem._Warehouse;
                        invJournalLine._Location = bom._Location ?? item._Location ?? selectedItem._Location;
                        invJournalLine._CostPrice = item._CostPrice;
                        invJournalLine._Qty = -Math.Round(bom.GetBOMQty(selectedItem._Qty), item._Decimals);
                    }
                    else
                        invJournalLine._Qty = -Math.Round(bom.GetBOMQty(selectedItem._Qty), 2);
                    lst.Add(invJournalLine);
                }
                if (selectedItem._MovementType != InvMovementType.ReportAsFinished)
                {
                    selectedItem._MovementType = InvMovementType.ReportAsFinished;
                    selectedItem.NotifyPropertyChanged(nameof(selectedItem.MovementType));
                }
                dgInvJournalLine.PasteRows(lst);
                this.DataChanged = true;
            }
            busyIndicator.IsBusy = false;
        }

        void PostJournal()
        {
            api.AllowBackgroundCrud = false;
            var savetask = saveGrid();
            api.AllowBackgroundCrud = true;

            var source = (ICollection<InvJournalLineGridClient>)dgInvJournalLine.ItemsSource;
            if (source == null || source.Count == 0)
                return;

            CWInvPosting invpostingDialog = new CWInvPosting(api);
            invpostingDialog.showCompanyName = true;
#if !SILVERLIGHT
            invpostingDialog.DialogTableId = 2000000039;
#endif
            invpostingDialog.Closed += async delegate
            {
                if (invpostingDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    if (savetask != null)
                        await savetask;
                    var postingApi = new Uniconta.API.Inventory.PostingAPI(api);
                    var postingResult = await postingApi.PostJournal(journal, invpostingDialog.Date, invpostingDialog.Text, invpostingDialog.TransType, invpostingDialog.Comment, invpostingDialog.FixedVoucher, invpostingDialog.Simulation, new GLTransClientTotal(), source.Count);
                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

                    if (postingResult == null)
                        return;

                    if (postingResult.Err != ErrorCodes.Succes)
                        Utility.ShowJournalError(postingResult, dgInvJournalLine);

                    else if (invpostingDialog.Simulation)
                    {
                        if (postingResult.SimulatedTrans != null)
                            AddDockItem(TabControls.SimulatedTransactions, postingResult.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                        else
                        {
                            var msg = string.Format(Uniconta.ClientTools.Localization.lookup("OBJisEmpty"), Uniconta.ClientTools.Localization.lookup("LedgerTransList"));
                            msg = Uniconta.ClientTools.Localization.lookup("JournalOK") + Environment.NewLine + msg;
                            UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                        }
                    }
                    else
                    {
                        string msg;
                        if (postingResult.JournalPostedlId != 0)
                            msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), postingResult.JournalPostedlId);
                        else
                            msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));

                        if (journal._DeleteLines)
                        {
                            var lst = new List<InvJournalLineGridClient>();
                            foreach (var journalLine in source)
                                if (journalLine._OnHold)
                                    lst.Add(journalLine);

                            dgInvJournalLine.ItemsSource = null;
                            dgInvJournalLine.ItemsSource = lst;
                            journal._NumberOfLines = lst.Count;
                            (journal as InvJournalClient)?.NotifyPropertyChanged("NumberOfLines");
                        }
                    }
                }
            };
            invpostingDialog.Show();
        }

        protected override async Task<ErrorCodes> saveGrid()
        {
            ErrorCodes res = await base.saveGrid();
            if (res == ErrorCodes.Succes)
                DataChanged = false;
            return res;
        }

        private void expanderCtrl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (expanderCtrl.SelectedItem == null)
                expanderCtrl.Height = 20;
            else
                expanderCtrl.Height = 65;
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1To, coldim2To, coldim3To, coldim4To, coldim5To,IsDimensionTo:true );
        }

        private void SerieBatch_GotFocus(object sender, RoutedEventArgs e)
        {
            InvJournalLineGridClient selectedItem = dgInvJournalLine.SelectedItem as InvJournalLineGridClient;
            if (selectedItem?._Item != null)
            {
                var selected = items.Get<InvItem>(selectedItem._Item);
                setSerieBatchSource(selected, selectedItem);
            }
        }

        CorasauGridLookupEditorClient prevLocation;
        private void Location_GotFocus(object sender, RoutedEventArgs e)
        {
            InvJournalLineGridClient selectedItem = dgInvJournalLine.SelectedItem as InvJournalLineGridClient;
            if (selectedItem?._Warehouse != null && warehouse != null)
            {
                var selected = warehouse.Get<InvWarehouseClient>(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
                if (prevLocation != null)
                    prevLocation.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevLocation = editor;
                editor.isValidate = true;
            }
        }

        CorasauGridLookupEditorClient prevLocationTo;
        private void LocationTo_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvJournalLine.SelectedItem as InvJournalLineGridClient;
            if (selectedItem?._WarehouseTo != null && Warehouse != null)
            {
                var selected = warehouse.Get<InvWarehouseClient>(selectedItem._WarehouseTo);
                setLocationTo(selected, selectedItem);
                if (prevLocationTo != null)
                    prevLocationTo.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevLocationTo = editor;
                editor.isValidate = true;
            }
        }
        CorasauGridLookupEditorClient prevAccount;
        private void DCAccount_GotFocus(object sender, RoutedEventArgs e)
        {
            InvJournalLineGridClient selectedItem = dgInvJournalLine.SelectedItem as InvJournalLineGridClient;
            if (selectedItem != null)
            {
                SetAccountSource(selectedItem);
                if (prevAccount != null)
                    prevAccount.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevAccount = editor;
                editor.isValidate = true;
            }
        }

        private void SetAccountSource(InvJournalLineGridClient rec)
        {
            SQLCache cache = (rec._MovementType == InvMovementType.Creditor) ? creditors : debtors;
            if (cache != null)
            {
                rec.accountSource = null;
                rec.NotifyPropertyChanged("AccountSource");
                rec.accountSource = cache;
                rec.NotifyPropertyChanged("AccountSource");
            }
        }

        CorasauGridLookupEditorClient prevVariant1;
        private void variant1_GotFocus(object sender, RoutedEventArgs e)
        {
            if (prevVariant1 != null)
                prevVariant1.isValidate = false;
            var editor = (CorasauGridLookupEditorClient)sender;
            prevVariant1 = editor;
            editor.isValidate = true;
        }

        CorasauGridLookupEditorClient prevVariant2;
        private void variant2_GotFocus(object sender, RoutedEventArgs e)
        {
            if (prevVariant2 != null)
                prevVariant2.isValidate = false;
            var editor = (CorasauGridLookupEditorClient)sender;
            prevVariant2 = editor;
            editor.isValidate = true;
        }
        protected override bool LoadTemplateHandledLocally(IEnumerable<UnicontaBaseEntity> templateRows)
        {
            foreach (var gl in (IEnumerable<Uniconta.DataModel.InvJournalLine>)templateRows)
                gl._Date = DateTime.MinValue;
            return false;
        }
    }
    public class InvJournalLineGridClient : InvJournalLineClient
    {
        internal object locationSource;
        public object LocationSource { get { return locationSource; } }
        internal object locationToSource;
        public object LocationToSource { get { return locationToSource; } }
        internal object accountSource;
        public object AccountSource { get { return accountSource; } }
        internal object serieBatchSource;
        public object SerieBatchSource { get { return serieBatchSource; } }

    }
}
