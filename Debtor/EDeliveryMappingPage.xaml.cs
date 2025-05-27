using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EDeliveryMappingClientExtended : eDeliveryMappingClient
    {
        private string _Value;

        [Display(Name = "Value", ResourceType = typeof(TableFieldsText))]
        public string Value
        {
            get => _Value;
            set
            {
                _Value = value;
                NotifyPropertyChanged("Value");
            }
        }
    }

    public class EDeliveryMappingSort : IComparer
    {
        public int Compare(object x, object y)
        {
            var xTag = ((EDeliveryMappingClientExtended)x).eDeliveryTag.Name;
            var yTag = ((EDeliveryMappingClientExtended)y).eDeliveryTag.Name;
            return xTag.CompareTo(yTag);
        }
    }

    public class EDeliveryMappingGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EDeliveryMappingClientExtended); } }
        public override IComparer GridSorting { get { return new EDeliveryMappingSort(); } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
        public override bool ClearSelectedItemOnSave { get { return false; } }
        public override bool AllowSave { get { return true; } }
        public override bool SingleBufferUpdate { get { return false; } }
    }

    public partial class EDeliveryMappingPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EDeliveryMappingPage; } }
        private UnicontaBaseEntity entity;
        private eDeliveryMappingGroupClient master;

        private SQLCache xmlTagsCache;
        private List<eDeliveryTagTypeClient> unusedXmlTags;

        public EDeliveryMappingPage(BaseAPI api) : base(api, string.Empty) => Init();
        public EDeliveryMappingPage(UnicontaBaseEntity entity) : base(null)
        {
            this.entity = entity;
            Init();
        }

        private async void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgEdeliveryMappingGrid;
            dgEdeliveryMappingGrid.api = api;
            SetRibbonControl(localMenu, dgEdeliveryMappingGrid);
            dgEdeliveryMappingGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            layOutEdeliveryMapping.Caption = Localization.lookup("EDeliveryMapping");

            xmlTagsCache = api.CompanyEntity.GetCache(typeof(eDeliveryTagTypeClient)) ??
                await api.CompanyEntity.LoadCache(typeof(eDeliveryTagTypeClient), api);

            var mapppingGrpCache = api.CompanyEntity.GetCache(typeof(eDeliveryMappingGroupClient)) ??
                await api.CompanyEntity.LoadCache(typeof(eDeliveryMappingGroupClient), api);

            if (mapppingGrpCache == null || mapppingGrpCache.Count == 0)
                CreateMappingGroup();
            else
            {
                leMappinggroup.ItemsSource = mapppingGrpCache;
                leMappinggroup.SelectedItem = mapppingGrpCache.GetNotNullArray
                    .FirstOrDefault(g => ((eDeliveryMappingGroupClient)g).IsDebtorGroup);
                if (leMappinggroup.SelectedItem == null)
                    leMappinggroup.SelectedItem = mapppingGrpCache.First();
            }
        }

        void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgEdeliveryMappingGrid.SelectedItem as EDeliveryMappingClientExtended;
            switch (ActionType)
            {
                case "AddRow":
                    var rowAdd = dgEdeliveryMappingGrid.AddRow() as EDeliveryMappingClientExtended;
                    rowAdd.Group = ((eDeliveryMappingGroup)leMappinggroup.SelectedItem)?.KeyName;
                    rowAdd.CompanyId = api.CompanyId;
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                    {
                        var eDeliveryTag = selectedItem.eDeliveryTag;
                        if (eDeliveryTag != null)
                            unusedXmlTags.Add(eDeliveryTag);

                        dgEdeliveryMappingGrid.DeleteRow();
                    }
                    break;
                case "SaveGrid":
                    if (dgEdeliveryMappingGrid.ItemsSource is List<EDeliveryMappingClientExtended> mappings &&
                        mappings != null)
                    {
                        if (mappings.Any(m => m.IsMissingValueSet))
                        {
                            UnicontaMessageBox.Show(Localization.lookup(ErrorCodes.FieldCannotBeBlank.ToString()), Localization.lookup("Error"));
                            return;
                        }

                        gridRibbon_BaseActions(ActionType);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void liMappinggroup_ButtonClicked(object sender) => CreateMappingGroup();
        private void CreateMappingGroup()
        {
            /*var cwEDeliveryMapping = new CWeDeliveryMappingDebtorGroupPage(api);
            cwEDeliveryMapping.Closed += delegate
            {
                if (cwEDeliveryMapping.DialogResult == true)
                {
                    var newMappingGroup = cwEDeliveryMapping.MappingGroup;
                    if (newMappingGroup == null)
                        return;

                    var groups = ((List<eDeliveryMappingGroupClient>)leMappinggroup.ItemsSource) ?? new List<eDeliveryMappingGroupClient>();
                    if (!groups.Contains(newMappingGroup))
                        groups.Add(newMappingGroup);

                    var mappings = ((List<EDeliveryMappingClientExtended>)dgEdeliveryMappingGrid.ItemsSource) ?? new List<EDeliveryMappingClientExtended>();
                    dgEdeliveryMappingGrid.ItemsSource = mappings.Where(m => m.Group == newMappingGroup.Name).ToList();
                    leMappinggroup.ItemsSource = groups;
                    leMappinggroup.SelectedItem = newMappingGroup;
                }
            };
            cwEDeliveryMapping.Show();*/
        }

        private async void leMappinggroup_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var newMappingGroup = (sender as LookupEditor).SelectedItem as eDeliveryMappingGroupClient;
            if (newMappingGroup == null || master == newMappingGroup)
                return;

            master = newMappingGroup;
            var docType = newMappingGroup._DocType;

            UnicontaBaseEntity[] result = null;
            if (docType == eDeliveryDocumentType.Invoice || docType == eDeliveryDocumentType.Creditnote)
            {
                var compareOp = docType == eDeliveryDocumentType.Invoice ?
                    CompareOperator.GreaterThan : CompareOperator.LessThan;

                var temp = (await api.Query<DebtorInvoiceClient>(
                    new PropValuePair[2]
                    {
                        PropValuePair.GenereteOrderByElement("Date", true),
                        //PropValuePair.GenereteWhereElements("LineAmount", 0L, compareOp),
                        PropValuePair.GenereteTakeN(10),
                    }))?
                    .ToList();

                if (temp == null || temp.Count == 0)
                    return;

                var isValidInvoiceOrCreditNote = entity is DebtorInvoiceClient inv &&
                    ((docType == eDeliveryDocumentType.Invoice && inv.TotalAmount > 0) ||
                     (docType == eDeliveryDocumentType.Creditnote && inv.TotalAmount < 0));

                if (isValidInvoiceOrCreditNote)
                    temp.Add((DebtorInvoiceClient)entity);
                else
                    entity = temp?.FirstOrDefault();

                result = temp.ToArray();
            }
            if (entity == null)
                return;

            tbDocumentNum.Text = master.DocType;
            tbDocumentNum.Visibility = Visibility.Visible;

            leDocumentNum.ItemsSource = result;
            leDocumentNum.SelectedItem = entity;
            leDocumentNum.Visibility = Visibility.Visible;

            var mappings = ((List<EDeliveryMappingClientExtended>)dgEdeliveryMappingGrid.ItemsSource) ??
                new List<EDeliveryMappingClientExtended>();

            dgEdeliveryMappingGrid.ItemsSource = mappings.Where(m => m.Group == master.Name).ToList();
            unusedXmlTags = xmlTagsCache.GetNotNullArray
                .Select(r => (eDeliveryTagTypeClient)r)
                .Where(t => !mappings.Any(m => m.Tag == t.RowId))
                .Where(t => t._DocVersion == master?._DocVersion && t._DocVersion == master?._DocVersion)
                .ToList();
        }

        private void cmbXmlTag_GotFocus(object sender, RoutedEventArgs e) =>
            ((ComboBoxEditor)sender).ItemsSource = unusedXmlTags?.Select(t => t.Name).OrderBy(x => x)?.ToList();

        private void cmbXmlTag_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (unusedXmlTags == null || unusedXmlTags.Count == 0)
                return;

            var rec = dgEdeliveryMappingGrid.SelectedItem as EDeliveryMappingClientExtended;
            var tag = ((ComboBoxEditor)sender).SelectedItem as string;
            rec.Tag = unusedXmlTags?.FirstOrDefault(t => t.Name == tag)?.RowId ?? 0;
            if (unusedXmlTags.Count == 1)
            { 
                System.Windows.MessageBox.Show("All tags used");
                unusedXmlTags = new List<eDeliveryTagTypeClient>();
            }
            else
                unusedXmlTags.Remove(rec.eDeliveryTag);
        }

        private void cbTable_GotFocus(object sender, RoutedEventArgs e) =>
            ((ComboBoxEditor)sender).ItemsSource = master?.GetTableIdNames()?.Values.OrderBy(x => x)?.ToList();

        bool tableChanged = false;
        private void cbTable_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var row = dgEdeliveryMappingGrid.SelectedItem as EDeliveryMappingClientExtended;
            row.Table = ((ComboBoxEditor)sender).SelectedItem as string;
            tableChanged = true;
        }

        private void cbProperty_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!tableChanged)
                return;

            var row = dgEdeliveryMappingGrid.SelectedItem as EDeliveryMappingClientExtended;
            var values = master.GetTableProperties(api.CompanyEntity, row.Table);
            if (values != null && values.Count > 0)
                ((ComboBoxEditor)sender).ItemsSource = values.OrderBy(x => x).ToList();

            tableChanged = false;
        }

        private void cbProperty_SelectedIndexChanged(object sender, RoutedEventArgs e) =>
            SetValue((sender as ComboBoxEditor).SelectedItem as string);

        private void leDocumentNum_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (master == null)
                return;

            var uniEntity = leDocumentNum.SelectedItem as UnicontaBaseEntity;
            var isValidInvoiceOrCreditNote = uniEntity is DebtorInvoiceClient inv &&
                              ((master._DocType == eDeliveryDocumentType.Invoice && inv.TotalAmount > 0) ||
                               (master._DocType == eDeliveryDocumentType.Creditnote && inv.TotalAmount < 0));

            if (isValidInvoiceOrCreditNote)
            {
                entity = uniEntity;
                SetValue(null);
            }
        }

        private void SetValue(string property)
        {
            var rec = dgEdeliveryMappingGrid.SelectedItem as EDeliveryMappingClientExtended;
            if (rec == null)
                return;
            if (!string.IsNullOrEmpty(property))
                rec.Property = property;

            rec.Value = rec.GetTablePropertyValueFromEntity(entity, api.CompanyEntity);
            Value.Visible = !string.IsNullOrEmpty(rec.Value);
        }

        private void leDocumentNum_GotFocus(object sender, RoutedEventArgs e)
        {
            var lookup = sender as LookupEditor;
            lookup.PopupContentTemplate = System.Windows.Application.Current.Resources["LookUpUrlInvoiceClientPopupContent"] as ControlTemplate;
            lookup.ValueMember = "InvoiceNumber";
            lookup.SelectedIndexChanged += leDocumentNum_SelectedIndexChanged;
        }
    }
}
