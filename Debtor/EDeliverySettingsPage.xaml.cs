using DevExpress.Utils;
using DevExpress.Utils.Extensions;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.WindowsAPI.ClientTools.DataModel.DC;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EDeliverySettingWithValueClient : EDeliverySettingClient
    {
        private string _value;

        [Display(Name = "Value", ResourceType = typeof(TableFieldsText))]
        public string Value
        {
            get => _value ?? _DefaultValue;
            set
            {
                _value = value;
                NotifyPropertyChanged("Value");
            }
        }

        private static List<EDeliverySettingWithValueClient> testList;
        private static int idTags = 0;
        public static EDeliverySettingWithValueClient[] Test()
        {
            testList = new List<EDeliverySettingWithValueClient>();
            CreateProperty(typeof(Invoice), null);
            var arrayResult = testList.ToArray();
            Array.Sort(arrayResult, new EDeliverySettingSort(arrayResult));
            var idList = arrayResult.Select(x => x.Id);
            return arrayResult;
        }

        private static void CreateProperty(Type type,
            EDeliverySettingWithValueClient parent)
        {
            if (type?.FullName == null || !type.FullName.Contains("Uniconta.WindowsAPI.ClientTools.DataModel.DC"))
                return;

            var properties = type.GetProperties();
            if (properties == null || properties.Length == 0)
                return;

            foreach (var prop in properties)
            {
                var propertyInfo = prop.PropertyType;
                var isArrayType = propertyInfo?.BaseType == typeof(Array);
                propertyInfo = isArrayType ? propertyInfo?.GetElementType() : propertyInfo;

                var tagName = isArrayType ? propertyInfo?.Name : prop.Name;
                if (tagName == parent?.TagName)
                    continue;

                var documentSettings = new EDeliverySettingWithValueClient
                {
                    _Id = ++idTags,
                    _ParentId = parent?.Id ?? 0,
                    _TagName = tagName,
                    _TypeVersion = XmlTypeVersion.OIOUBL_21,
                    _DocumentType = XmlTypeDocument.Invoice,
                    _IsArrayType = isArrayType,
                    _DefaultProperty = "Test",
                    _DefaultValue = "Test",
                    _IsEditable = true
                };

                testList.Add(documentSettings);

                var baseType = propertyInfo?.BaseType;
                if (baseType != null && baseType == typeof(System.Object))
                    CreateProperty(propertyInfo, documentSettings);
            }
        }
    }

    public class EDeliverySettingsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EDeliverySettingWithValueClient); } }
        public override IComparer GridSorting { get { return new EDeliverySettingSort(); } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return true; } }
        public override bool IsAutoSave { get { return true; } }
        public override bool ShowTreeListView => true;

        public override TreeListView SetTreeListViewFromPage
        {
            get
            {
                var tv = base.SetTreeListViewFromPage;
                tv.KeyFieldName = "Id";
                tv.ShowIndicator = false;
                tv.ParentFieldName = "ParentId";
                tv.TreeDerivationMode = TreeDerivationMode.Selfreference;
                tv.ShowTotalSummary = false;
                return tv;
            }
        }
    }

    public partial class EDeliverySettingsPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EDeliverySettingsPage; } }
        UnicontaBaseEntity master;

        public EDeliverySettingsPage(BaseAPI api) : base(api, string.Empty) => Init();
        public EDeliverySettingsPage(UnicontaBaseEntity entity) : base(null)
        {
            Init();

            master = entity;
            if (master != null)
            {
                // TODO: This need to change when we have support to generate documents for other than OIOUBL
                if (master is DCInvoice invoice)
                    txtInvoice.Text = invoice.InvoiceNum;

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
                    key = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), invoice.InvoiceNum);
            }

            if (string.IsNullOrEmpty(key))
                return;

            var header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("eDeliverySettings"), key);
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

            dgEdelivery.Loaded += DgEdelivery_Loaded;
            RefreshGrid();
        }

        bool firstLoad;
        private void DgEdelivery_Loaded(object sender, RoutedEventArgs e)
        {
            if (!firstLoad)
            {
                dgEdelivery.treeListView.ExpandAllNodes();
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
            var mappingData = dgEdelivery.ItemsSource as EDeliverySettingWithValueClient[];
            if (mappingData == null || mappingData.Length == 0)
            {
                mappingData = EDeliverySettingWithValueClient.Test();
            }

            busyIndicator.IsBusy = true;
            dgEdelivery.Visibility = Visibility.Hidden;

            dgEdelivery.ItemsSource = null;

            UpdateMaster();
            SetMasterDataOnSettings(mappingData);

            if (mappingData?.Length > 0)
            {
                Array.Sort(mappingData, new EDeliverySettingSort(mappingData));
                var maxId = mappingData.Select(s => s.Id).Max();
                foreach (var mapping in mappingData)
                {
                    if (mapping.Id == 0)
                        mapping.Id = ++maxId;
                }
                dgEdelivery.MaxId = maxId;
                dgEdelivery.SetSource(mappingData);
            }

            dgEdelivery.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
        }

        private void UpdateMaster()
        {
            var invoiceText = txtInvoice?.Text;
            if (string.IsNullOrEmpty(invoiceText) || !Regex.IsMatch(invoiceText, @"^\d+$"))
                return;

            var invoiceNumber = Convert.ToInt64(invoiceText);
            if (master == null || (master is DebtorInvoice invoice && invoice._InvoiceNumber != invoiceNumber))
            {
                var propValPair = new List<PropValuePair>
                {
                    PropValuePair.GenereteWhereElements("InvoiceNumber", invoiceNumber, CompareOperator.Equal)
                };

                master = api.Query<DebtorInvoiceClient>(propValPair).Result.First();
            }
        }

        private void SetMasterDataOnSettings(EDeliverySettingWithValueClient[] settings)
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
        }

        private void Value_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedItem = dgEdelivery.SelectedItem as EDeliverySettingWithValueClient;
            var txtblock = sender as TextBlock;
            if (selectedItem != null && !selectedItem.IsEditable)
            {
                var tip = new ToolTip
                {
                    Content = Uniconta.ClientTools.Localization.lookup("CannotChangeFieldOBJ"),
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
