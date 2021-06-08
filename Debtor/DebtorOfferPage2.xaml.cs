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
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using DevExpress.Xpf.Editors;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorOfferPage2 : FormBasePage
    {
        DebtorOfferClient editrow;
        SQLCache ProjectCache;
        DebtorClient Debtor;
        ContactClient Contact;
        CrmProspectClient Prospect;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }

        public override Type TableType { get { return typeof(DebtorOfferClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorOfferClient)value; } }

        bool lookupZipCode = true;
        public DebtorOfferPage2(UnicontaBaseEntity sourcedata, UnicontaBaseEntity master) /* called for edit from particular account */
            : base(sourcedata, true)
        {
            InitializeComponent();
            if (master != null)
            {
                Debtor = master as DebtorClient;
                if (Debtor == null)
                {
                    Prospect = master as CrmProspectClient;
                    if (Prospect == null)
                        Contact = master as ContactClient;
                }
            }
            InitPage(api, master);
        }

        public DebtorOfferPage2(CrudAPI crudApi, UnicontaBaseEntity master) /* called for add from particular account */
            : base(crudApi, "")
        {
            InitializeComponent();
            if (master != null)
            {
                Debtor = master as DebtorClient;
                if (Debtor == null)
                {
                    Prospect = master as CrmProspectClient;
                    if (Prospect == null)
                        Contact = master as ContactClient;
                }
            }
            InitPage(crudApi, master);
        }

        public DebtorOfferPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api, sourcedata);
        }
        public DebtorOfferPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi, null);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(leAccount, leAccount);
#endif
        }

        UnicontaBaseEntity master;

        void InitPage(CrudAPI crudapi, UnicontaBaseEntity master)
        {
            RemoveMenuItem();
            BusyIndicator = busyIndicator;
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            Employeelookupeditor.api = leAccount.api = lePayment.api = cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = leLayoutGroup.api = leInvoiceAccount.api = PriceListlookupeditior.api =
               leGroup.api = leShipment.api = leDeliveryTerm.api = Projectlookupeditor.api = PrCategorylookupeditor.api = leDeliveryAddress.api = leVat.api = prTasklookupeditor.api = crudapi;

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
                editrow = CreateNew() as DebtorOfferClient;
                editrow._Created = DateTime.MinValue;
                if (Debtor != null)
                {
                    editrow.SetMaster(Debtor);
                    if (editrow.RowId == 0)
                        SetValuesFromMaster(Debtor);
                    leAccount.IsEnabled = txtName.IsEnabled = false;
                }
                if (Contact != null)
                {
                    editrow.SetMaster(Contact);
                    editrow.ContactName = Contact.Name;
                    cmbContactName.IsEnabled = false;
                }
                editrow.SetMaster(master); // cound be prospect or project
            }
            else
            {
                BindContact(editrow.Debtor);
            }

            if (Prospect != null)
                BindContact(Prospect);
            if (Contact != null)
                BindContact(Contact.Prospect);

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            AcItem.ButtonClicked += AcItem_ButtonClicked;

#if !SILVERLIGHT
            editrow.PropertyChanged += Editrow_PropertyChanged;
#endif
            this.master = master;
            if (Prospect != null || Contact != null)
            {
                leAccount.IsEnabled = false;
                AcItem.IsEnabled = false;
                txtName.IsEnabled = false;
                txtName.Text = editrow.Name;
            }

            if (master == null)
            {
                if (crudapi.CompanyEntity.CRM)
                    LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.CrmProspect) });
                else
                    LoadType(typeof(Uniconta.DataModel.Debtor));
            }
            StartLoadCache();
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)frmRibbon.DataContext;
            var Comp = api.CompanyEntity;
            UtilDisplay.RemoveMenuCommand(rb, "PhysicalVou");
        }

        protected override void AfterTemplateSet(UnicontaBaseEntity row)
        {
            var editrow = (row as DebtorOfferClient);
            if (this.editrow != null)
            {
                editrow._OrderNumber = this.editrow._OrderNumber;
                editrow.RowId = this.editrow.RowId;
            }
            if (this.Debtor != null)
                editrow.SetMaster(this.Debtor);
        }

        protected override void OnLayoutCtrlLoaded()
        {
            AdjustLayout();
        }

        void AdjustLayout()
        {
            var Comp = api.CompanyEntity;
            if (Comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (!Comp.DeliveryAddress)
                dAddress.Visibility = Visibility.Collapsed;
            if (!Comp.Project)
                grpProject.Visibility = Visibility.Collapsed;
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

        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DeliveryZipCode")
            {
                if (lookupZipCode)
                {
                    var deliveryCountry = editrow.DeliveryCountry ?? editrow.Country;
                    var city = await UtilDisplay.GetCityAndAddress(editrow.DeliveryZipCode, deliveryCountry);
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

        public override void RowsPastedDone()
        {
            editrow.SetMaster(master);
            SetValuesFromMaster(Debtor);
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
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "SaveAndOrderLines":
                    saveFormAndOpenControl(TabControls.DebtorOfferLines);
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
            closePageOnSave = false;
            var res = await saveForm(false);
            closePageOnSave = true;
            if (res)
            {
                header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OfferLine"), editrow._OrderNumber, editrow.Name);
                AddDockItem(Control, ModifiedRow, header);
                dockCtrl?.JustClosePanel(this.ParentControl);
            }
        }
        private void AddDebtor_Click(object sender, RoutedEventArgs e)
        {
            object[] param = new object[2];
            param[0] = api;
            param[1] = null;
            AddDockItem(TabControls.DebtorAccountPage2, param, Uniconta.ClientTools.Localization.lookup("DebtorAccount"), "Add_16x16.png");
        }

        private void leAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            string id = Convert.ToString(e.NewValue);
            var debtors = api.CompanyEntity.GetCache(typeof(Debtor));
            if (debtors != null && Contact == null)
                SetValuesFromMaster((Debtor)debtors.Get(id));
        }

        async void SetValuesFromMaster(Debtor debtor)
        {
            if (debtor == null)
                return;
            var loadedOrder = LoadedRow as DCOrder;
            if (loadedOrder?._DCAccount == debtor._Account)
                return;
            if (this.Prospect == null) // no master
            {
                editrow.SetMaster(debtor);
                layoutItems.DataContext = null;
                layoutItems.DataContext = editrow;
            }
            else
            {
                editrow.Account = debtor._Account;
                editrow.SetCurrency(debtor._Currency);
            }
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



            TableField.SetUserFieldsFromRecord(debtor, editrow);
            if (ProjectCache != null)
            {
                Projectlookupeditor.cacheFilter = new DebtorProjectFilter(ProjectCache, debtor._Account);
                Projectlookupeditor.InvalidCache();
            }

            BindContact(debtor);
            if (installationCache != null)
            {
                leDeliveryAddress.cacheFilter = new AccountCacheFilter(installationCache, 1, debtor._Account);
                leDeliveryAddress.InvalidCache();
            }
            await api.Read(debtor);
            editrow.RefreshBalance();
        }

        void SetContactSource(SQLCache cache, Debtor debtor)
        {
            cmbContactName.ItemsSource = cache != null ? new ContactCacheFilter(cache, 1, debtor._Account) : null;
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
                    contact = cache?.Get(contactRefId);
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

        async void BindContact(CrmProspect prospect)
        {
            if (prospect == null)
            {
                cmbContactName.ItemsSource = new [] { Contact };
                cmbContactName.DisplayMember = "KeyName";
                cmbContactName.SelectedItem = Contact;
            }
            else
            {
                var cache = api.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Contact));
                if (cache == null || cache.Count == 0) return;

                cmbContactName.ItemsSource = new ContactCacheFilter(cache, 3, prospect.KeyStr);
                cmbContactName.DisplayMember = "KeyName";

                if (editrow._ContactRef != 0)
                {
                    var contact = cache.Get(editrow._ContactRef);
                    cmbContactName.SelectedItem = contact;
                    if (contact == null)
                    {
                        editrow._ContactRef = 0;
                        editrow.ContactName = null;
                    }
                }
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

        private void Projectlookupeditor_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (ProjectCache != null)
            {
                var selectedItem = Projectlookupeditor.SelectedItem as ProjectClient;
                setTask(selectedItem);
            }
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
            var selectedItem = Projectlookupeditor.SelectedItem as ProjectClient;
            setTask(selectedItem);
        }


#if !SILVERLIGHT
        private void LiDeliveryZipCode_OnButtonClicked(object sender)
        {
            var location = editrow._DeliveryAddress1 + "+" + editrow._DeliveryAddress2 + "+" + editrow._DeliveryAddress3 + "+" + editrow._DeliveryZipCode + "+" + editrow._DeliveryCity + "+" + editrow._DeliveryCountry;
            Utility.OpenGoogleMap(location);
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
        SQLCache installationCache;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            if (Comp.Project)
            {
                ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
                if (editrow._DCAccount != null)
                    Projectlookupeditor.cacheFilter = new DebtorProjectFilter(ProjectCache, editrow._DCAccount);

                var prCache = api.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? await api.LoadCache(typeof(Uniconta.DataModel.PrCategory)).ConfigureAwait(false);
                PrCategorylookupeditor.cacheFilter = new PrCategoryRevenueFilter(prCache);
            }

            if (Comp.DeliveryAddress)
            {
                installationCache = api.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) ?? await api.LoadCache(typeof(Uniconta.DataModel.WorkInstallation)).ConfigureAwait(false);
                if (editrow._DCAccount != null)
                    leDeliveryAddress.cacheFilter = new AccountCacheFilter(installationCache, 1, editrow._DCAccount);
            }
        }
    }
}
