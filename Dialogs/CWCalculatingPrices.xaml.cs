using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.ClientTools.Page;
using System.ComponentModel.DataAnnotations;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWCalculatingPrices : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.NumberSerie))]
        [InputFieldData]
        [Display(Name = "NumberSerie", ResourceType = typeof(InputFieldDataText))]
        public string NumberSeries { get; set; }
        [InputFieldData]
        [Display(Name = "PostingDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime PostingDate { get; set; }
        [InputFieldData]
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public string Comment { get; set; }
        [InputFieldData]
        [Display(Name = "NoCorrectionsBefore", ResourceType = typeof(InputFieldDataText))]
        public DateTime NoCorrectionsBefore { get; set; }
        [InputFieldData]
        [Display(Name = "PostingPer", ResourceType = typeof(InputFieldDataText))]
        public bool AllJournal { get; set; }

        static bool KeepCostprices;
        [InputFieldData]
        [Display(Name = "UpdateCost", ResourceType = typeof(InputFieldDataText))]
        public bool UpdateCost { get { return !KeepCostprices; } set { KeepCostprices = !value; } }
        CrudAPI _api;

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }

        public CWCalculatingPrices(CrudAPI api)
        {
            InitializeComponent();
            _api = api;
            leNumberSeries.api = api;
            this.Title = $"{Uniconta.ClientTools.Localization.lookup("RecalculateCostPrices")} ({Uniconta.ClientTools.Localization.lookup("AlwaysCheckUnipedia")})";
            txtPostingPer.Text = $"{Uniconta.ClientTools.Localization.lookup("PostingPer")} ({Uniconta.ClientTools.Localization.lookup("UseOnlyInvoice")})";
            this.DataContext = this;
            cmbPostingPer.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Invoice"), Uniconta.ClientTools.Localization.lookup("Journal") };
            this.Loaded += CWCalculatingPrices_Loaded;
            txtUpdateCost.Text = string.Format(Uniconta.ClientTools.Localization.lookup("UpdateOBJ"), Uniconta.ClientTools.Localization.lookup("CostPrices")) + " (" + Uniconta.ClientTools.Localization.lookup("ItemTable") + ")";
        }

        public CWCalculatingPrices(CrudAPI api, DateTime financialStartDate) : this(api)
        {
            NoCorrectionsBefore = financialStartDate;
        }

        private void CWCalculatingPrices_Loaded(object sender, RoutedEventArgs e)
        {
            cmbPostingPer.SelectedIndex = !AllJournal ? 0 : 1;
            SetSource();
            Dispatcher.BeginInvoke(new Action(() => { leNumberSeries.Focus(); }));
        }

        async private void SetSource()
        {
            var api = this._api;
            var Comp = api.CompanyEntity;
            var numSeriesCache = Comp.GetCache(typeof(Uniconta.DataModel.NumberSerie)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.NumberSerie), api);
            leNumberSeries.ItemsSource = numSeriesCache;
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void cmbPostingPer_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbPostingPer.SelectedIndex >= 0)
                AllJournal = cmbPostingPer.SelectedIndex != 0;
        }
    }
}
