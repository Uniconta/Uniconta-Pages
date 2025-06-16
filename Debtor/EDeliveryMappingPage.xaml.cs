using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
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

        public object PropertySource { get; set; }
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
        public override Type TableType => typeof(EDeliveryMappingClientExtended);
        public override IComparer GridSorting => new EDeliveryMappingSort();
        public override bool Readonly => false;
        public override bool IsAutoSave => false;
    }

    public partial class EDeliveryMappingPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EDeliveryMappingPage; } }

        private SQLCache xmlCache;
        private DebtorInvoiceClient invoice;

        public EDeliveryMappingPage(BaseAPI api) : base(api, string.Empty) => Init(null);
        public EDeliveryMappingPage(eDeliveryMappingGroupClient master) : base(null) => Init(master);

        private void Init(eDeliveryMappingGroupClient master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgEdeliveryMappingGrid;
            dgEdeliveryMappingGrid.api = api;
            SetRibbonControl(localMenu, dgEdeliveryMappingGrid);
            dgEdeliveryMappingGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            SetMappingGroups(master);
            SyncEntityMasterRowChanged((eDeliveryMappingGroupClient)leMappinggroup.SelectedItem);
            dgEdeliveryMappingGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgEdeliveryMappingGrid.SelectedItem as EDeliveryMappingClientExtended;
            switch (ActionType)
            {
                case "AddRow":
                    if (dgEdeliveryMappingGrid.masterRecord == null)
                    {
                        UnicontaMessageBox.Show(Localization.lookup("eDeliveryMappingMissingGroup"), Localization.lookup("EDeliveryMapping"));
                        return;
                    }
                    dgEdeliveryMappingGrid.AddRow();
                    xmlDocument = null;
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                    {
                        dgEdeliveryMappingGrid.DeleteRow();
                        gridRibbon_BaseActions("SaveGrid");
                        xmlDocument = null;
                    }
                    break;
                case "SaveGrid":
                    ValidateAndSave();
                    break;
                case "View":
                    ViewVoucher();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private async void ValidateAndSave()
        {
            var result = await GetValidatedeDeliveryMappingDoc();
            if (result != null && result.Length > 1)
                await saveGrid();
        }

        private byte[] xmlDocument = null;
        private async Task<byte[]> GetValidatedeDeliveryMappingDoc()
        {
            if (xmlDocument != null)
                return xmlDocument;

            if (dgEdeliveryMappingGrid.ItemsSource is List<EDeliveryMappingClientExtended> mappings &&
                mappings != null && mappings.Count > 0)
            {
                if (mappings.Any(m => m.TagId == 0))
                {
                    UnicontaMessageBox.Show(Localization.lookup(ErrorCodes.FieldCannotBeBlank.ToString() + ": Tag"),
                        Localization.lookup("Error"));
                    return null;
                }
                if (mappings.GroupBy(x => x.TagId).Any(g => g.Count() > 1))
                {
                    UnicontaMessageBox.Show(Localization.lookup("eDeliveryMappingDuplicateTags"), Localization.lookup("Error"));
                    return null;
                }

                var xmlTagsAndValues = mappings.ToDictionary(x => x.eDeliveryTag.Name, 
                    y =>
                    {
                        if (y.Value == null)
                            SetValue(y);

                        return y.Value;
                    });

                var result = await new NHRAPI(api).GetValidatedeDeliveryMappingDoc(invoice, xmlTagsAndValues);
                if (result != null && result.Length == 1)
                    UnicontaMessageBox.Show(Localization.lookup(((ErrorCodes)result[0]).ToString()), Localization.lookup("Error"));
                else if (result == null || result.Length == 0)
                    UnicontaMessageBox.Show(Localization.lookup(api.LastError.ToString()), Localization.lookup("Error"));

                xmlDocument = result;
            }

            return xmlDocument;
        }

        VoucherViewerWindow voucherViewer;
        protected async void ViewVoucher()
        {
            var header = string.Format(Localization.lookup("ViewOBJ"), "XML");
            if (voucherViewer == null || xmlDocument == null)
            {
                var xmlDocument = await GetValidatedeDeliveryMappingDoc();
                var voucher = new VouchersClient
                {
                    _Data = xmlDocument,
                    _Fileextension = FileextensionsTypes.XML
                };

                voucherViewer = new VoucherViewerWindow(invoice, this.api, header);
                voucherViewer._LoadInitMaster(invoice, voucher, 0, true);
                voucherViewer.Owner = UtilDisplay.GetCurentWindow();
                voucherViewer.Closed += delegate { voucherViewer = null; };
            }
            if (VoucherViewerWindow.lastHeight != 0)
            {
                voucherViewer.Width = VoucherViewerWindow.lastWidth;
                voucherViewer.Height = VoucherViewerWindow.lastHeight;
            }
            if (VoucherViewerWindow.isMaximized)
                voucherViewer.WindowState = WindowState.Maximized;

            voucherViewer.Show();
        }

        bool hasTableIdChanged;
        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as EDeliveryMappingClientExtended;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged += eDeliveryMappingClient_PropertyChanged;
            var selectedItem = e.NewItem as EDeliveryMappingClientExtended;
            if (selectedItem != null)
            {
                hasTableIdChanged = oldselectedItem == null || oldselectedItem.TableId != selectedItem.TableId;
                selectedItem.PropertyChanged += eDeliveryMappingClient_PropertyChanged;
            }
        }

        private void eDeliveryMappingClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var rec = sender as EDeliveryMappingClientExtended;
            switch (e.PropertyName)
            {
                case "TableId":
                    if (hasTableIdChanged)
                        SetProperty(rec, true);
                    break;
                case "Property":
                    SetValue(rec);
                    break;
            }
        }

        private async void SetMappingGroups(eDeliveryMappingGroupClient master)
        {
            var mapppingGrpCache = api.CompanyEntity.GetCache(typeof(eDeliveryMappingGroupClient)) ??
                await api.CompanyEntity.LoadCache(typeof(eDeliveryMappingGroupClient), api);

            if ((mapppingGrpCache == null || mapppingGrpCache.Count == 0) && master != null)
                if (mapppingGrpCache != null && mapppingGrpCache.Get(master.Name) == null)
                    mapppingGrpCache.Add(master);
                else
                    mapppingGrpCache = new SQLCache(new[] { master });

            if (mapppingGrpCache == null || mapppingGrpCache.Count == 0)
                return;

            leMappinggroup.ItemsSource = mapppingGrpCache;
            if (master != null)
                leMappinggroup.SelectedItem = master;
            else
                leMappinggroup.SelectedItem = mapppingGrpCache.GetNotNullArray
                    .FirstOrDefault(g => ((eDeliveryMappingGroupClient)g).IsDefault);
            if (leMappinggroup.SelectedItem == null)
                leMappinggroup.SelectedItem = mapppingGrpCache.First();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            if (args is eDeliveryMappingGroupClient master && master != null)
            {
                dgEdeliveryMappingGrid.UpdateMaster(master);
                InitQuery();
                SetXmlTags(master);
                cmbTableIds.ItemsSource = master?.GetTableAndProperties(api.CompanyEntity)?.OrderBy(x => x.DisplayName)?.ToList();
            }
        }

        private async void SetXmlTags(eDeliveryMappingGroupClient master)
        {
            if (xmlCache == null)
                xmlCache = api.CompanyEntity.GetCache(typeof(eDeliveryTagTypeClient)) ??
                    await api.CompanyEntity.LoadCache(typeof(eDeliveryTagTypeClient), api);

            cmbTagIds.ItemsSource = xmlCache?.GetNotNullArray?
                .Select(x => (eDeliveryTagTypeClient)x)?
                .Where(t => t._DocVersion == master._DocVersion && t._DocType == master._DocType)?
                .OrderBy(x => x.Name)?
                .ToList();
        }

        private void liMappinggroup_ButtonClicked(object sender) =>
            AddDockItem(TabControls.EDeliveryMappingGroupPage, (BaseAPI)api);

        private async void leMappinggroup_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var newMappingGroup = (sender as LookupEditor).SelectedItem as eDeliveryMappingGroupClient;
            if (newMappingGroup == null || dgEdeliveryMappingGrid.masterRecord == newMappingGroup)
                return;

            var docType = newMappingGroup._DocType;

            UnicontaBaseEntity[] result = null;
            if (docType == eDeliveryDocumentType.Invoice || docType == eDeliveryDocumentType.CreditNote)
            {
                var compareOp = docType == eDeliveryDocumentType.Invoice ?
                    CompareOperator.GreaterThan : CompareOperator.LessThan;

                result = (await api.Query<DebtorInvoiceClient>(
                    new PropValuePair[3]
                    {
                        PropValuePair.GenereteOrderByElement("Date", true),
                        PropValuePair.GenereteWhereElements("LineTotal", 0L, compareOp),
                        PropValuePair.GenereteTakeN(10),
                    }))?
                    .ToArray();
            }

            tbDocumentNum.Text = newMappingGroup.DocType;
            tbDocumentNum.Visibility = Visibility.Visible;

            leDocumentNum.ItemsSource = result;
            leDocumentNum.SelectedItem = invoice = (DebtorInvoiceClient)result?.FirstOrDefault();
            leDocumentNum.Visibility = Visibility.Visible;

            SyncEntityMasterRowChanged(newMappingGroup);
        }

        private void SetProperty(EDeliveryMappingClientExtended rec, bool resetProperty)
        {
            if (rec == null)
                return;

            if (resetProperty)
                rec.Property = null;

            rec.PropertySource = rec.eDeliveryMappingGroup?
                   .GetTableAndProperties(api.CompanyEntity)?
                   .Where(t => t.Id == rec.TableId)?
                   .SelectMany(t => t.Properties)?
                   .OrderBy(p => p.DisplayName)?
                   .ToList();

            rec.NotifyPropertyChanged("PropertySource");
        }

        private void SetValue(EDeliveryMappingClientExtended rec)
        {
            if (rec == null || invoice == null)
                return;

            var value = rec.GetTablePropertyValueFromEntity(invoice, (CompanyClient)api.CompanyEntity);
            rec.Value = !string.IsNullOrEmpty(value) ? value : "{" + Localization.lookup("Empty") + "}";
            Value.Visible = true;
        }

        private void leDocumentNum_GotFocus(object sender, RoutedEventArgs e)
        {
            var lookup = sender as LookupEditor;
            lookup.api = api;
            lookup.PopupContentTemplate = System.Windows.Application.Current.Resources["LookUpUrlInvoiceClientPopupContent"] as ControlTemplate;
            lookup.ValueMember = "InvoiceNumber";
            lookup.DisplayMember = "InvoiceNum";
            lookup.SelectedIndexChanged += leDocumentNum_SelectedIndexChanged;
        }

        private void leDocumentNum_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var master = (eDeliveryMappingGroupClient)leMappinggroup.SelectedItem;
            if (master == null)
                return;

            var uniEntity = leDocumentNum.SelectedItem as UnicontaBaseEntity;
            if (uniEntity is DebtorInvoiceClient inv &&
                ((master._DocType == eDeliveryDocumentType.Invoice && inv.TotalAmount > 0) ||
                (master._DocType == eDeliveryDocumentType.CreditNote && inv.TotalAmount < 0)))
            {
                invoice = leDocumentNum.SelectedItem as DebtorInvoiceClient;
                var rows = dgEdeliveryMappingGrid.ItemsSource as List<EDeliveryMappingClientExtended>;
                dgEdeliveryMappingGrid.ItemsSource = rows?
                    .Select(x =>
                    {
                        SetValue(x);
                        return x;
                    })
                    .ToList();
            }
        }

        private void PART_Editor_GotFocus(object sender, RoutedEventArgs e) =>
            SetProperty(dgEdeliveryMappingGrid.SelectedItem as EDeliveryMappingClientExtended, false);
    }
}
