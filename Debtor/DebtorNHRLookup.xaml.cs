using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.Common.Utility;
using Uniconta.API.Service;
using System.Text.RegularExpressions;
using System.Collections;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorNHRLookupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return GetTableType(); } }
        public override bool Readonly { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool IsAutoSave { get { return false; } }

        public int DCtype;

        Type GetTableType()
        {
            return typeof(DebtorNHR);
        }
    }
    public partial class DebtorNHRLookup : GridBasePage
    {
        ItemBase iActivate, iDeavtivate, iUpdateRegistration, iErrRegistration;
        SQLTableCache<Uniconta.DataModel.Debtor> debtorCache;
        SQLTableCache<Uniconta.DataModel.Contact> contactCache;

        Uniconta.DataModel.Contact[] contactArr;
        Uniconta.DataModel.Contact searchContact;
        SortContact sortContact;

        public DebtorNHRLookup(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = false;
            return false;
        }

        public override bool IsCalculatedFieldsToBeHandled()
        {
            return false;
        }

        private void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgDebtorNHRLookup;
            SetRibbonControl(localMenu, dgDebtorNHRLookup);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            localMenu.OnChecked += LocalMenu_OnChecked;
            GetMenuItem();

            debtorCache = api.GetCache<Uniconta.DataModel.Debtor>();
            contactCache = api.GetCache<Uniconta.DataModel.Contact>();
        }

        private void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            iActivate = UtilDisplay.GetMenuCommandByName(rb, "ActivateNemhandel");
            iDeavtivate = UtilDisplay.GetMenuCommandByName(rb, "DeactivateNemhandel");
            iUpdateRegistration = UtilDisplay.GetMenuCommandByName(rb, "UpdateRegistration");
            iErrRegistration = UtilDisplay.GetMenuCommandByName(rb, "ErrorRegistration");
        }

        private void LocalMenu_OnChecked(string ActionType, bool IsChecked)
        {
            int index = 0;
            switch (ActionType)
            {
                case "ActivateNemhandel":
                    index = 2;
                    break;
                case "DeactivateNemhandel":
                    index = 3;
                    break;
                case "UpdateRegistration":
                    index = 4;
                    break;
                case "ErrorRegistration":
                    index = 5;
                    break;
            }

            if (IsChecked)
                ApplyFilter(index);
            else
                UnApplyFilter(index);
        }

        private void ApplyFilter(int index)
        {
            var filterStr = AppEnums.NemhandelStatus.Label(index);

            if (!string.IsNullOrEmpty(filterStr))
            {
                var newFitlerStr = string.Format("[Status] = '{0}'", Uniconta.ClientTools.Localization.lookup(filterStr));
                if (!string.IsNullOrEmpty(dgDebtorNHRLookup.FilterString))
                    dgDebtorNHRLookup.FilterString = string.Format("{0} Or {1}", dgDebtorNHRLookup.FilterString, newFitlerStr);
                else
                    dgDebtorNHRLookup.FilterString = newFitlerStr;
            }
            else
                dgDebtorNHRLookup.FilterString = string.Empty;
        }

        private void UnApplyFilter(int index)
        {
            var filterStr = Uniconta.ClientTools.Localization.lookup(AppEnums.NemhandelStatus.Label(index));

            var currentFilterStr = dgDebtorNHRLookup.FilterString;
            if (currentFilterStr.Contains("Or"))
            {
                var andFilters = currentFilterStr.Split(new[] { "Or" }, StringSplitOptions.None);
                var newfilters = new string[andFilters.Length - 1];
                int idx = 0;
                foreach (var filter in andFilters)
                {
                    if (filter.Contains(filterStr))
                        continue;

                    newfilters[idx] = filter;
                    idx++;
                }
                dgDebtorNHRLookup.FilterString = string.Join("Or", newfilters);
            }
            else
                dgDebtorNHRLookup.FilterString = string.Empty;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorNHRLookup.SelectedItem as DebtorNHR;

            switch (ActionType)
            {
                case "Remove":
                    dgDebtorNHRLookup.RemoveFocusedRowFromGrid();
                    break;
                case "Search":
                    LoadGrid();
                    break;
                case "SaveGrid":
                    SaveGrid();
                    break;
                case "NHR":
                    if (selectedItem != null)
                        NHRUrl(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override Task InitQuery()
        {
            return null;
        }

        void LoadGrid()
        {
            SetBusy();
            LoadDebtorList();
            dgDebtorNHRLookup.Visibility = Visibility.Visible;
            ClearBusy();
        }

        void NHRUrl(DebtorNHR selectedItem)
        {
            bool isLive = api.session.Connection.Target == APITarget.Live;

            var endPointKeyType = selectedItem.NewGLN != null && selectedItem.NewGLN != NHR.SELECTGLN ? "GLN" : "DK:CVR";
            var endPointKey = selectedItem.NewGLN != null && selectedItem.NewGLN != NHR.SELECTGLN ? selectedItem.NewGLN : selectedItem.CVRNumber;
            var urlNHR = string.Concat("https://registration", isLive ? null : "-demo", ".nemhandel.dk/NemHandelRegisterWeb/public/participant/info?keytype=", endPointKeyType, "&key=", endPointKey);
            Utility.OpenWebSite(urlNHR);
        }


        static bool Equal(string s1, string s2)
        {
            if (s1 != null)
            {
                s1 = s1.Trim();
                if (s1.Length == 0)
                    s1 = null;
            }
            return string.Compare(s1, s2, StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        async void LoadDebtorList()
        {
            var filter = new List<PropValuePair>()
            {
                PropValuePair.GenereteWhereElements(nameof(DebtorNHR._LegalIdent), typeof(string), "!null"),
                PropValuePair.GenereteWhereElements(nameof(DebtorNHR.Country), typeof(int), NumberConvert.ToString((int)CountryCode.Denmark)),
                PropValuePair.GenereteWhereElements(nameof(DebtorNHR.Country), typeof(int), NumberConvert.ToString((int)CountryCode.Greenland)),
                PropValuePair.GenereteWhereElements(nameof(DebtorNHR.Country), typeof(int), NumberConvert.ToString((int)CountryCode.FaroeIslands))
            };

            var isLive = api.session.Connection.Target == APITarget.Live;
            var debtorList = await api.Query<DebtorNHR>(filter);
            if (debtorList == null || debtorList.Length == 0)
                return;
            int cntAll = 0;
            int cntActivateNHR = 0;
            int cntDeactivateNHR = 0;
            int cntUpdateRegistration = 0;
            int cntErrorRegistration = 0;
            int cntNHRInActive = 0;
            int cntNHRActive = 0;

            var newDebNHRLst = new List<DebtorNHR>(debtorList.Count());
            int cntTotal = debtorList.Length;

            if (contactCache == null)
                contactCache = await api.LoadCache<Uniconta.DataModel.Contact>();

            contactArr = contactCache.ToArray();
            sortContact = new SortContact();
            Array.Sort(contactArr, sortContact);
            searchContact = new Contact();

            foreach (var debtor in debtorList)
            {
                busyIndicator.IsBusy = true;

                try
                {
                    if (HasGLNSetting(debtor))
                    {
                        cntNHRActive++;
                        continue;
                    }

                    cntAll++;
                    if (cntAll == 1 || cntAll == cntTotal || (cntAll % 50) == 0)
                    {
                        busyIndicator.BusyContent = string.Concat(Uniconta.ClientTools.Localization.lookup("Loading") + " " + NumberConvert.ToString(cntAll), " af ", cntTotal);
                        busyIndicator.IsBusy = false;
                    }

                    var nhrInfo = await NHR.Lookup(debtor, isLive);
                    if (nhrInfo == null || nhrInfo.NotRegistered)
                    {
                        cntNHRInActive++;
                        continue;
                    }

                    switch (nhrInfo.Status)
                    {
                        case NemhandelStatus.OK: cntNHRActive++; break;
                        case NemhandelStatus.ActivateNHR: cntActivateNHR++; break;
                        case NemhandelStatus.DeactivateNHR: cntDeactivateNHR++; break;
                        case NemhandelStatus.UpdateRegistration: cntUpdateRegistration++; break;
                        case NemhandelStatus.ErrorRegistration: cntErrorRegistration++; break;
                    }

                    if (nhrInfo.Status != Uniconta.ClientTools.NemhandelStatus.OK && nhrInfo.Status != Uniconta.ClientTools.NemhandelStatus.None)
                    {
                        var debtorNHR = debtor;
                        debtorNHR.StatusInfo = nhrInfo.StatusInfo;
                        debtorNHR.NewGLN = nhrInfo.GLNNew;
                        debtorNHR.OnlyOIORASP = nhrInfo.OnlyOIORASP;
                        debtorNHR._Status = (byte)nhrInfo.Status;
                        debtorNHR.EndPointURL = nhrInfo.EndPointURL;
                        debtorNHR.EndPointRegisterName = nhrInfo.EndPointRegisterName;
                        debtorNHR.GLNSource = nhrInfo.GLNList;

                        newDebNHRLst.Add(debtorNHR);
                    }
                }
                catch
                {
                    busyIndicator.IsBusy = false;
                }
            }
            ClearBusy();
            dgDebtorNHRLookup.ItemsSource = newDebNHRLst;
            SetStatusText(cntTotal, cntNHRInActive, cntNHRActive, cntActivateNHR, cntDeactivateNHR, cntUpdateRegistration, cntErrorRegistration);
            iActivate.IsChecked = true;
            iDeavtivate.IsChecked = true;
        }

        bool HasGLNSetting(DebtorNHR debtor)
        {
            var debInv = (DCAccount)debtorCache?.Get(debtor._InvoiceAccount);
            if (debInv?._EAN != null)
                return true;

            searchContact._DCType = debtor.__DCType();
            searchContact._DCAccount = debtor._Account;
            var pos = Array.BinarySearch(contactArr, searchContact, sortContact);
            if (pos < 0)
                pos = ~pos;

            bool found = false;
            while (pos < contactArr.Length)
            {
                var s = contactArr[pos++];
                if (s._DCAccount != debtor._Account)
                    break;

                if (s._EAN != null)
                {
                    found = true;
                    break;
                }
            }
            if (found)
                return true;

            return false;
        }

        private void NewGLN_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgDebtorNHRLookup.SelectedItem as DebtorNHR;

            if (selectedItem == null)
                return;

            var cmb = sender as ComboBoxEditor;
            if (cmb != null && selectedItem.GLNSource != null)
            {
                var src = selectedItem.GLNSource as IEnumerable<NHRGLN>;
                cmb.ItemsSource = src.Select(p => string.Format("{0}  ({1})", p.Id, p.Name));
            }
        }

        async void SaveGrid()
        {
            Task<ErrorCodes> t;
            SetBusy();
            t = UpdateDebtorList();
            if (t != null)
            {
                var err = await t;
                ClearBusy();
                if (err == ErrorCodes.Succes && !api.CompanyEntity._OIOUBLSendOnServer)
                    UnicontaMessageBox.Show("For at kunne sende via Nemhandel skal 'Send e-faktura fra serveren' aktiveres under Firmaoplysninger", Uniconta.ClientTools.Localization.lookup("Information"));
                else
                    UtilDisplay.ShowErrorCode(err);
                dgDebtorNHRLookup.ItemsSource = null;
            }
            else
            {
                UtilDisplay.ShowErrorCode(ErrorCodes.NoLinesToUpdate);
                ClearBusy();
            }
        }

        Task<ErrorCodes> UpdateDebtorList()
        {
            var dcAccLst = dgDebtorNHRLookup.GetVisibleRows() as ICollection<DebtorNHR>;
            if (dcAccLst == null || dcAccLst.Count == 0)
                return null;
            var lst1 = new List<DebtorNHR>();
            var lst2 = new List<DebtorNHR>();
            int i = 0;
            foreach (var dcAcc in dcAccLst)
            {
                bool eInvoice = false;
                string newGLN = null;

                if (dcAcc._Status == (byte)NemhandelStatus.None || dcAcc._Status == (byte)NemhandelStatus.OK)
                    continue;

                if (dcAcc._Status == (byte)NemhandelStatus.ActivateNHR)
                    eInvoice = true;
                else if (dcAcc._Status == (byte)NemhandelStatus.DeactivateNHR)
                    eInvoice = false;
                else if (dcAcc._Status == (byte)NemhandelStatus.UpdateRegistration || dcAcc._Status == (byte)NemhandelStatus.ErrorRegistration)
                {
                    bool updateGLN = false;
                    newGLN = dcAcc.NewGLN;
                    if (newGLN != null)
                    {
                        if (newGLN.Length != 13)
                            newGLN = StringBuilderReuse.Create(newGLN).SubString(0, 13);

                        if (newGLN != null && Util.IsValidEAN(newGLN))
                        {
                            updateGLN = true;
                            eInvoice = true;
                        }
                    }

                    if (!updateGLN)
                        continue;
                }
                
                var dictKey = ((long)dcAcc.CompanyId << 32) + dcAcc.RowId;
                if (NHR.dictNHRInfo.ContainsKey(dictKey))
                    NHR.dictNHRInfo.Remove(dictKey);
                lst1.Add(StreamingManager.Clone(dcAcc) as DebtorNHR);

                dcAcc._Status = (byte)NemhandelStatus.OK;
                dcAcc._InvoiceInXML = eInvoice;
                if (newGLN != null)
                    dcAcc.EAN = newGLN;

                lst2.Add(dcAcc);
                i++;
            }

            if (lst1.Count == 0)
                return null;

            return api.Update(lst1, lst2);
        }

        void SetStatusText(int total, int nhrInActive, int nhrActive, int activateNHR, int deactivateNHR, int updateRegistration, int errorRegistration)
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);

            var totalTxt = Uniconta.ClientTools.Localization.lookup("NumberOfCustomers");
            //var nhrActiveTxt = "Send via Nemhandel";
            //var nhrInActiveTxt = Uniconta.ClientTools.Localization.lookup("NemhandelNotRegistered");
            var nhrActiveTxt = string.Format(Uniconta.ClientTools.Localization.lookup("NemhandelOBJ"), Uniconta.ClientTools.Localization.lookup("Active").ToLower());
            var nhrInActiveTxt = string.Format(Uniconta.ClientTools.Localization.lookup("NemhandelOBJ"), Uniconta.ClientTools.Localization.lookup("Inactive").ToLower());

            var activateNHRTxt = Uniconta.ClientTools.Localization.lookup("ActivateNemhandel");
            var deactivateNHRTxt = Uniconta.ClientTools.Localization.lookup("DeactivateNemhandel");
            var updateTxt = Uniconta.ClientTools.Localization.lookup("UpdateRegistration");
            var errorTxt = Uniconta.ClientTools.Localization.lookup("ErrorRegistration");

            foreach (var grp in groups)
            {
                if (grp.Caption == totalTxt)
                    grp.StatusValue = NumberConvert.ToString(total);
                else if (grp.Caption == nhrActiveTxt)
                    grp.StatusValue = NumberConvert.ToString(nhrActive);
                else if (grp.Caption == nhrInActiveTxt)
                    grp.StatusValue = NumberConvert.ToString(nhrInActive);
                else
                    grp.StatusValue = string.Empty;
            }

            iActivate.Caption = string.Format("{0}: {1}", activateNHRTxt, NumberConvert.ToString(activateNHR));
            iDeavtivate.Caption = string.Format("{0}: {1}", deactivateNHRTxt, NumberConvert.ToString(deactivateNHR));
            iUpdateRegistration.Caption = string.Format("{0}: {1}", updateTxt, NumberConvert.ToString(updateRegistration));
            iErrRegistration.Caption = string.Format("{0}: {1}", errorTxt, NumberConvert.ToString(errorRegistration));
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            debtorCache = debtorCache ?? await api.LoadCache<Uniconta.DataModel.Debtor>().ConfigureAwait(false);
            contactCache = contactCache ?? await api.LoadCache<Uniconta.DataModel.Contact>().ConfigureAwait(false);
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var debtorNHR = dgDebtorNHRLookup.SelectedItem as DebtorNHR;
            if (debtorNHR == null)
                return lookup;
            if (dgDebtorNHRLookup.CurrentColumn?.Name == "Account")
            {
                lookup.TableType = typeof(Uniconta.DataModel.Debtor);
            }
            return lookup;
        }
    }

    public class DebtorNHR : DebtorClient
    {
        private string _NewGLN;
        private string _StatusInfo;
        private string _EndPointURL;
        private string _EndPointRegisterName;
        private bool _OnlyOIORASP;
        private object _glnSource;
        public byte _Status;

        [StringLength(20)]
        [Display(Name = "CompanyRegNo", ResourceType = typeof(DCAccountText))]
        public string CVRNumber { get { return Regex.Replace(_LegalIdent ?? string.Empty, "[^0-9]", ""); } }

        [Display(Name = "NewGLN", ResourceType = typeof(DCAccountText))]
        public string NewGLN { get { return _NewGLN; } set { _NewGLN = value; NotifyPropertyChanged("NewGLN"); } }

        [StringLength(150)]
        [Display(Name = "StatusInfo", ResourceType = typeof(DCTransText))]
        public string StatusInfo { get { return _StatusInfo; } set { _StatusInfo = value; NotifyPropertyChanged("StatusInfo"); } }

        [AppEnumAttribute(EnumName = "NemhandelStatus")]
        [Display(Name = "Status", ResourceType = typeof(DCAccountText))]
        public string Status { get { return AppEnums.NemhandelStatus.ToString(_Status); } }

        [Display(Name = "OIORASP", ResourceType = typeof(DCAccountText))]
        public bool OnlyOIORASP { get { return _OnlyOIORASP; } set { _OnlyOIORASP = value; NotifyPropertyChanged("OnlyOIORASP"); } }

        [StringLength(150)]
        [Display(Name = "EndPointRegisterName", ResourceType = typeof(DCAccountText))]
        public string EndPointRegisterName { get { return _EndPointRegisterName; } set { _EndPointRegisterName = value; NotifyPropertyChanged("EndPointRegisterName"); } }

        [StringLength(150)]
        [Display(Name = "EndPointURL", ResourceType = typeof(DCAccountText))]
        public string EndPointURL { get { return _EndPointURL; } set { _EndPointURL = value; NotifyPropertyChanged("EndPointURL"); } }

        public object GLNSource { get { return _glnSource; } set { _glnSource = value; } }

        [ReportingAttribute]
        public DebtorClient DebtorRef
        {
            get
            {
                return ClientHelper.GetRefClient<Uniconta.ClientTools.DataModel.DebtorClient>(CompanyId, typeof(Uniconta.DataModel.Debtor), this.RowId);
            }
        }
    }

    class SortContact : IComparer<Uniconta.DataModel.Contact>
    {
        public int Compare(Uniconta.DataModel.Contact x, Uniconta.DataModel.Contact y)
        {
            var c = x._DCType - y._DCType;
            if (c != 0)
                return c;

            var s = string.Compare(x._DCAccount, y._DCAccount);
            if (s != 0)
                return s;

            return x.RowId - y.RowId;
        }
    }

}
