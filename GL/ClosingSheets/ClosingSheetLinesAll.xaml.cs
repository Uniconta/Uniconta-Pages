using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class GLClosingSheetLineAllGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLClosingSheetLineLocal); } }
        public override bool Readonly { get { return false; } }
        public override IComparer GridSorting { get { return new GLClosingSheetLineSort(); } }
        
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (GLClosingSheetLineLocal)this.SelectedItem;
            return (selectedItem != null && selectedItem._AmountCent != 0);
        }
    }

    public partial class ClosingSheetLinesAll : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ClosingSheetLinesAll; } }
        bool RoundTo100;
        DateTime postingDate;

        public ClosingSheetLinesAll(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgClosingSheetLineAll);
            dgClosingSheetLineAll.api = api;
            dgClosingSheetLineAll.BusyIndicator = busyIndicator;
            dgClosingSheetLineAll.UpdateMaster(master);
            var ClosingMaster = master as GLClosingSheet;
            if (ClosingMaster != null)
                postingDate = ClosingMaster._ToDate;
            else
                postingDate = BasePage.GetSystemDefaultDate();

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgClosingSheetLineAll.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged1;
            var Comp = api.CompanyEntity;
            RoundTo100 = Comp.RoundTo100;
            if (RoundTo100)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = false;
            this.BeforeClose += ClosingSheetLines_BeforeClose;
        }

        public async override Task InitQuery()
        {
            await dgClosingSheetLineAll.Filter(null);
            RecalculateSum();
        }

        private void DataControl_CurrentItemChanged1(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as GLClosingSheetLineLocal;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= ClosingSheetLine_PropertyChanged;

            var selectedItem = e.NewItem as GLClosingSheetLineLocal;
            if (selectedItem != null)
                selectedItem.PropertyChanged += ClosingSheetLine_PropertyChanged;
        }

        void ClosingSheetLine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Uniconta.DataModel.GLTransType t;
            var rec = sender as GLClosingSheetLineLocal;
            switch (e.PropertyName)
            {
                case "Text":
                    t = UnicontaClient.Pages.GLDailyJournalLine.getTransText(rec._Text, TextTypes);
                    if (t != null)
                    {
                        rec.Text = t._TransType;
                        if (t._Account != null && t._AccountType == 0)
                            rec.Account = t._Account;
                        if (t._OffsetAccount != null && t._OffsetAccountType == 0)
                            rec.OffsetAccount = t._OffsetAccount;
                    }
                    break;
                case "OffsetText":
                    t = UnicontaClient.Pages.GLDailyJournalLine.getTransText(rec._OffsetText, TextTypes);
                    if (t != null)
                    {
                        rec.OffsetText = t._TransType;
                        if (t._Account != null && t._AccountType == 0)
                            rec.OffsetAccount = t._Account;
                        if (t._OffsetAccount != null && t._OffsetAccountType == 0)
                            rec.Account = t._OffsetAccount;
                    }
                    break;
                case "Amount":
                case "Debit":
                case "Credit":
                case "Account":
                case "OffsetAccount":
                case "Vat":
                case "OffsetVat":
                    RecalculateSum();
                    break;
            }
        }

        internal void RecalculateSum()
        {
            var gridItems = (IEnumerable<GLClosingSheetLineLocal>)dgClosingSheetLineAll.ItemsSource;
            if (gridItems == null)
                return;

            long deb = 0, cre = 0;
            foreach (var lin in gridItems)
            {
                if (lin._Account != null)
                {
                    if (lin._AmountCent > 0)
                        deb += lin._AmountCent;
                    else
                        cre -= lin._AmountCent;

                    var vatValue = ClosingSheetLines.CalcVat(lin._Vat, lin._Amount, postingDate, RoundTo100, VatCache);
                    lin.SetVatAmount(vatValue);
                }
                if (lin._OffsetAccount != null)
                {
                    if (lin._Amount > 0)
                        cre += lin._AmountCent;
                    else
                        deb -= lin._AmountCent;
                    var vatValue = ClosingSheetLines.CalcVat(lin._OffsetVat, lin._Amount, postingDate, RoundTo100, VatCache);
                    lin.SetVatAmountOffset(-vatValue);
                }
                lin.SetTotal(deb - cre);
            }

            string format = RoundTo100 ? "N0" : "N2";
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);

            var debtxt = Uniconta.ClientTools.Localization.lookup("Debit");
            var cretxt = Uniconta.ClientTools.Localization.lookup("Credit");
            var diftxt = Uniconta.ClientTools.Localization.lookup("Dif");
            foreach (var grp in groups)
            {
                if (grp.Caption == debtxt)
                    grp.StatusValue = (deb / 100d).ToString(format);
                else if (grp.Caption == cretxt)
                    grp.StatusValue = (cre / 100d).ToString(format);
                else if (grp.Caption == diftxt)
                    grp.StatusValue = ((deb - cre) / 100d).ToString(format);
                else grp.StatusValue = string.Empty;
            }
        }

        SQLCache TextTypes, VatCache;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            VatCache = Comp.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLVat), api).ConfigureAwait(false);
            TextTypes = Comp.GetCache(typeof(Uniconta.DataModel.GLTransType)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLTransType), api).ConfigureAwait(false);
        }

        private void ClosingSheetLines_BeforeClose()
        {
            globalEvents.OnRefresh(TabControls.ClosingSheetLinesAll);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgClosingSheetLineAll.AddRow();
                    break;
                case "CopyRow":
                    dgClosingSheetLineAll.CopyRow();
                    RecalculateSum();
                    break;
                case "SaveGrid":
                    dgClosingSheetLineAll.SaveData();
                    break;
                case "DeleteRow":
                    dgClosingSheetLineAll.DeleteRow();
                    RecalculateSum();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var api = this.api;
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, colOffsetdim1, colOffsetdim2, colOffsetdim3, colOffsetdim4, colOffsetdim5);

            var offsetName = Uniconta.ClientTools.Localization.lookup("OffsetAccount");
            var c = api.CompanyEntity;
            colOffsetdim1.Header = string.Format("{1} ({0})", offsetName, c._Dim1);
            colOffsetdim2.Header = string.Format("{1} ({0})", offsetName, c._Dim2);
            colOffsetdim3.Header = string.Format("{1} ({0})", offsetName, c._Dim3);
            colOffsetdim4.Header = string.Format("{1} ({0})", offsetName, c._Dim4);
            colOffsetdim5.Header = string.Format("{1} ({0})", offsetName, c._Dim5);
        }

    }
}
