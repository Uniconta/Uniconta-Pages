using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.ClientTools.Util;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Controls;
using DevExpress.Xpf.Editors;
using System.ComponentModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorOrdersPage2 : FormBasePage
    {
        DebtorOrderClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            int length = RefreshParams.Length;
            Array.Resize(ref RefreshParams, length + 1);
            RefreshParams[length] = editrow;
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.DebtorOrdersPage2.ToString(); } }

        public override Type TableType { get { return typeof(DebtorOrderClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorOrderClient)value; } }
        DebtorClient Debtor;
        SQLCache ProjectCache;
        ProjectClient Project;
        public DebtorOrdersPage2(UnicontaBaseEntity sourcedata, UnicontaBaseEntity master) /* called for edit from particular account */
            : base(sourcedata, true)
        {
            InitializeComponent();
            if (master != null)
            {
                if (master is DebtorClient)
                    Debtor = master as DebtorClient;
                else if (master is ProjectClient)
                    Project = master as ProjectClient;
            }
            InitPage(api);
        }
        public DebtorOrdersPage2(CrudAPI crudApi, UnicontaBaseEntity master) /* called for add from particular account */
            : base(crudApi, "")
        {
            InitializeComponent();
            if (master != null)
            {
                if (master is DebtorClient)
                    Debtor = master as DebtorClient;
                else if (master is ProjectClient)
                    Project = master as ProjectClient;
            }
            InitPage(api);
        }
        public DebtorOrdersPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public DebtorOrdersPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(leAccount, leAccount);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            RemoveMenuItem();
            BusyIndicator = busyIndicator;
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            PrCategorylookupeditor.api = Projectlookupeditor.api =
            Employeelookupeditor.api = leAccount.api = lePayment.api = cmbDim1.api = cmbDim2.api =
            cmbDim3.api = cmbDim4.api = cmbDim5.api = leTransType.api = leGroup.api = lePostingAccount.api
            = leShipment.api = leLayoutGroup.api = leDeliveryTerm.api = leInvoiceAccount.api= PriceListlookupeditior.api=
            leRelatedOrder.api= leDeliveryAddress.api = leApprover.api= crudapi;
            cbDeliveryCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            AdjustLayout();
            if (editrow == null)
            {
                frmRibbon.DisableButtons("Delete");
                liCreatedTime.Visibility = Visibility.Collapsed;
                editrow = CreateNew() as DebtorOrderClient;
                if (Debtor != null)
                {
                    editrow.SetMaster(Debtor);
                    if (editrow.RowId == 0)
                        SetFieldFromDebtor(Debtor);
                    leAccount.IsEnabled = txtName.IsEnabled = false;
                }
                else if (Project != null)
                {
                    editrow.SetMaster(Project);
                    leAccount.IsEnabled = txtName.IsEnabled = false;
                    Projectlookupeditor.IsEnabled = false;
                }
            }
            else
            {
                BindContact(editrow.Debtor);
            }

            if (editrow.LastInvoice == DateTime.MinValue)
                txtLastInvoice.Text = string.Empty;
            else
                txtLastInvoice.Text = editrow.LastInvoice.ToString("d");

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            
            AcItem.ButtonClicked += AcItem_ButtonClicked;
#if !SILVERLIGHT
            txtDelZipCode.EditValueChanged += TxtDelZipCode_EditValueChanged;
            AcItem.ToolTip = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("Debtor"));
#endif
            StartLoadCache();

            
        }
        protected override void OnLayoutCtrlLoaded()
        {
             AdjustLayout();
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)frmRibbon.DataContext;
            var Comp = api.CompanyEntity;
            UtilDisplay.RemoveMenuCommand(rb, "PhysicalVou");
        }

        void AdjustLayout()
        {
            var Comp = api.CompanyEntity;
            if (!Comp.Project)
            {
                grpProject.Visibility = Visibility.Collapsed;
            }
            if (!Comp.Shipments)
                shipmentItem.Visibility = Visibility.Collapsed;
            if (Comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (!Comp.DeliveryAddress)
                dAddress.Visibility = Visibility.Collapsed;
        }

        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup)
        {
            parentGroup = lastGroup;
            return true;
        }

        public override void RowsPastedDone()
        {
            if (Debtor != null)
                editrow.SetMaster(Debtor);
            else if (Project != null)
                editrow.SetMaster(Project);
            SetFieldFromDebtor(Debtor);
        }

        void AcItem_ButtonClicked(object sender)
        {
            AddDebtor_Click(null, null);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorAccountPage2)
            {
                var args = argument as object[];
                if (args[2] == this.ParentControl)
                {
                    leAccount.LoadItemSource();
                    var dc = args[3] as UnicontaBaseEntity;
                    editrow.SetMaster(dc);
                }
            }

            if(screenName == TabControls.AttachVoucherGridPage && argument !=null)
            {
                var voucherObj = argument as object[];

                if(voucherObj[0] is VouchersClient)
                {
                    var voucher = voucherObj[0] as VouchersClient;
                    editrow.DocumentRef = voucher.RowId;
                }
            }
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "SaveAndOrderLines":
                    saveFormAndOpenControl(TabControls.DebtorOrderLines);
                    break;
                case "RefVoucher":
                    var _refferedVouchers = new List<int>();
                    if (editrow._DocumentRef != 0)
                        _refferedVouchers.Add(editrow._DocumentRef);

                    AddDockItem(TabControls.AttachVoucherGridPage, new object[] { _refferedVouchers }, true);
                    break;
                case "ViewVoucher":
                    busyIndicator.IsBusy = true;
                    ViewVoucher(TabControls.VouchersPage3, editrow);
                    busyIndicator.IsBusy = false;
                    break;

                case "ImportVoucher":
                    var voucher = new VouchersClient();
                    voucher._Content = ContentTypes.Invoice;
                    Utility.ImportVoucher(editrow, api, voucher);
                    break;

                case "RemoveVoucher":
                    RemoveVoucher(editrow);
                    break;
                case "Save":
                    if (!Utility.IsExecuteWithBlockedAccount(editrow.Debtor)) return;
                    frmRibbon_BaseActions(ActionType);
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override async void saveFormAndOpenControl(string Control, string header = null)
        {
            if(!Utility.IsExecuteWithBlockedAccount(editrow.Debtor))
                return;

            closePageOnSave = false;
            var res = await saveForm(false);
            closePageOnSave = true;
            if (res)
            {
                header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), editrow._OrderNumber, editrow.Name);
                AddDockItem(Control, ModifiedRow, header);
                dockCtrl?.JustClosePanel(this.ParentControl);
            }
        }

        private void AddDebtor_Click(object sender, RoutedEventArgs e)
        {
            object[] param = new object[2];
            param[0] = api;
            param[1] = null;
            AddDockItem(TabControls.DebtorAccountPage2, param, Uniconta.ClientTools.Localization.lookup("DebtorAccount"), ";component/Assets/img/Add_16x16.png");
        }

        private void leAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            string id = Convert.ToString(e.NewValue);
            var debtors = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Debtor));
            SetFieldFromDebtor((Uniconta.DataModel.Debtor)debtors?.Get(id));
        }

        async void SetFieldFromDebtor(Debtor debtor)
        {
            if (debtor == null)
                return;
            var loadedOrder = LoadedRow as DCOrder;
            if (loadedOrder?._DCAccount == debtor._Account)
                return;
            editrow.Account = debtor._Account;
            editrow.SetCurrency(debtor._Currency);
            editrow.Payment = debtor._Payment;
            editrow.EndDiscountPct = debtor._EndDiscountPct;
            editrow.PricesInclVat = debtor._PricesInclVat;
            editrow.PostingAccount = debtor._PostingAccount;
            editrow.Shipment = debtor._Shipment;
            editrow.LayoutGroup = debtor._LayoutGroup;
            editrow.Employee = debtor._Employee;
            editrow.DeliveryTerm = debtor._DeliveryTerm;
            if (!RecordLoadedFromTemplate || debtor._DeliveryAddress1 != null)
            {
                editrow.DeliveryName = debtor._DeliveryName;
                editrow.DeliveryAddress1 = debtor._DeliveryAddress1;
                editrow.DeliveryAddress2 = debtor._DeliveryAddress2;
                editrow.DeliveryAddress3 = debtor._DeliveryAddress3;
                editrow.DeliveryZipCode = debtor._DeliveryZipCode;
                editrow.DeliveryCity = debtor._DeliveryCity;
                if (debtor._DeliveryCountry != 0)
                    editrow.DeliveryCountry = debtor._DeliveryCountry;
                else
                    editrow.DeliveryCountry = null;
            }
            if (ProjectCache != null)
            {
                Projectlookupeditor.cacheFilter = new DebtorProjectFilter(ProjectCache, debtor._Account);
                Projectlookupeditor.InvalidCache();
            }
            TableField.SetUserFieldsFromRecord(debtor, editrow);
            BindContact(debtor);
            if(installationCache!= null)
            {
                leDeliveryAddress.cacheFilter = new AccountCacheFilter(installationCache, 1, debtor._Account);
                leDeliveryAddress.InvalidCache();
            }
            await api.Read(debtor);
            editrow.RefreshBalance();
        }

        int contactRefId;
        async void BindContact(Debtor debtor)
        {
            if (debtor == null)
                return;

            contactRefId = editrow._ContactRef;
            var Comp = api.CompanyEntity;
            var cache = Comp.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Contact), api);
            var items = ((IEnumerable<Uniconta.DataModel.Contact>)cache?.GetNotNullArray)?.Where(x => x._DCType == 1 && x._DCAccount == debtor._Account);
            cmbContactName.ItemsSource = items;
            cmbContactName.DisplayMember = "KeyName";

            if (contactRefId != 0 && items != null)
            {
                var contact = items.Where(x => x.RowId == contactRefId).FirstOrDefault();
                cmbContactName.SelectedItem = contact;
                if (contact == null)
                {
                    editrow._ContactRef = 0;
                    editrow.ContactName = null;
                }
            }
        }

        SQLCache installationCache;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            if (Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) == null)
                await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api).ConfigureAwait(false);

            if (Comp.Project)
            {
                ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Project), api).ConfigureAwait(false);
                if (editrow._DCAccount != null)
                    Projectlookupeditor.cacheFilter = new DebtorProjectFilter(ProjectCache, editrow._DCAccount);

                var prCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.PrCategory), api).ConfigureAwait(false);
                PrCategorylookupeditor.cacheFilter = new PrCategoryRevenueFilter(prCache);
            }
            if (Comp.DeliveryAddress)
            {
                installationCache = Comp.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.WorkInstallation), api).ConfigureAwait(false);
                if (editrow._DCAccount != null)
                    leDeliveryAddress.cacheFilter = new AccountCacheFilter(installationCache, 1, editrow._DCAccount);
            }
        }

        private async void TxtDelZipCode_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded)
            {
                var deliveryCountry = editrow.DeliveryCountry ?? editrow.Country;
                var city = await UtilDisplay.GetCityName(s.Text, deliveryCountry);
                if (city != null)
                    editrow.DeliveryCity = city;
            }
        }

        private void cmbContactName_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            var selectedItem = cmbContactName.SelectedItem as Contact;
            if (selectedItem != null)
            {
                editrow._ContactRef = selectedItem.RowId;
                editrow.ContactName = selectedItem._Name;
            }
            else
            {
                editrow._ContactRef = 0;
                editrow.ContactName = null;
            }
        }

#if !SILVERLIGHT
        private void LiDeliveryZipCode_OnButtonClicked(object sender)
        {
            var location = editrow.DeliveryAddress1 + "+" + editrow.DeliveryAddress2 + "+" + editrow.DeliveryAddress3 + "+" + editrow.DeliveryZipCode + "+" + editrow.DeliveryCity + "+" + editrow.DeliveryCountry;
            Utility.OpenGoogleMap(location);
        }
#endif
    }
}
