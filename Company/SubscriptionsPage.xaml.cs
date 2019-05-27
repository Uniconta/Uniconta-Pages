using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Pages.Maintenance;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading.Tasks;
using UnicontaClient.Controls.Dialogs;
using DevExpress.Xpf.Editors;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class SubscriptionsPage : FormBasePage
    {
        SubscriptionClient editrow;

        public override Type TableType { get { return typeof(SubscriptionClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (SubscriptionClient)value; } }
        public SubscriptionsPage(UnicontaBaseEntity Sourcedata)
            : base(Sourcedata, true)
        {
            InitializeComponent();
            InitPage(session.Uid);
        }

        public SubscriptionsPage(CrudAPI crudApi, string UidStr)
            : base(crudApi, UidStr)
        {
            int uid = 0;
            if (!string.IsNullOrEmpty(UidStr))
                int.TryParse(UidStr, out uid);

            InitializeComponent();
            InitPage(uid);
            //FocusManager.SetFocusedElement(txtCompanyRegNo, txtCompanyRegNo);
        }

        void InitPage(int Uid)
        {
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow =CreateNew() as SubscriptionClient;
                editrow._OwnerUid = Uid;
                editrow._Country = session.User._Nationality;
                editrow.Name = api.CompanyEntity.Name;
                editrow.Address1 = api.CompanyEntity._Address1;
                editrow.Address2 = api.CompanyEntity._Address2;
                editrow.Currency = AppEnums.Currencies.ToString((int)api.CompanyEntity._CurrencyId);
                editrow.LegalIdent = api.CompanyEntity._Id;
                editrow.Phone = api.CompanyEntity._Phone;

            }
            else
                txtOwnerUid.IsReadOnly = true;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            cmbCurrency.ItemsSource = Utility.GetCurrencyEnum();
#if SILVERLIGHT
            var countries = Utility.GetEnumItemsWithPascalToSpace(typeof(CountryCode));
            cmbCountry.ItemsSource = countries;
#endif
            OwnerUidItem.ButtonClicked += OwnerUidItem_ButtonClicked;

            txtZipCode.EditValueChanged += TextEditor_EditValueChanged;

            if (api.session.User._Role < (byte)Uniconta.Common.User.UserRoles.Reseller)
                chkBlocked.IsReadOnly = true;
        }

        private void OwnerUidItem_ButtonClicked(object sender)
        {
            CWUsers usersDialog = new CWUsers(api);
            usersDialog.Closing += delegate
            {
                if (usersDialog.DialogResult == true)
                {
                    var owner = usersDialog.selectedUser;
                    if (owner != null)
                        editrow.OwnerUid = owner.Uid;
                }
            };
            usersDialog.Show();
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Company")
            {
                // ERIK  Todo. insert link here
                // AddDockItem(TabControls.SubscriptionCompanyPage, editrow, Uniconta.ClientTools.Localization.lookup("Companies"));
            }
            else if (ActionType == "Invoice")
                AddDockItem(TabControls.SubscriptionInvoicePage, editrow);
            else
                frmRibbon_BaseActions(ActionType);
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }

        public override void OnClosePage(object[] refreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, refreshParams);
        }

        public override string NameOfControl
        {
            get { return TabControls.SubscriptionsPage; }
        }

        private async void TextEditor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded)
            {
                var city = await UtilDisplay.GetCityName(s.Text,editrow.Country);
                if (city != null)
                    editrow.City = city;
            }
        }

        private bool onlyRunOnce;

        private async void TxtCompanyRegNo_OnEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded)
            {
                var cvr = s.Text;
                if (cvr == null || cvr.Length < 5)
                    return;

                var allIsLetter = cvr?.All(x => char.IsLetter(x));

                if (allIsLetter.HasValue && allIsLetter.Value == true)
                    return;

                CompanyInfo ci = null;
                try
                {
#if !SILVERLIGHT
                    ci = await CVR.CheckCountry(cvr, editrow._Country);
#else
                    var lookupApi = new Uniconta.API.System.UtilityAPI(api);
                    ci = await lookupApi.LookupCVR(cvr, editrow._Country);
#endif
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK);
                    return;
                }

                if (!onlyRunOnce)
                {
                    if (ci == null)
                        return;

                    if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                    {
                        var address = ci.address;
                        if (address != null)
                        {
                            var streetAddress = address.CompleteStreet;
                            if (ci.life.name == editrow._Name && streetAddress == editrow._Address1 && address.zipcode == editrow._ZipCode)
                                return; // we wil not override since address has not changed

                            onlyRunOnce = true;
                            if (editrow._Address1 == null)
                            {
                                editrow.Address1 = address.CompleteStreet;
                                editrow.ZipCode = address.zipcode;
                                editrow.City = address.cityname;
                                editrow.Country = address.Country;
                            }
                            else
                            {
                                var result = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateAddress"), Uniconta.ClientTools.Localization.lookup("Information"), UnicontaMessageBox.YesNo);
                                if (result != UnicontaMessageBox.Yes)
                                    return;
                                {
                                    editrow.Address1 = address.CompleteStreet;
                                    editrow.ZipCode = address.zipcode;
                                    editrow.City = address.cityname;
                                    editrow.Country = address.Country;
                                }
                            }
                        }

                        if (string.IsNullOrWhiteSpace(editrow._Name))
                            editrow.Name = ci.life.name;

                        var contact = ci.contact;
                        if (contact != null)
                        {
                            editrow.Phone = contact.phone;
                            editrow.ContactEmail = contact.email;
                        }
                    }
                }
                else
                    onlyRunOnce = false;
            }
        }

#if !SILVERLIGHT
        private void Email_ButtonClicked(object sender)
        {
            var txtEmail = ((CorasauLayoutItem)sender).Content as TextEditor;
            if (txtEmail == null)
                return;
            var mail = string.Concat("mailto:", txtEmail.Text);
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = mail;
            proc.Start();
        }

        private void CorasauLayoutItem_OnButtonClicked(object sender)
        {
            var location = editrow.Address1 + "+" + editrow.Address2 + "+" + editrow.ZipCode + "+" + editrow.City + "+" + editrow.Country;
            Utility.OpenGoogleMap(location);
        }
#endif

    }
}
