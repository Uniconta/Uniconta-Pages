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
using DevExpress.Xpf.Editors;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;
using UnicontaClient.Pages;
using Uniconta.Common.User;
using UnicontaClient.Controls.Dialogs;
using System.Windows.Markup.Localizer;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class EditCompany : FormBasePage
    {
        CompanyClient editrow;

        public EditCompany(UnicontaBaseEntity sourceData)
            : base(sourceData, true)
        {
            InitializeComponent();
            cmbCurrency.ItemsSource = cmbGroupCurrency.ItemsSource = AppEnums.Currencies.GetLabels();
            cmbCompanyType.ItemsSource = Enum.GetValues(typeof(Uniconta.DataModel.CompanyTypeType));
            layoutControl = layoutItems;
            var Comp = api.CompanyEntity;
            Withholdinglookupeditior.api = api;
            if (!Comp._HasWithholding)
            {
                itemWithholding.Visibility = Visibility.Collapsed;
                Withholdinglookupeditior.Visibility = Visibility.Collapsed;
            }
            ribbonControl = frmRibbon;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            txtCulture.Text = string.Concat("(", Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName, ")");
            this.SaveComplete += EditCompany_SaveComplete;
            leIndustryCode.api = api;
            if (editrow.Deactive)
            {
                ribbonControl.EnableButtons("Activate");
                ribbonControl.DisableButtons("Deactivate");
            }
            else
            {
                ribbonControl.DisableButtons("Activate");
                ribbonControl.EnableButtons("Deactivate");
            }
            GetAccountant();
            accountantItem.ButtonClicked += AcItem_ButtonClicked;
            accountant2Item.ButtonClicked += Ac2Item_ButtonClicked;
            accountant2Item.Label = $"{Uniconta.ClientTools.Localization.lookup("Accountant")} 2";
            txtCompanyRegNo.EditValueChanged += TxtCVR_EditValueChanged;
            var country = Comp._CountryId;

            if (country != CountryCode.Denmark &&
                country != CountryCode.Greenland &&
                country != CountryCode.FaroeIslands &&
                country != CountryCode.Norway)
                liPymtCodeOpt.Visibility = Visibility.Collapsed;

            RemoveMenuItem();

            cmbSendAppRemdr.ItemsSource = AppEnums.Weekdays.Values.ToList();
            SetOwnerName();
        }

        async void SetOwnerName()
        {
            liOwnerName.Label = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Owner"), Uniconta.ClientTools.Localization.lookup("Name"));
            liReseller.Label = Uniconta.ClientTools.Localization.lookup("Reseller");
            var owners = await api.Query<UserClient>(editrow);
            var owner = owners?.FirstOrDefault(x => x.Uid == editrow._OwnerUid);
            if (owner != null)
            {
                txtOwner.Text = owner._Name;
                txtReseller.Text = owner.ResellerName;
            }
        }

        private bool onlyRunOnce;

        private async void TxtCVR_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
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
                    ci = await CVR.CheckCountry(cvr, editrow.Country);
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                    return;
                }

                if (!onlyRunOnce)
                {
                    if (ci == null)
                        return;

                    if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                    {
                        editrow.IndustryCode = ci.industrycode?.code;

                        var address = ci.address;
                        if (address != null)
                        {
                            onlyRunOnce = true;
                            if (editrow._Address1 != null)
                            {
                                var result = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateAddress"), Uniconta.ClientTools.Localization.lookup("Information"), UnicontaMessageBox.YesNo);
                                if (result != UnicontaMessageBox.Yes)
                                    return;
                            }
                            editrow.Address1 = address.CompleteStreet;
                            editrow.Address2 = address.ZipCity;
                            editrow.Country = address.Country;
                        }

                        if (string.IsNullOrWhiteSpace(editrow._Name))
                            editrow._Name = ci.life.name;
                        if (!string.IsNullOrEmpty(ci.contact?.phone))
                            editrow.Phone = ci.contact.phone;
                        if (!string.IsNullOrEmpty(ci.contact?.www))
                            editrow.Www = ci.contact.www;
                    }
                }
                else
                    onlyRunOnce = false;
            }
        }

        void AcItem_ButtonClicked(object sender)
        {
            AccountantAccess actAccessDialog = new AccountantAccess(api, currentAccountant);
            actAccessDialog.Closing += delegate
             {
                 if (actAccessDialog.DialogResult == true)
                 {
                     GetAccountant();
                 }
             };
            actAccessDialog.Show();
        }
        void Ac2Item_ButtonClicked(object sender)
        {
            var act2Dialog = new CWAccountants(api, currentAccountant2);
            act2Dialog.Closing += delegate
            {
                if (act2Dialog.DialogResult == true)
                {
                    currentAccountant2 = act2Dialog.selectedAccountant;
                    if (currentAccountant2 != null)
                    {
                        editrow._Accountant2 = currentAccountant2.Id;
                        txtaccountant2.Text = currentAccountant2.Name;
                    }
                    else
                    {
                        editrow._Accountant2 = 0;
                        txtaccountant2.Text = string.Empty;
                    }
                }
            };
            act2Dialog.Show();
        }
        AccountantClient currentAccountant;
        AccountantClient currentAccountant2;
        private async void GetAccountant()
        {
            var acc = await api.Query<AccountantClient>(new UnicontaBaseEntity[] { editrow }, null);
            if (acc != null && acc.Length > 0)
            {
                currentAccountant = acc.FirstOrDefault();
                editrow._Accountant = currentAccountant.Id;
                txtaccountant.Text = currentAccountant.Name;
                if (acc.Length > 1)
                {
                    currentAccountant2 = acc[1];
                    editrow._Accountant2 = currentAccountant2.Id;
                    txtaccountant2.Text = currentAccountant2.Name;
                }
                else
                {
                    editrow._Accountant2 = 0;
                    txtaccountant2.Text = string.Empty;
                    currentAccountant2 = null;
                }
            }
            else
            {
                editrow._Accountant = 0;
                txtaccountant.Text = string.Empty;
                currentAccountant = null;
                editrow._Accountant2 = 0;
                txtaccountant2.Text = string.Empty;
                currentAccountant2 = null;
            }
        }

        void EditCompany_SaveComplete(object args)
        {
            globalEvents?.OnRefresh(TabControls.CreateCompany, editrow.CompanyId);
        }
        public override Type TableType { get { return typeof(CompanyClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanyClient)value; } }

        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }

        public override void OnClosePage(object[] refreshParams)
        {
        }

        public override string NameOfControl
        {
            get { return TabControls.EditCompany; }
        }

        private void SetCountry()
        {
            var countries = Utility.GetEnumItemsWithPascalToSpace(typeof(Uniconta.Common.CountryCode));
            cmbCountry.ItemsSource = countries;
            cmbCountry.SelectedItem = editrow.CountryName;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddNote":
                    AddDockItem(TabControls.UserNotesPage, editrow, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Notes"), editrow._Name));
                    break;
                case "AddDoc":
                    AddDockItem(TabControls.UserDocsPage, editrow, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Documents"), editrow._Name));
                    break;
                case "Delete":
                    DeleteCompany();
                    break;
                case "Save":
                    save();
                    globalEvents.OnRefresh(TabControls.CreateCompany, editrow.CompanyId);
                    break;
                case "OpenDocs":
                    AddDockItem(TabControls.CompanyDocumentPage, null);
                    globalEvents.OnRefresh(TabControls.CreateCompany, editrow.CompanyId);
                    break;
                case "Activate":
                case "Deactivate":
                    CompnayActivation(ActionType);
                    break;
                case "DeleteAllTransactions":
                    DeleteAllAttachmentsCompany();
                    break;
                case "CreateBackup":
                    CreateBackupConfirmation dailog = new CreateBackupConfirmation();
                    dailog.Closed += delegate
                    {
                        if (dailog.DialogResult == true)
                        {
                            CreateBackup(dailog.name, dailog.copyTrans, dailog.copyPhysicalVouchers, dailog.copyAttachments, dailog.copyUsers);
                        }
                    };
                    dailog.Show();
                    break;
                case "AllowedIPAdresses":
                    AddDockItem(TabControls.CompanyIPRestrictionPage, editrow, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("AllowedIPAdresses"), editrow._Name));
                    break;
                case "RegisterEdelivery":
                    Nemhandel();
                    break;
                case "RefreshCache":
                    RefreshCache();
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    globalEvents.OnRefresh(TabControls.CreateCompany, editrow.CompanyId);
                    break;
            }
        }

        void RemoveMenuItem()
        {
            var country = api.CompanyEntity._CountryId;
            if (country != CountryCode.Denmark &&
                country != CountryCode.Greenland &&
                country != CountryCode.FaroeIslands)
            {
                RibbonBase rb = (RibbonBase)frmRibbon.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, "RegisterEdelivery");
            }
            if (api.CompanyEntity.NumberOfDimensions == 0)
                liFullPrimo.Visibility = Visibility.Collapsed;
        }

        async void CreateBackup(string name, bool copyTrans, bool copyPhysicalVouchers, bool copyAttachments, bool copyUsers)
        {
            var compApi = new CompanyAPI(api);
            var result = await compApi.CreateCopy(name, copyTrans,  copyPhysicalVouchers,  copyAttachments, copyUsers);
            UtilDisplay.ShowErrorCode(result);
        }

        async private void CompnayActivation(string action)
        {
            var compApi = new CompanyAPI(api);
            ErrorCodes result;
            string message;

            if (action == "Activate")
            {
                message = "ActivateCompany";
                result = await compApi.Activate();
            }
            else if (action == "Deactivate")
            {
                message = "CompanyIsDeactivated";
                result = await compApi.Deactivate();
            }
            else
            {
                message = null;
                result = ErrorCodes.NoSucces;
            }

            if (result != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(result);
            else
            {
                CloseDockItem();
                UnicontaMessageBox.Show(string.Format("{0}. {1}", Uniconta.ClientTools.Localization.lookup(message), api.CompanyEntity._Name),
                    Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
            }
        }

        private void DeleteCompany()
        {
            EraseYearWindow EraseYearWindowDialog = new EraseYearWindow(editrow.CompanyName, false);
            EraseYearWindowDialog.Closed += async delegate
            {
                if (EraseYearWindowDialog.DialogResult == true)
                {
                    CompanyAPI compApi = new CompanyAPI(api);
                    var res = await compApi.Delete();
                    if (res != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(res);
                    else
                    {
                        var companiesTemp = Controls.CWDefaultCompany.loadedCompanies.ToList();
                        companiesTemp.Remove(editrow);
                        Controls.CWDefaultCompany.loadedCompanies = companiesTemp.ToArray();

                        var defCompId = session.User._DefaultCompany;
                        //Check to Ensure that User hasn't deleted his own default company
                        var comp = companiesTemp.Where(c => c.CompanyId == defCompId).SingleOrDefault();

                        if (comp != null)
                            globalEvents.OnRefresh(TabControls.CreateCompany, comp.CompanyId);
                        else
                            globalEvents.OnRefresh(TabControls.CreateCompany, companiesTemp.FirstOrDefault());

                        dockCtrl?.CloseAllDocuments(true);
                    }
                }
            };
            EraseYearWindowDialog.Show();
        }

        private async void Nemhandel()
        {
            var res = await NHR.ValidateEndPoints(api);
            if (res != null)
            {
                UnicontaMessageBox.Show(res, Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            CWNemhandel cwNemhandel = new CWNemhandel(api);
            cwNemhandel.DialogTableId = 2000000090;
            cwNemhandel.Closed += async delegate
            {
                if (cwNemhandel.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    ErrorCodes result;
                    if (cwNemhandel.HasChanges)
                    {
                        var nhrAPI = new Uniconta.API.DebtorCreditor.NHRAPI(api);
                        result = await nhrAPI.NHROperation(cwNemhandel.EndPointLst);
                    }
                    else
                    {
                        result = ErrorCodes.NoChange;
                    }

                    busyIndicator.IsBusy = false;
                    UtilDisplay.ShowErrorCode(result);
                }
            };
            cwNemhandel.Show();
        }

        private void DeleteAllAttachmentsCompany()
        {
            EraseYearWindow EraseYearWindowDialog = new EraseYearWindow(editrow.CompanyName, false);
            EraseYearWindowDialog.Closed += delegate
            {
                if (EraseYearWindowDialog.DialogResult == true)
                {
                    EnterPasswordWindow passwordConfirmationDailog = new EnterPasswordWindow();
                    passwordConfirmationDailog.Closed += async delegate
                    {
                        if (passwordConfirmationDailog.DialogResult == true)
                        {
                            if (UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteTrans"), editrow.CompanyName), Uniconta.ClientTools.Localization.lookup("Confirmation"),
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                CompanyAPI compApi = new CompanyAPI(api);
                                var res = await compApi.EraseAllTransactions(passwordConfirmationDailog.Password);
                                if (res != ErrorCodes.Succes)
                                    UtilDisplay.ShowErrorCode(res);
                                else
                                {
                                    UtilDisplay.ShowErrorCode(res);
                                }
                            }
                        }
                    };

                    passwordConfirmationDailog.Show();
                }
            };
            EraseYearWindowDialog.Show();
        }

        async void save()
        {
            await this.saveForm();
            session.OpenCompany(api.CompanyEntity.CompanyId, true);
        }

        private void CorasauLayoutItem_OnButtonClicked(object sender)
        {
            var location = editrow.Address1 + "+" + editrow.Address2 + "+" + editrow.Address3 + "+" + editrow.Country;
            Utility.OpenGoogleMap(location);
        }
        async void RefreshCache()
        {
            ErrorCodes res = await new CompanyAPI(session, api.CompanyEntity).RefreshCache();
            UtilDisplay.ShowErrorCode(res);
        }
    }
}
