using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EDeliveryMappingGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EDeliveryMappingClient); } }
        public override IComparer GridSorting { get { return new EDeliveryMappingSort(); } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return true; } }
        public override bool IsAutoSave { get { return true; } }
        public override bool ShowTreeListView => true;

        public override TreeListView SetTreeListViewFromPage
        {
            get
            {
                var tv = base.SetTreeListViewFromPage;
                tv.KeyFieldName = "RowId";
                tv.ShowIndicator = false;
                tv.ParentFieldName = "ParentRowId";
                tv.TreeDerivationMode = TreeDerivationMode.Selfreference;
                tv.ShowTotalSummary = false;
                return tv;
            }
        }
    }

    public partial class EDeliveryMappingPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EDeliveryMappingPage; } }
        UnicontaBaseEntity master;

        public EDeliveryMappingPage(BaseAPI api) : base(api, string.Empty) => Init();
        public EDeliveryMappingPage(UnicontaBaseEntity entity) : base(null)
        {
            master = entity;
            Init();

            if (entity != null)
            {
                // TODO: This need to change when we have support to generate documents for other than OIOUBL
                if (master is DCInvoice invoice)
                {
                    txtInvoice.Text = invoice.InvoiceNum;
                    cmbDocumentType.SelectedItem = AppEnums.EDeliveryDocumentType.ToString((int)XmlTypeDocument.Invoice);
                }

                SetHeader();
            }
        }

        private void SetHeader()
        {
            var key = "";
            if (master != null)
            {
                // TODO: This need to change when we have support to generate documents for other than OIOUBL
                if (master is DCInvoice invoice)
                    key = string.Format("{0} : {1}", Localization.lookup("Invoice"), invoice.InvoiceNum);
            }

            if (string.IsNullOrEmpty(key))
                return;

            var header = string.Format("{0} : {1}", Localization.lookup("EDeliveryMapping"), key);
            SetHeader(header);
        }

        void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgEdelivery;
            dgEdelivery.api = api;
            SetRibbonControl(localMenu, dgEdelivery);
            dgEdelivery.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;

            cmbTypeVersion.ItemsSource = AppEnums.EDeliveryTypeVersion.Values;
            cmbDocumentType.ItemsSource = AppEnums.EDeliveryDocumentType.Values;

            cmbTypeVersion.SelectedIndex = 0;

            dgEdelivery.Loaded += DgEdelivery_Loaded;
        }

        bool firstLoad;
        private void DgEdelivery_Loaded(object sender, RoutedEventArgs e)
        {
            if (!firstLoad)
            {
                dgEdelivery.treeListView.CollapseAllNodes();
                firstLoad = true;
            }
        }

        void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "RefreshGrid":
                    RefreshGrid();
                    break;
                case "GenerateOioXml":
                    break;
                case "SendUBL":
                    break;
                case "SaveLayout":
                    break;
                case "ExpandAll":
                    dgEdelivery.treeListView.ExpandAllNodes();
                    break;
                case "CollapseAll":
                    dgEdelivery.treeListView.CollapseAllNodes();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override async Task InitQuery() => RefreshGrid();

        private void RefreshGrid()
        {
            UpdateMaster();
            if (master == null || !(master is DebtorInvoiceClient masterInvoice))
                masterInvoice = new DebtorInvoiceClient();

            var mappingData = new NHRAPI(api).GetInvoiceOIOUBL(masterInvoice).Result;
            if (mappingData?.Length == 0)
                return;

            busyIndicator.IsBusy = true;
            dgEdelivery.Visibility = Visibility.Hidden;
            dgEdelivery.ItemsSource = null;

            Array.Sort(mappingData, new EDeliveryMappingSort(mappingData));

            dgEdelivery.MaxId = mappingData.Select(s => s.RowId).Max();
            dgEdelivery.SetSource(mappingData);

            dgEdelivery.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
        }

        private void UpdateMaster()
        {
            var invoiceText = txtInvoice?.Text;
            if (string.IsNullOrEmpty(invoiceText) || !Regex.IsMatch(invoiceText, @"^\d+$"))
                return;

            var invoiceNumber = Convert.ToInt64(invoiceText);
            if (master == null || !(master is DebtorInvoiceClient invoice) || invoice._InvoiceNumber != invoiceNumber)
            {
                var propValPair = new List<PropValuePair>
                {
                    PropValuePair.GenereteWhereElements("InvoiceNumber", invoiceNumber, CompareOperator.Equal)
                };

                master = api.Query<DebtorInvoiceClient>(propValPair).Result.First();
            }
        }

        /*private void SetMasterDataOnSettings(EDeliverySettingWithValueClient[] settings)
        {
            if (master == null)
                return;

            var typeVersion = cmbTypeVersion.Text;
            var documentType = cmbDocumentType.Text;
            var masterProperties = master.GetType().GetProperties();

            settings
                .Where(line => line.TypeVersion == typeVersion && line.DocumentType == documentType)
                .ForEach(line => SetMasterPropertyValueOnSetting(line, masterProperties));
        }

        private void SetMasterPropertyValueOnSetting(EDeliverySettingWithValueClient setting,
            PropertyInfo[] masterProperties)
        {
            object val = null;
            masterProperties
                .Where(p => p.Name == setting.Property)
                .ForEach(p => val = p.GetValue(master));

            if (val != null)
                setting.Value = val.ToString();

            if (setting.Value != null && setting.Format != null)
                setting.Value = string.Format(setting.Format, setting.Value);
        }*/

        private void Value_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedItem = dgEdelivery.SelectedItem as EDeliveryMappingClient;
            var txtblock = sender as TextBlock;
            if (selectedItem != null && !selectedItem.IsEditable)
            {
                var tip = new ToolTip
                {
                    Content = Localization.lookup("CannotChangeFieldOBJ"),
                    IsOpen = true,
                    PlacementTarget = txtblock,
                    Placement = System.Windows.Controls.Primitives.PlacementMode.Right
                };

                ToolTipService.SetToolTip(txtblock, tip);
                return;
            }

            ToolTipService.SetToolTip(txtblock, null);
        }

        private void Value_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var txtblock = sender as TextBlock;
            var tooltip = ToolTipService.GetToolTip(txtblock) as ToolTip;
            if (tooltip != null)
                tooltip.IsOpen = false;
        }
    }
}
