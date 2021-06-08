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
using Uniconta.Common.Utility;

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
        ContactClient Contact;
        public DebtorOrdersPage2(UnicontaBaseEntity sourcedata, UnicontaBaseEntity master) /* called for edit from particular account */
            : base(sourcedata, true)
        {
            InitializeComponent();
            if (master != null)
            {
                Debtor = master as DebtorClient;
                if (Debtor == null)
                {
                    Contact = master as ContactClient;
                    if (Contact == null)
                    {
                        Project = master as ProjectClient;
                        Debtor = Project?.Debtor;
                    }
                    else
                        Debtor = Contact.Debtor;
                }
            }
            InitPage(api);
        }
        public DebtorOrdersPage2(CrudAPI crudApi, UnicontaBaseEntity master) /* called for add from particular account */
            : base(crudApi, "")
        {
            InitializeComponent();
            if (master != null)
            {
                Debtor = master as DebtorClient;
                if (Debtor == null)
                {
                    Contact = master as ContactClient;
                    if (Contact == null)
                    {
                        Project = master as ProjectClient;
                        Debtor = Project?.Debtor;
                    }
                    else
                        Debtor = Contact.Debtor;
                }
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
        bool lookupZipCode = true;
        void InitPage(CrudAPI crudapi)
        {
            RemoveMenuItem();
            BusyIndicator = busyIndicator;
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            PrCategorylookupeditor.api = Projectlookupeditor.api =
            Employeelookupeditor.api = leAccount.api = lePayment.api = cmbDim1.api = cmbDim2.api =
            cmbDim3.api = cmbDim4.api = cmbDim5.api = leTransType.api = leGroup.api = lePostingAccount.api
            = leShipment.api = leLayoutGroup.api = leDeliveryTerm.api = leInvoiceAccount.api = PriceListlookupeditior.api =
            leDeliveryAddress.api = leApprover.api = leSplit.api = leVat.api = prTasklookupeditor.api = crudapi;

#if SILVERLIGHT
            leRelatedOrder.api = crudapi;
#else
            leRelatedOrder.CrudApi = crudapi;
#endif
            cbDeliveryCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            AdjustLayout();
            if (editrow == null)
            {
                frmRibbon.DisableButtons("Delete");
                liCreatedTime.Visibility = Visibility.Collapsed;
                editrow = CreateNew() as DebtorOrderClient;
                editrow._Created = DateTime.MinValue;
                if (Debtor != null)
                {
                    SetFieldFromDebtor(editrow, Debtor);
                    leAccount.IsEnabled = txtName.IsEnabled = false;
                }
                if (Contact != null)
                {
                    editrow.SetMaster(Contact);
                    leAccount.IsEnabled = txtName.IsEnabled = cmbContactName.IsEnabled = false;
                }
                else if (Project != null)
                {
                    editrow.SetMaster(Project);
                    leAccount.IsEnabled = txtName.IsEnabled = false;
                    Projectlookupeditor.IsEnabled = false;
                    SetPrCategory(editrow);
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
            editrow.PropertyChanged += Editrow_PropertyChanged;
            AcItem.ToolTip = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("Debtor"));
#endif
            StartLoadCache();
        }
        protected override void AfterTemplateSet(UnicontaBaseEntity row)
        {
            var editrow = row as DebtorOrderClient;
            if (this.editrow != null)
            {
                editrow._OrderNumber = this.editrow._OrderNumber;
                editrow.RowId = this.editrow.RowId;
            }
            if (Debtor != null)
                SetFieldFromDebtor(editrow, Debtor);
            if (Project != null)
            {
                editrow.SetMaster(Project);
                SetPrCategory(editrow);
            }
        }

        protected override void OnLayoutCtrlLoaded()
        {
            AdjustLayout();
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)frmRibbon.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "PhysicalVou");
        }

        void AdjustLayout()
        {
            var Comp = api.CompanyEntity;
            if (!Comp.Project)
                grpProject.Visibility = Visibility.Collapsed;
            if (!Comp.Shipments)
                shipmentItem.Visibility = Visibility.Collapsed;
            if (Comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (!Comp.DeliveryAddress)
                dAddress.Visibility = Visibility.Collapsed;
            if (!Comp.Contacts)
                cmbContactName.Visibility = Visibility.Collapsed;
            if (!Comp.ApproveSalesOrders)
                grpApproval.Visibility = Visibility.Collapsed;
            if (!Comp.SetupSizes)
                grpSize.Visibility = Visibility.Collapsed;

            if (!Comp.ProjectTask)
                projectTask.Visibility = Visibility.Collapsed;
            else if (editrow?._Project != null)
            {
                var project = Comp.GetCache(typeof(Uniconta.DataModel.Project))?.Get(editrow._Project) as ProjectClient;
                setTask(project);
            }
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
            if (Contact != null)
                editrow.SetMaster(Contact);
            else if (Project != null)
                editrow.SetMaster(Project);
            SetFieldFromDebtor(editrow, Debtor);
        }

        void AcItem_ButtonClicked(object sender)
        {
            AddDebtor_Click(null, null);
        }

        bool setVoucher;
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            object[] args;
            if (screenName == TabControls.DebtorAccountPage2)
            {
                args = argument as object[];
                if (args[2] == this.ParentControl)
                {
                    leAccount.LoadItemSource();
                    editrow.SetMaster(args[3] as UnicontaBaseEntity);
                }
            }

            if (screenName == TabControls.AttachVoucherGridPage && argument != null && setVoucher)
            {
                setVoucher = false;
                args = argument as object[];
                var voucher = args[0] as VouchersClient;
                if (voucher != null)
                    editrow.DocumentRef = voucher.RowId;
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
                    setVoucher = true;
                    AddDockItem(TabControls.AttachVoucherGridPage, new object[] { _refferedVouchers }, true);
                    break;
                case "ViewVoucher":
                    busyIndicator.IsBusy = true;
                    ViewVoucher(TabControls.VouchersPage3, editrow);
                    busyIndicator.IsBusy = false;
                    break;

                case "ImportVoucher":
                    Utility.ImportVoucher(editrow, api, new VouchersClient() { _Content = ContentTypes.Invoice });
                    break;

                case "RemoveVoucher":
                    RemoveVoucher(editrow);
                    break;
                case "Save":
                    if (Utility.IsExecuteWithBlockedAccount(editrow.Debtor))
                        frmRibbon_BaseActions(ActionType);
                    break;
                case "Delete":
                    if (editrow._OrderTotal != 0)
                    {
                        var msg = Uniconta.ClientTools.Localization.lookup("NumberOfLines") + ": " + NumberConvert.ToString(editrow._Lines);
                        var msg2 = msg + "\r\n" + string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), editrow._OrderNumber);
                        CWConfirmationBox dialog = new CWConfirmationBox(msg2, Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
                        dialog.Closing += delegate
                        {
                            if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                                frmRibbon_BaseActions(ActionType);
                        };
                        dialog.Show();
                    }
                    else
                        frmRibbon_BaseActions(ActionType);
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override async void saveFormAndOpenControl(string Control, string header = null)
        {
            if (!Utility.IsExecuteWithBlockedAccount(editrow.Debtor))
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
            AddDockItem(TabControls.DebtorAccountPage2, new object[] { api, null }, Uniconta.ClientTools.Localization.lookup("DebtorAccount"), "Add_16x16.png");
        }

        private void leAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            string debAcc = Convert.ToString(e.OldValue);
            string id = Convert.ToString(e.NewValue);
            if (id != editrow._DCAccount ||
                editrow.RowId == 0 ||
                (!string.IsNullOrEmpty(debAcc) && debAcc != id) ||
                ((LoadedRow as DCOrder)?._DCAccount != id))
            {
                if (leAccount.IsEnabled)
                {
                    editrow.Installation = null;
                    SetFieldFromDebtor(editrow, (Uniconta.DataModel.Debtor)api.GetCache(typeof(Uniconta.DataModel.Debtor))?.Get(id));
                }
            }
        }

        private void Projectlookupeditor_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (Project != null)
                setTask(Projectlookupeditor.SelectedItem as ProjectClient);
        }

        async private void setTask(ProjectClient master)
        {
            if (api.CompanyEntity.ProjectTask)
            {
                if (master != null)
                    editrow.taskSource = master.Tasks ?? await master.LoadTasks(api);
                else
                    editrow.taskSource = api.GetCache(typeof(Uniconta.DataModel.ProjectTask));
                editrow.NotifyPropertyChanged("TaskSource");
                prTasklookupeditor.ItemsSource = editrow.TaskSource;
            }
        }

        private void prTasklookupeditor_GotFocus(object sender, RoutedEventArgs e)
        {
            setTask(Projectlookupeditor.SelectedItem as ProjectClient);
        }

        async void SetFieldFromDebtor(DebtorOrderClient editrow, Debtor debtor)
        {
            if (debtor == null)
                return;
            var loadedOrder = LoadedRow as DCOrder;
            editrow.SetMaster(debtor);
            if (this.Project == null) // no project master
                editrow.PricesInclVat = debtor._PricesInclVat;
            if (!RecordLoadedFromTemplate || debtor._DeliveryAddress1 != null)
            {
                editrow.DeliveryName = debtor._DeliveryName;
                editrow.DeliveryAddress1 = debtor._DeliveryAddress1;
                editrow.DeliveryAddress2 = debtor._DeliveryAddress2;
                editrow.DeliveryAddress3 = debtor._DeliveryAddress3;
                editrow.DeliveryCity = debtor._DeliveryCity;
                if (editrow.DeliveryZipCode != debtor._DeliveryZipCode)
                {
                    lookupZipCode = false;
                    editrow.DeliveryZipCode = debtor._DeliveryZipCode;
                }
                if (debtor._DeliveryCountry != 0)
                    editrow.DeliveryCountry = debtor._DeliveryCountry;
                else
                    editrow.DeliveryCountry = null;
            }
            if (ProjectCache != null)
                Projectlookupeditor.cache = ProjectCache;

            TableField.SetUserFieldsFromRecord(debtor, editrow);
            BindContact(debtor);
            if (installationCache != null)
            {
                leDeliveryAddress.cacheFilter = new AccountCacheFilter(installationCache, 1, debtor._Account);
                leDeliveryAddress.InvalidCache();
            }
            layoutItems.DataContext = null;
            layoutItems.DataContext = editrow;

            await api.Read(debtor);
            editrow.RefreshBalance();
        }

        async void SetPrCategory(DebtorOrderClient editrow)
        {
            var cats = api.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? await api.LoadCache(typeof(Uniconta.DataModel.PrCategory));
            foreach (var rec in (Uniconta.DataModel.PrCategory[])cats.GetNotNullArray)
            {
                if (rec._CatType == CategoryType.OnAccountInvoicing)
                {
                    editrow.PrCategory = rec._Number;
                    if (rec._Default)
                        break;
                }
            }
        }

        void SetContactSource(SQLCache cache, Debtor debtor)
        {
            if (cache != null && cache.Count > 0)
                cmbContactName.ItemsSource = new ContactCacheFilter(cache, 1, debtor._Account);
        }

        async void BindContact(Debtor debtor)
        {
            if (debtor == null) return;

            var cache = api.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Contact));
            SetContactSource(cache, debtor);
            cmbContactName.DisplayMember = "KeyName";

            if (editrow == null) return;

            var contactRefId = editrow._ContactRef;
            if (contactRefId != 0)
            {
                var contact = cache.Get(contactRefId);
                if (contact == null)
                {
                    cache = api.LoadCache(typeof(Uniconta.DataModel.Contact), true).GetAwaiter().GetResult();
                    contact = cache.Get(contactRefId);
                    SetContactSource(cache, debtor);
                }
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
                await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);

            if (Comp.Project)
            {
                ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
                Projectlookupeditor.cache = ProjectCache;

                var prCache = api.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? await api.LoadCache(typeof(Uniconta.DataModel.PrCategory)).ConfigureAwait(false);
                PrCategorylookupeditor.cacheFilter = new PrCategoryRevenueFilter(prCache);
            }
            if (Comp.DeliveryAddress)
            {
                installationCache = Comp.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) ?? await api.LoadCache(typeof(Uniconta.DataModel.WorkInstallation)).ConfigureAwait(false);
                if (editrow._DCAccount != null)
                    leDeliveryAddress.cacheFilter = new AccountCacheFilter(installationCache, 1, editrow._DCAccount);
            }
        }

        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DeliveryZipCode")
            {
                if (lookupZipCode)
                {
                    var deliveryCountry = editrow.DeliveryCountry ?? editrow.Country;
                    var city = await UtilDisplay.GetCityAndAddress(editrow._DeliveryZipCode, deliveryCountry);
                    if (city != null)
                    {
                        editrow.DeliveryCity = city[0];
                        var add1 = city[1];
                        if (!string.IsNullOrEmpty(add1))
                            editrow.DeliveryAddress1 = add1;
                        var zip = city[2];
                        if (!string.IsNullOrEmpty(zip) && editrow.DeliveryZipCode != zip)
                        {
                            lookupZipCode = false;
                            editrow.DeliveryZipCode = zip;
                        }
                    }
                }
                else
                    lookupZipCode = true;
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
            var location = editrow._DeliveryAddress1 + "+" + editrow._DeliveryAddress2 + "+" + editrow._DeliveryAddress3 + "+" + editrow._DeliveryZipCode + "+" + editrow._DeliveryCity + "+" + editrow.DeliveryCountry;
            Utility.OpenGoogleMap(location);
        }

        private void liName_OnLookupButtonClicked(object sender)
        {
            var lookupEditor = sender as LookupEditor;
            lookupEditor.ItemsSource = api.GetCache(typeof(Uniconta.DataModel.Debtor));
            lookupEditor.SelectedIndexChanged += LookupEditor_SelectedIndexChanged;
        }

        private void LookupEditor_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var lookup = sender as LookupEditor;
            var selectedDebt = lookup.SelectedItem as DebtorClient;

            if (selectedDebt != null)
                CopyAddressToRow(selectedDebt._Name, selectedDebt._Address1, selectedDebt._Address2, selectedDebt._Address3, selectedDebt._ZipCode, selectedDebt._City, selectedDebt._Country);
            lookup.SelectedIndexChanged -= LookupEditor_SelectedIndexChanged;
        }

        private void lblDeliveryAddress_ButtonClicked(object sender)
        {
            var selectedInstallation = leDeliveryAddress.SelectedItem as WorkInstallationClient;
            if (selectedInstallation != null)
            {
                CopyAddressToRow(selectedInstallation._Name, selectedInstallation._Address1, selectedInstallation._Address2, selectedInstallation._Address3, selectedInstallation._ZipCode, selectedInstallation._City, selectedInstallation._Country);
                if (selectedInstallation._DeliveryTerm != null)
                    editrow.DeliveryTerm = selectedInstallation._DeliveryTerm;
            }
        }

        private void CopyAddressToRow(string name, string address1, string address2, string address3, string zipCode, string city, CountryCode? country)
        {
            editrow.DeliveryName = name;
            editrow.DeliveryAddress1 = address1;
            editrow.DeliveryAddress2 = address2;
            editrow.DeliveryAddress3 = address3;
            editrow.DeliveryCity = city;
            if (editrow.DeliveryZipCode != zipCode)
            {
                lookupZipCode = false;
                editrow.DeliveryZipCode = zipCode;
            }
            editrow.DeliveryCountry = country;
        }
#endif
    }
}
