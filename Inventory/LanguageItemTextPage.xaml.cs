using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using Uniconta.API.System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class LanguageItemTextPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvItemTextClientGrid); } }
        public override IComparer GridSorting { get { return new InvItemTextSort(); } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
        public override bool SingleBufferUpdate { get { return false; } }
        protected override List<string> GridSkipFields { get { return new List<string>(1) { "ItemName" }; } }

        public override bool AddRowOnPageDown()
        {
            var selectedItem = (InvItemTextClientGrid)this.SelectedItem;
            return (selectedItem?._Item != null && (selectedItem._Number != null || selectedItem._Text != null));
        }
    }

    public partial class LanguageItemTextPage : GridBasePage
    {
        SQLCache items;


        public LanguageItemTextPage(CrudAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
        }

        public LanguageItemTextPage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public LanguageItemTextPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgLanguageItemTextPageGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgLanguageItemTextPageGrid.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("LanguageItemText"), key);
            SetHeader(header);
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            string masterValue = null;
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "MasterName", StringComparison.CurrentCultureIgnoreCase) == 0)
                    masterValue = rec.Value;
            }

            if (masterValue != null)
            {
                var cache = api.GetCache(typeof(Uniconta.DataModel.InvItemNameGroup)) ?? api.LoadCache(typeof(Uniconta.DataModel.InvItemNameGroup)).GetAwaiter().GetResult();
                var master = cache.Get<InvItemNameGroup>(masterValue);
                InitPage(master);
            }

            base.SetParameter(Parameters);
        }

        void InitPage(UnicontaBaseEntity masterRecord)
        {
            dgLanguageItemTextPageGrid.UpdateMaster(masterRecord);
            ((TableView)dgLanguageItemTextPageGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            dgLanguageItemTextPageGrid.api = api;
            SetRibbonControl(localMenu, dgLanguageItemTextPageGrid);
            dgLanguageItemTextPageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgLanguageItemTextPageGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            var Comp = api.CompanyEntity;
            this.items = Comp.GetCache(typeof(InvItem));
            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }

        private void DataControl_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as InvItemTextClientGrid;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= SelectedItem_PropertyChanged;

            var selectedItem = e.NewItem as InvItemTextClientGrid;
            if (selectedItem != null)
                selectedItem.PropertyChanged += SelectedItem_PropertyChanged;
        }

        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (InvItemTextClientGrid)sender;
            switch (e.PropertyName)
            {
                case "Item":
                    var selectedItem = (InvItem)items.Get(rec._Item);
                    if (selectedItem != null)
                    {
                        rec.UpdateDefaultText();
                        globalEvents?.NotifyRefreshViewer(NameOfControl, selectedItem);
                    }
                    break;
                case "EAN":
                    if (!UnicontaClient.Utilities.Utility.IsValidEAN(rec.EAN, api.CompanyEntity))
                    {
                        UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("EANinvalid"), rec.EAN), Uniconta.ClientTools.Localization.lookup("Error"));
                        rec.EAN = null;
                    }
                    break;
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool ShowDC = true, ShowItem = true;
            var master = dgLanguageItemTextPageGrid.masterRecords?.First();
            if (master is Uniconta.DataModel.InvItemNameGroup || master is Uniconta.DataModel.DCAccount)
                ShowDC = false;
            if (master is Uniconta.DataModel.InvItem)
                ShowItem = false;

            ItemName.Visible = ShowItem;
            Item.ReadOnly = !ShowItem;
            ItemNameGroup.Visible = ShowDC;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgLanguageItemTextPageGrid.AddRow();
                    break;
                case "CopyRow":
                    dgLanguageItemTextPageGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgLanguageItemTextPageGrid.SaveData();
                    break;
                case "DeleteRow":
                    dgLanguageItemTextPageGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (this.items == null)
                this.items = await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api).ConfigureAwait(false);
        }
    }
    public class InvItemTextClientGrid : InvItemTextClient
    {
        internal object locationSource;
        public object LocationSource { get { return locationSource; } set { locationSource = value; } }
        public void UpdateDefaultText()
        {
            this.NotifyPropertyChanged("DefaultText");
        }
        public string DefaultText
        {
            get
            {
                return ItemName;
            }
        }
    }
}
