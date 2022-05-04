using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using Uniconta.Common.Utility;
using Uniconta.API.Service;
using Uniconta.API.System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UpdateDebAddressViaCvrGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return GetTableType(); } }
        public override bool Readonly { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool IsAutoSave { get { return false; } }

        public int DCtype;

        Type GetTableType()
        {
            if (DCtype == 0)
                return typeof(DebtorClientLocal);
            else if (DCtype == 1)
                return typeof(CreditorClientLocal);
            else
                return typeof(CrmProspectClientLocal);
        }
    }
    public partial class UpdateDebAddressViaCvr : GridBasePage
    {
        public UpdateDebAddressViaCvr(BaseAPI API) : base(API, string.Empty)
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
            localMenu.dataGrid = dgUpdateDebtorAddress;
            SetRibbonControl(localMenu, dgUpdateDebtorAddress);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "DeleteRow":
                    dgUpdateDebtorAddress.RemoveFocusedRowFromGrid();
                    break;
                case "Search":
                    LoadGrid();
                    break;
                case "SaveGrid":
                    SaveGrid();
                    break;
            }
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (rec.Name == null)
                {
                    if (rec.Value == "Debtor")
                    {
                        dgUpdateDebtorAddress.DCtype = 0;
                        SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("Debtor"), ": ", Uniconta.ClientTools.Localization.lookup("UpdateAddress")));
                    }
                    else if (rec.Value == "Creditor")
                    {
                        dgUpdateDebtorAddress.DCtype = 1;
                        SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("Creditor"), ": ", Uniconta.ClientTools.Localization.lookup("UpdateAddress")));

                    }
                    else if (rec.Value == "Prospect")
                    {
                        Account.Visible = false;
                        dgUpdateDebtorAddress.DCtype = 2;
                        SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("Prospect"), ": ", Uniconta.ClientTools.Localization.lookup("UpdateAddress")));

                    }
                }
            }
            base.SetParameter(Parameters);
        }

        public override Task InitQuery()
        {
            LoadGrid();
            return null;
        }

        void LoadGrid()
        {
            SetBusy();
            if (dgUpdateDebtorAddress.DCtype == 0)
                LoadDebtorList();
            else if (dgUpdateDebtorAddress.DCtype == 1)
                LoadCreditorList();
            else
                LoadProspectList();
            dgUpdateDebtorAddress.Visibility = Visibility.Visible;
            ClearBusy();
        }

        async void LoadDebtorList()
        {
            var filter = new[] { PropValuePair.GenereteWhereElements("LegalIdent", typeof(string), "!null") };
            var debtorList = await api.Query<DebtorClientLocal>(filter);
            if (debtorList == null || debtorList.Length == 0)
                return;
            int counterFound = 0;
            var newDebList = new List<DebtorClientLocal>();
            foreach (var debtor in debtorList)
            {
                var cvr = debtor._LegalIdent;
                if (cvr == null || cvr.Length < 5)
                    continue;
                SetBusy();
                var ci = await CVR.CheckCountry(cvr, debtor._Country);
                ClearBusy();

                if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                {
                    counterFound++;
                    var address = ci.address;
                    var streetAddress = address.CompleteStreet;
                    if (streetAddress == debtor._Address1 && address.street2 == debtor._Address2 &&
                               address.zipcode == debtor._ZipCode)
                        continue;

                    var newDebtor = debtor;
                    newDebtor.NewAddress = streetAddress;
                    newDebtor.NewAddress2 = address.street2;
                    newDebtor.NewZipCode = address.zipcode;
                    newDebtor.NewCity = address.cityname;
                    newDebList.Add(newDebtor);
                }
            }
            dgUpdateDebtorAddress.ItemsSource = newDebList;
            SetStatusText(debtorList.Length, counterFound, newDebList.Count);
        }

        async void LoadCreditorList()
        {
            var filter = new[] { PropValuePair.GenereteWhereElements("LegalIdent", typeof(string), "!null") };
            var lst = await api.Query<CreditorClientLocal>(filter);
            if (lst == null || lst.Length == 0)
                return;
            int counterFound = 0;
            var newCredList = new List<CreditorClientLocal>();
            foreach (var creditor in lst)
            {
                var cvr = creditor._LegalIdent;
                if (cvr == null || cvr.Length < 5)
                    continue;
                SetBusy();
                var ci = await CVR.CheckCountry(cvr, creditor._Country);
                ClearBusy();

                if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                {
                    counterFound++;
                    var address = ci.address;
                    var streetAddress = address.CompleteStreet;
                    if (ci.life.name == creditor._Name && streetAddress == creditor._Address1 &&
                               address.zipcode == creditor._ZipCode)
                        continue;

                    var newCreditor = creditor;
                    newCreditor.NewAddress = streetAddress;
                    newCreditor.NewAddress2 = address.street2;
                    newCreditor.NewZipCode = address.zipcode;
                    newCreditor.NewCity = address.cityname;
                    newCredList.Add(newCreditor);
                }
            }
            dgUpdateDebtorAddress.ItemsSource = newCredList;
            SetStatusText(lst.Length, counterFound, newCredList.Count);
        }

        async void LoadProspectList()
        {
            var filter = new[] { PropValuePair.GenereteWhereElements("LegalIdent", typeof(string), "!null") };
            var lst = await api.Query<CrmProspectClientLocal>(filter);
            if (lst == null || lst.Length == 0)
                return;
            int counterFound = 0;
            var newProsList = new List<CrmProspectClientLocal>();
            foreach (var prospect in lst)
            {
                var cvr = prospect._LegalIdent;
                if (cvr == null || cvr.Length < 5)
                    continue;
                SetBusy();
                var ci = await CVR.CheckCountry(cvr, prospect._Country);
                ClearBusy();

                if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                {
                    counterFound++;
                    var address = ci.address;
                    var streetAddress = address.CompleteStreet;
                    if (ci.life.name == prospect._Name && streetAddress == prospect._Address1 &&
                               address.zipcode == prospect._ZipCode)
                        continue;

                    var newProspect = prospect;
                    newProspect.NewAddress = streetAddress;
                    newProspect.NewAddress2 = address.street2;
                    newProspect.NewZipCode = address.zipcode;
                    newProspect.NewCity = address.cityname;
                    newProsList.Add(newProspect);
                }
            }
            dgUpdateDebtorAddress.ItemsSource = newProsList;
            SetStatusText(lst.Length, counterFound, newProsList.Count);
        }

        async void SaveGrid()
        {
            Task<ErrorCodes> t;
            SetBusy();
            if (dgUpdateDebtorAddress.DCtype == 0)
                t = UpdateDebtorList();
            else if (dgUpdateDebtorAddress.DCtype == 1)
                t = UpdateCreditorList();
            else
                t = UpdateProspectList();
            if (t != null)
            {
                var err = await t;
                ClearBusy();
                UtilDisplay.ShowErrorCode(err);
                dgUpdateDebtorAddress.ItemsSource = null;
                LoadGrid();
            }
            else
                ClearBusy();
        }

        Task<ErrorCodes> UpdateDebtorList()
        {
            var lst = dgUpdateDebtorAddress.GetVisibleRows() as ICollection<DebtorClientLocal>;
            if (lst == null || lst.Count == 0)
                return null;
            var lst1 = new DebtorClientLocal[lst.Count];
            var lst2 = new DebtorClientLocal[lst.Count];
            int i = 0;
            foreach (var item in lst)
            {
                lst1[i] = StreamingManager.Clone(item) as DebtorClientLocal;
                item._Address1 = item.NewAddress;
                item._Address2 = item.NewAddress2;
                item._ZipCode = item.NewZipCode;
                item._City = item.NewCity;
                lst2[i] = item;
                i++;
            }
            return api.Update(lst1, lst2);
        }

        Task<ErrorCodes> UpdateCreditorList()
        {
            var lst = dgUpdateDebtorAddress.GetVisibleRows() as ICollection<CreditorClientLocal>;
            if (lst == null || lst.Count == 0)
                return null;
            var lst1 = new CreditorClientLocal[lst.Count];
            var lst2 = new CreditorClientLocal[lst.Count];
            int i = 0;
            foreach (var item in lst)
            {
                lst1[i] = StreamingManager.Clone(item) as CreditorClientLocal;
                item._Address1 = item.NewAddress;
                item._Address2 = item.NewAddress2;
                item._ZipCode = item.NewZipCode;
                item._City = item.NewCity;
                lst2[i] = item;
                i++;
            }
            return api.Update(lst1, lst2);
        }

        Task<ErrorCodes> UpdateProspectList()
        {
            var lst = dgUpdateDebtorAddress.GetVisibleRows() as ICollection<CrmProspectClientLocal>;
            if (lst == null || lst.Count == 0)
                return null;
            var lst1 = new CrmProspectClientLocal[lst.Count];
            var lst2 = new CrmProspectClientLocal[lst.Count];
            int i = 0;
            foreach (var item in lst)
            {
                lst1[i] = StreamingManager.Clone(item) as CrmProspectClientLocal;
                item._Address1 = item.NewAddress;
                item._Address2 = item.NewAddress2;
                item._ZipCode = item.NewZipCode;
                item._City = item.NewCity;
                lst2[i] = item;
                i++;
            }
            return api.Update(lst1, lst2);
        }

        void SetStatusText(int total, int found, int newRecord)
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            var foundTxt = Uniconta.ClientTools.Localization.lookup("Available");
            var totalTxt = Uniconta.ClientTools.Localization.lookup("Total");
            var newTxt = Uniconta.ClientTools.Localization.lookup("New");

            foreach (var grp in groups)
            {
                if (grp.Caption == totalTxt)
                    grp.StatusValue = total.ToString();
                else if (grp.Caption == foundTxt)
                    grp.StatusValue = found.ToString();
                else if (grp.Caption == newTxt)
                    grp.StatusValue = newRecord.ToString();
                else
                    grp.StatusValue = string.Empty;
            }
        }
    }

    public class DebtorClientLocal : DebtorClient
    {
        private string _address, _address2, _city, _zipCode;
        [StringLength(60)]
        [Display(Name = "NewAddress", ResourceType = typeof(DCAccountText))]
        public string NewAddress { get { return _address; } set { _address = value; NotifyPropertyChanged("NewAddress1"); } }

        [StringLength(60)]
        [Display(Name = "NewAddress2", ResourceType = typeof(DCAccountText))]
        public string NewAddress2 { get { return _address2; } set { _address2 = value; NotifyPropertyChanged("NewAddress2"); } }

        [StringLength(12)]
        [Display(Name = "NewZipCode", ResourceType = typeof(DCAccountText))]
        public string NewZipCode { get { return _zipCode; } set { if (_zipCode == value) return; _zipCode = value; NotifyPropertyChanged("NewZipCode"); } }

        [StringLength(30)]
        [Display(Name = "NewCity", ResourceType = typeof(DCAccountText))]
        public string NewCity { get { return _city; } set { _city = value; NotifyPropertyChanged("NewCity"); } }
    }

    public class CreditorClientLocal : CreditorClient
    {
        private string _address, _address2, _city, _zipCode;
        [StringLength(60)]
        [Display(Name = "NewAddress", ResourceType = typeof(DCAccountText))]
        public string NewAddress { get { return _address; } set { _address = value; NotifyPropertyChanged("NewAddress1"); } }

        [StringLength(60)]
        [Display(Name = "NewAddress2", ResourceType = typeof(DCAccountText))]
        public string NewAddress2 { get { return _address2; } set { _address2 = value; NotifyPropertyChanged("NewAddress2"); } }

        [StringLength(12)]
        [Display(Name = "NewZipCode", ResourceType = typeof(DCAccountText))]
        public string NewZipCode { get { return _zipCode; } set { if (_zipCode == value) return; _zipCode = value; NotifyPropertyChanged("NewZipCode"); } }

        [StringLength(30)]
        [Display(Name = "NewCity", ResourceType = typeof(DCAccountText))]
        public string NewCity { get { return _city; } set { _city = value; NotifyPropertyChanged("NewCity"); } }
    }

    public class CrmProspectClientLocal : CrmProspectClient
    {
        private string _address, _address2, _city, _zipCode;
        [StringLength(60)]
        [Display(Name = "NewAddress", ResourceType = typeof(DCAccountText))]
        public string NewAddress { get { return _address; } set { _address = value; NotifyPropertyChanged("NewAddress1"); } }

        [StringLength(60)]
        [Display(Name = "NewAddress2", ResourceType = typeof(DCAccountText))]
        public string NewAddress2 { get { return _address2; } set { _address2 = value; NotifyPropertyChanged("NewAddress2"); } }

        [StringLength(12)]
        [Display(Name = "NewZipCode", ResourceType = typeof(DCAccountText))]
        public string NewZipCode { get { return _zipCode; } set { if (_zipCode == value) return; _zipCode = value; NotifyPropertyChanged("NewZipCode"); } }

        [StringLength(30)]
        [Display(Name = "NewCity", ResourceType = typeof(DCAccountText))]
        public string NewCity { get { return _city; } set { _city = value; NotifyPropertyChanged("NewCity"); } }
    }
}

