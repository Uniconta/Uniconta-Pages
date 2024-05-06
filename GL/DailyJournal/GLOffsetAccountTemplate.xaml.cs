using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLOffsetAccountLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLOffsetAccountLineGridClient); } }
        public override bool Readonly { get { return false; } }
        protected override List<string> GridSkipFields { get { return new List<string>() { "AccountName" }; } }
        protected override bool RenderAllColumns { get { return true; } }
        public override bool IsAutoSave { get { return false; } }
    }

    public partial class GLOffsetAccountTemplate : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLOffsetAccountTemplate; } }

        SQLCache LedgerCache, DebtorCache, CreditorCache, ProjectCache;

        public GLOffsetAccountTemplate(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        public GLOffsetAccountTemplate(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }

        GLDailyJournalLineClient glDailyJournalLine;
        BankStatementLineClient bankStatementLine;
        VouchersClient vouchersClient;
        UnicontaBaseEntity _master;
        int Decimals;
        double MasterAmount;
        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            _master = master;
            glDailyJournalLine = master as GLDailyJournalLineClient;
            bankStatementLine = master as BankStatementLineClient;
            vouchersClient = master as VouchersClient;

            ((TableView)dgGlOffSetAccountTplt.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgGlOffSetAccountTplt;
            dgGlOffSetAccountTplt.api = api;
            SetRibbonControl(localMenu, dgGlOffSetAccountTplt);
            dgGlOffSetAccountTplt.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgGlOffSetAccountTplt.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            btnAdd.Content = Uniconta.ClientTools.Localization.lookup("Add");
            btnEdit.Content = string.Format(Uniconta.ClientTools.Localization.lookup("EditOBJ"), (Uniconta.ClientTools.Localization.lookup("Name")));
            btnDelete.Content = Uniconta.ClientTools.Localization.lookup("Delete");
            var company = api.CompanyEntity;
            LedgerCache = company.GetCache(typeof(Uniconta.DataModel.GLAccount));
            DebtorCache = company.GetCache(typeof(Uniconta.DataModel.Debtor));
            CreditorCache = company.GetCache(typeof(Uniconta.DataModel.Creditor));
            ProjectCache = company.GetCache(typeof(Uniconta.DataModel.Project));
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (company.RoundTo100)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = false;
            else
                Decimals = 2;
            if (master != null)
            {
                dgGlOffSetAccountTplt.UpdateMaster(master);
                var glOffSetAccLines = new List<GLOffsetAccountLineGridClient>();

                GLOffsetAccountLine[] glOffsetLines;
                if (glDailyJournalLine != null)
                {
                    MasterAmount = glDailyJournalLine.AmountCur == 0 ? glDailyJournalLine.Amount : glDailyJournalLine.AmountCur;
                    glOffsetLines = glDailyJournalLine.GetOffsetAccount(typeof(GLOffsetAccountLineGridClient));
                }
                else if (bankStatementLine != null)
                {
                    MasterAmount = bankStatementLine.AmountCur ?? bankStatementLine.Amount;
                    glOffsetLines = bankStatementLine.GetOffsetAccount(typeof(GLOffsetAccountLineGridClient));
                }
                else if (vouchersClient != null)
                {
                    MasterAmount = -vouchersClient._Amount;
                    glOffsetLines = vouchersClient.GetOffsetAccount(typeof(GLOffsetAccountLineGridClient));
                }
                else
                    glOffsetLines = null;
                if (glOffsetLines != null)
                {
                    foreach (var item in glOffsetLines)
                    {
                        item.SetMaster(company);
                        glOffSetAccLines.Add(item as GLOffsetAccountLineGridClient);
                    }
                }
                dgGlOffSetAccountTplt.ItemsSource = glOffSetAccLines;
                dgGlOffSetAccountTplt.Visibility = Visibility.Visible;
                RecalculateSum();
                btnAdd.Visibility = btnDelete.Visibility = btnEdit.Visibility = Visibility.Collapsed;
                UtilDisplay.RemoveMenuCommand(rb, "SaveGrid");
            }
            else
            {
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "SaveAndClose", "StatusAmt", "StatusSum", "StatusDiff", "LoadTemplate",
                "SaveTemplate" ,"DeleteTemplate"});
            }

            var TemplateCache = company.GetCache(typeof(Uniconta.DataModel.GLOffsetAccountTemplate));
            if (TemplateCache == null)
                BindComboBox();
            else
            {
                BindComboBox(TemplateCache);
            }
        }

        public override Task InitQuery()
        {
            return null;
        }

        public override bool IsDataChaged { get { return _master == null && base.IsDataChaged; } }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgGlOffSetAccountTplt.AddRow();
                    RecalculateSum();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "SaveAndClose":
                    SaveGridData();
                    break;
                case "DeleteRow":
                    dgGlOffSetAccountTplt.DeleteRow();
                    RecalculateSum();
                    break;
                case "CopyRow":
                    dgGlOffSetAccountTplt.CopyRow();
                    RecalculateSum();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void SaveGridData()
        {
            var lines = dgGlOffSetAccountTplt.ItemsSource as IEnumerable<GLOffsetAccountLine>;
            if (lines != null && lines.Count() == 0)
                lines = null;
            if (glDailyJournalLine != null)
            {
                glDailyJournalLine.SetOffsetAccount(lines);
                glDailyJournalLine.NotifyPropertyChanged("OffsetAccountList");
            }
            else
            {
                bankStatementLine?.SetOffsetAccount(lines);
                vouchersClient?.SetOffsetAccount(lines);
            }
            CloseDockItem();
        }

        protected override void OnLayoutLoaded()
        {
            if (!api.CompanyEntity.Project)
            {
                this.Project.Visible = this.Project.ShowInColumnChooser = false;
                this.PrCategory.Visible = this.PrCategory.ShowInColumnChooser = false;
                this.WorkSpace.Visible = this.WorkSpace.ShowInColumnChooser = false;
                this.Qty.Visible = this.Qty.ShowInColumnChooser = false;
            }
            base.OnLayoutLoaded();
            setDim();
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            GLOffsetAccountLineGridClient oldselectedItem = e.OldItem as GLOffsetAccountLineGridClient;
            if (oldselectedItem != null)
            {
                oldselectedItem.PropertyChanged -= GLOffSetAccountTemplateGridClient_PropertyChanged;
            }

            GLOffsetAccountLineGridClient selectedItem = e.NewItem as GLOffsetAccountLineGridClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += GLOffSetAccountTemplateGridClient_PropertyChanged;
                if (selectedItem.accntSource == null)
                    SetAccountSource(selectedItem);
            }
        }

        void copyDCAccount(GLOffsetAccountLineGridClient rec)
        {
            var cache = (rec._AccountType == GLJournalAccountType.Debtor) ? DebtorCache : CreditorCache;
            var dc = (DCAccount)cache?.Get(rec._Account);
            if (dc != null)
            {
                rec.Vat = null;
                if (dc._Dim1 != null)
                    rec.Dimension1 = dc._Dim1;
                if (dc._Dim2 != null)
                    rec.Dimension2 = dc._Dim2;
                if (dc._Dim3 != null)
                    rec.Dimension3 = dc._Dim3;
                if (dc._Dim4 != null)
                    rec.Dimension4 = dc._Dim4;
                if (dc._Dim5 != null)
                    rec.Dimension5 = dc._Dim5;
            }
        }

        void lookupProjectDim(GLOffsetAccountLineGridClient rec)
        {
            var proj = (Uniconta.DataModel.Project)ProjectCache?.Get(rec._Project);
            if (proj != null)
            {
                if (proj._Dim1 != null)
                    rec.Dimension1 = proj._Dim1;
                if (proj._Dim2 != null)
                    rec.Dimension2 = proj._Dim2;
                if (proj._Dim3 != null)
                    rec.Dimension3 = proj._Dim3;
                if (proj._Dim4 != null)
                    rec.Dimension4 = proj._Dim4;
                if (proj._Dim5 != null)
                    rec.Dimension5 = proj._Dim5;
            }
        }

        void GLOffSetAccountTemplateGridClient_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as GLOffsetAccountLineGridClient;
            switch (e.PropertyName)
            {
                case "AccountType":
                    SetAccountSource(rec);
                    break;
                case "Account":
                    if (rec._AccountType != GLJournalAccountType.Finans)
                        copyDCAccount(rec);
                    else
                    {
                        var Acc = (GLAccount)LedgerCache?.Get(rec._Account);
                        if (Acc != null)
                        {
                            if (Acc._MandatoryTax == VatOptions.NoVat)
                                rec.Vat = null;
                            else
                                rec.Vat = Acc._Vat;
                        }
                    }
                    break;
                case "Amount":
                    RecalculateSum();
                    break;
                case "Project":
                    lookupProjectDim(rec);
                    break;
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            if (LedgerCache == null)
                LedgerCache = await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            if (DebtorCache == null)
                DebtorCache = await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (CreditorCache == null)
                CreditorCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            if (ProjectCache == null && api.CompanyEntity.Project)
                ProjectCache = await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
            LoadType(typeof(Uniconta.DataModel.GLVat));
        }

        private void SetAccountSource(GLOffsetAccountLineGridClient rec)
        {
            SQLCache cache;
            switch (rec._AccountType)
            {
                case GLJournalAccountType.Finans:
                    cache = LedgerCache;
                    break;
                case GLJournalAccountType.Debtor:
                    cache = DebtorCache;
                    break;
                case GLJournalAccountType.Creditor:
                    cache = CreditorCache;
                    break;
                default: return;
            }
            if (cache != null)
            {
                int ver = cache.version + 10000 * ((int)rec._AccountType + 1);
                if (ver != rec.AccountVersion)
                {
                    rec.AccountVersion = ver;
                    rec.accntSource = cache.GetNotNullArray;
                    rec.NotifyPropertyChanged("AccountSource");
                }
            }
        }

        async Task BindComboBox()
        {
            var TemplateCache = api.GetCache(typeof(Uniconta.DataModel.GLOffsetAccountTemplate)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLOffsetAccountTemplate));
            BindComboBox(TemplateCache);
        }

        ObservableCollection<GLOffsetAccountTemplateClient> GlOffSetAccTemList;
        void BindComboBox(SQLCache TemplateCache)
        {
            GlOffSetAccTemList = new ObservableCollection<GLOffsetAccountTemplateClient>();
            foreach (var rec in (IEnumerable<Uniconta.DataModel.GLOffsetAccountTemplate>)TemplateCache.GetRecords)
            {
                if (rec != null)
                {
                    var r2 = new GLOffsetAccountTemplateClient();
                    StreamingManager.Copy(rec, r2);
                    GlOffSetAccTemList.Add(r2);
                }
            }
            cmbGLOffsetAccTempName.ItemsSource = GlOffSetAccTemList;
            cmbGLOffsetAccTempName.DisplayMember = "Name";
        }

        GLOffsetAccountTemplateClient masterRecord;
        private void cmbGLOffsetAccTempName_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbGLOffsetAccTempName.SelectedIndex == -1) return;
            if (cmbGLOffsetAccTempName.SelectedItem != null && GlOffSetAccTemList != null)
            {
                if (_master == null)
                    saveGrid();
                var glOffsetAccount = cmbGLOffsetAccTempName.SelectedItem as GLOffsetAccountTemplateClient;
                masterRecord = glOffsetAccount;
                dgGlOffSetAccountTplt.UpdateMaster(masterRecord);
                BindGrid();
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            CWAddGLOffsetAccountTemplate childWindow = new CWAddGLOffsetAccountTemplate(null);
            childWindow.Closing += async delegate
            {
                if (childWindow.DialogResult == true)
                {
                    var offsetAccTemp = new GLOffsetAccountTemplateClient();
                    offsetAccTemp._Name = childWindow.offSetAccountName;
                    busyIndicator.IsBusy = true;
                    var res = await api.Insert(offsetAccTemp);
                    busyIndicator.IsBusy = false;
                    if (res != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(res);
                    else
                        GlOffSetAccTemList.Add(offsetAccTemp);
                }
            };
            childWindow.Show();
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (masterRecord == null)
                return;

            CWAddGLOffsetAccountTemplate childWindow = new CWAddGLOffsetAccountTemplate(masterRecord.Name);
            childWindow.Closing += async delegate
            {
                if (childWindow.DialogResult == true)
                {
                    masterRecord._Name = childWindow.offSetAccountName;
                    busyIndicator.IsBusy = true;
                    var err = await api.Update(masterRecord);
                    busyIndicator.IsBusy = false;
                    if (err != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(err);
                }
            };
            childWindow.Show();
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (masterRecord == null) return;
            if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                busyIndicator.IsBusy = true;
                var err = await api.Delete(masterRecord);
                busyIndicator.IsBusy = false;
                if (err != ErrorCodes.Succes)
                    UtilDisplay.ShowErrorCode(err);
                else
                {
                    GlOffSetAccTemList.Remove(masterRecord);
                    masterRecord = null;
                    dgGlOffSetAccountTplt.ItemsSource = null;
                }
            }
        }

        private async void BindGrid()
        {
            await dgGlOffSetAccountTplt.Filter(null);
            if (MasterAmount != 0d)
            {
                var gridItems = (IEnumerable<GLOffsetAccountLineGridClient>)dgGlOffSetAccountTplt.ItemsSource;
                if (gridItems != null)
                {
                    foreach (var lin in gridItems)
                    {
                        if (lin._Pct != 0 && lin._Amount == 0)
                            lin._Amount = Math.Round(MasterAmount * lin.Pct / -100d, this.Decimals);
                    }
                    RecalculateSum();
                }
            }
        }

        CorasauGridLookupEditorClient prevAccount;
        private void Account_GotFocus(object sender, RoutedEventArgs e)
        {
            GLOffsetAccountLineGridClient selectedItem = dgGlOffSetAccountTplt.SelectedItem as GLOffsetAccountLineGridClient;
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

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        internal void RecalculateSum()
        {
            if (MasterAmount != 0)
            {
                var gridItems = (IEnumerable<GLOffsetAccountLineGridClient>)dgGlOffSetAccountTplt.ItemsSource;
                if (gridItems != null)
                {
                    var sumOffsetAmt = gridItems.Select(x => x._Amount).Sum();
                    const string format = "N2";
                    RibbonBase rb = (RibbonBase)localMenu.DataContext;
                    var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
                    var jrnlAmttxt = Uniconta.ClientTools.Localization.lookup("Amount");
                    var offSetAmtSum = Uniconta.ClientTools.Localization.lookup("Sum");
                    var diftxt = Uniconta.ClientTools.Localization.lookup("Dif");
                    foreach (var grp in groups)
                    {
                        if (grp.Caption == jrnlAmttxt)
                            grp.StatusValue = MasterAmount.ToString(format);
                        else if (grp.Caption == offSetAmtSum)
                            grp.StatusValue = sumOffsetAmt.ToString(format);
                        else if (grp.Caption == diftxt)
                        {
                            var diff = MasterAmount + sumOffsetAmt;
                            grp.StatusValue = diff.ToString(format);
                        }
                        else grp.StatusValue = string.Empty;
                    }
                }
            }
        }
        private void Account_LostFocus(object sender, RoutedEventArgs e)
        {
            SetAccountByLookupText(sender);
        }
        void SetAccountByLookupText(object sender)
        {
            var selectedItem = dgGlOffSetAccountTplt.SelectedItem as GLOffsetAccountLineGridClient;
            if (selectedItem == null)
                return;
            var actType = selectedItem._AccountType;
            if (actType != GLJournalAccountType.Finans)
                return;
            var le = sender as CorasauGridLookupEditor;
            if (string.IsNullOrEmpty(le.EnteredText))
                return;
            var accounts = LedgerCache?.GetNotNullArray as GLAccount[];
            if (accounts != null)
            {
                var act = accounts.Where(ac => string.Compare(ac._Lookup, le.EnteredText, StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                if (act != null)
                {
                    dgGlOffSetAccountTplt.SetLoadedRow(selectedItem);
                    selectedItem.Account = act.KeyStr;
                    le.EditValue = act.KeyStr;
                    dgGlOffSetAccountTplt.SetModifiedRow(selectedItem);
                }
            }
            le.EnteredText = null;
        }
    }

    public class GLOffsetAccountLineGridClient : GLOffsetAccountLineClient
    {
        internal int AccountVersion;
        internal object accntSource;
        public object AccountSource { get { return accntSource; } }
    }
}
