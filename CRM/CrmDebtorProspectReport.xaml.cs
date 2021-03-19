using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using System.ComponentModel.DataAnnotations;
using Uniconta.DataModel;
using Uniconta.Common;
using UnicontaClient.Models;
using Uniconta.API.Crm;
using Uniconta.ClientTools.Util;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Controls;
using System.Collections;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CrmDebtorProspectReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CrmProspectView); } }
        public override bool CanInsert { get { return false; } }
        public override bool Readonly { get { return true; } }
    }

    public partial class CrmDebtorProspectReport : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.CrmDebtorProspectReport.ToString(); }
        }

        public override bool IsDataChaged { get { return false; } }

        public CrmDebtorProspectReport(BaseAPI API)
            : base(API, string.Empty)
        {
            this.DataContext = this;
            InitializeComponent();
            localMenu.dataGrid = dgCrmDebtorProspect;
            dgCrmDebtorProspect.api = api;
            dgCrmDebtorProspect.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCrmDebtorProspect);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCrmDebtorProspect.View.DataControl.CurrentItemChanged += DgCrmDebtorProspect_CurrentItemChanged;
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = useBinding= true;
            return true;
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var crmView = dg.SelectedItem as CrmDebtorView;

            if (crmView == null) return lookup;

            if (dg.CurrentColumn?.Name == "AccountNumber")
                lookup.TableType = typeof(Uniconta.DataModel.Debtor);
            return lookup;
        }

        private void DgCrmDebtorProspect_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            if (e.NewItem is CrmDebtorView)
            {
                if (e.OldItem != null && e.OldItem is CrmDebtorView)
                    return;
                else
                    ribbonControl?.DisableButtons(new string[] { "ConvertToDebtor" });
            }
            else if (e.NewItem is CrmProspectView)
            {
                if (e.OldItem != null && e.OldItem is CrmProspectView)
                    return;
                else
                    ribbonControl?.EnableButtons(new string[] { "ConvertToDebtor" });
            }
        }

        public override void Utility_Refresh(string screenName, object argument)
        {
            object[] args = argument as object[];
            if (screenName == TabControls.CrmProspectPage2 || screenName == TabControls.DebtorAccountPage2)
            {
                var gridSourceList = (System.Collections.IList)dgCrmDebtorProspect.ItemsSource;
                UnicontaBaseEntity objectView = null;
                if (args[1] is CrmProspectClient)
                    objectView = StreamingManager.Copy(args[1] as CrmProspectClient, new CrmProspectView());
                else if (args[1] is DebtorClient)
                    objectView = StreamingManager.Copy(args[1] as DebtorClient, new CrmDebtorView());
                var index = gridSourceList.IndexOf(objectView);
                if (index > -1)
                {
                    gridSourceList[index] = objectView;
                    int rowHandle = dgCrmDebtorProspect.GetRowHandleByListIndex(index);
                    dgCrmDebtorProspect.RefreshRow(rowHandle);
                    dgCrmDebtorProspect.SelectedItem = objectView;
                }
                else if (objectView != null)
                {
                    gridSourceList.Add(objectView);
                    dgCrmDebtorProspect.ItemsSource = null;
                    dgCrmDebtorProspect.ItemsSource = gridSourceList;
                    dgCrmDebtorProspect.SelectedItem = objectView;
                    dgCrmDebtorProspect.Visibility = Visibility.Visible;
                }
            }
        }

        private void UpdateGridOnAdd(UnicontaBaseEntity entity, IList gridSourceList)
        {
            object objectView = null;
            if (entity is CrmProspectClient)
                objectView = StreamingManager.Copy(entity as CrmProspectClient, new CrmProspectView());
            else if (entity is DebtorClient)
                objectView = StreamingManager.Copy(entity as CrmProspectClient, new CrmDebtorView());

            if (objectView != null)
            {
                gridSourceList.Add(objectView);
                dgCrmDebtorProspect.ItemsSource = null;
                dgCrmDebtorProspect.ItemsSource = gridSourceList;
                dgCrmDebtorProspect.SelectedItem = objectView;
                dgCrmDebtorProspect.Visibility = Visibility.Visible;
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCrmDebtorProspect.SelectedItem as ICrmProspect;

            switch (ActionType)
            {
                case "AddDebtor":
                case "AddProspect":
                    object[] param = new object[2] { api, null };
                    if (ActionType == "AddDebtor")
                        AddDockItem(TabControls.DebtorAccountPage2, param, Uniconta.ClientTools.Localization.lookup("DebtorAccount"), "Add_16x16.png");
                    else if (ActionType == "AddProspect")
                        AddDockItem(TabControls.CrmProspectPage2, param, Uniconta.ClientTools.Localization.lookup("Prospects"), "Add_16x16.png");
                    break;

                case "EditRow":
                    if (selectedItem is CrmProspectView)
                    {
                        var crmProspectUser = api.CompanyEntity.CreateUserType<CrmProspectClient>();
                        StreamingManager.Copy(selectedItem as CrmProspectClient, crmProspectUser);
                        AddDockItem(TabControls.CrmProspectPage2, new object[] { crmProspectUser, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Prospects"), selectedItem.Name));
                    }
                    else if (selectedItem is CrmDebtorView)
                    {
                        var debtorUser = api.CompanyEntity.CreateUserType<DebtorClient>();
                        StreamingManager.Copy(selectedItem as DebtorClient, debtorUser);
                        AddDockItem(TabControls.DebtorAccountPage2, new object[] { debtorUser, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("DebtorAccount"), selectedItem.Name));
                    }
                    break;

                case "Contacts":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ContactPage, dgCrmDebtorProspect.syncEntity);
                    break;

                case "FollowUp":
                    if (selectedItem is CrmDebtorView)
                    {
                        var crmSelectedItem = selectedItem as CrmDebtorView;
                        var followUpHeader = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), crmSelectedItem._Name, Uniconta.ClientTools.Localization.lookup("Debtor"));
                        AddDockItem(TabControls.CrmFollowUpPage, dgCrmDebtorProspect.syncEntity, followUpHeader);
                    }
                    else if (selectedItem is CrmProspectView)
                    {
                        var crmSelectedItem = selectedItem as CrmProspectView;
                        var followUpHeader = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), crmSelectedItem._Name, Uniconta.ClientTools.Localization.lookup("Prospects"));
                        AddDockItem(TabControls.CrmFollowUpPage, dgCrmDebtorProspect.syncEntity, followUpHeader);
                    }
                    break;

                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgCrmDebtorProspect.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgCrmDebtorProspect.syncEntity);
                    break;
                case "Orders":
                    if (dgCrmDebtorProspect.syncEntity == null || selectedItem == null || !selectedItem.IsDebtor)
                        return;
                    AddDockItem(TabControls.DebtorOrdersMultiple, dgCrmDebtorProspect.syncEntity);
                    break;
                case "Offers":
                    if (selectedItem is CrmProspectView)
                        AddDockItem(TabControls.DebtorOffers, selectedItem as CrmProspectClient);
                    else if (selectedItem is CrmDebtorView)
                        AddDockItem(TabControls.DebtorOffers, selectedItem as DebtorClient);
                    break;

                case "ConvertToDebtor":
                    if (selectedItem is CrmProspectView)
                        ConvertProspectToDebtor(selectedItem as CrmProspectClient);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public async override Task InitQuery()
        {
            busyIndicator.IsBusy = true;
            var debtorEntity = await api.Query<CrmDebtorView>();
            var prospectEntity = await api.Query<CrmProspectView>();

            var debtorProspectList = new List<ICrmProspect>();
            if (debtorEntity != null)
                debtorProspectList.AddRange(debtorEntity);
            if (prospectEntity != null)
                debtorProspectList.AddRange(prospectEntity);

            dgCrmDebtorProspect.ItemsSource = debtorProspectList;
            busyIndicator.IsBusy = false;
            dgCrmDebtorProspect.Visibility = Visibility.Visible;
        }

        void ConvertProspectToDebtor(CrmProspectClient crmProspect)
        {
            if (crmProspect == null) return;

            CWConvertProspectToDebtor cwwin = new CWConvertProspectToDebtor(api, crmProspect);
            cwwin.Closing += async delegate
            {
                if (cwwin.DialogResult == true)
                {
                    if (cwwin?.DebtorClient == null) return;
                    CrmAPI crmApi = new CrmAPI(api);
                    var res = await crmApi.ConvertToDebtor(crmProspect, cwwin.DebtorClient);

                    if (res == ErrorCodes.Succes)
                    {
                        UtilDisplay.ShowErrorCode(res);
                        InitQuery();
                    }
                    else
                        UtilDisplay.ShowErrorCode(res);
                }
            };
            cwwin.Show();
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectedRowImageTag = (sender as Image).Tag;

            OnClickImage(selectedRowImageTag, TabControls.UserNotesPage);
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectedRowImageTag = (sender as Image).Tag;

            OnClickImage(selectedRowImageTag, TabControls.UserDocsPage);
        }

        private void OnClickImage(object crmView, string page)
        {
            if (crmView == null) return;

            if (crmView is CrmProspectView)
            {
                var prospectClient = crmView as CrmProspectClient;
                if (prospectClient != null)
                    AddDockItem(page, prospectClient);
            }
            else if (crmView is CrmDebtorView)
            {
                var debtorClient = crmView as DebtorClient;
                if (debtorClient != null)
                    AddDockItem(page, debtorClient);
            }
        }

#if !SILVERLIGHT
        private void HasEmailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var prospect = (sender as TextBlock).Tag as ICrmProspect;

            if (prospect != null)
            {
                var mail = string.Concat("mailto:", prospect.ContactEmail);
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = mail;
                proc.Start();
            }
        }
#endif
    }

    public interface ICrmProspect
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(CrmGroup))]
        [Display(Name = "Group", ResourceType = typeof(DCGroupText))]
        string CrmGroup { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.DebtorGroup))]
        [Display(Name = "DebGroup", ResourceType = typeof(DCGroupText))]
        string DebGroup { get; set; }

        [Display(Name = "GroupName", ResourceType = typeof(DCOrderGroupText))]
        [NoSQL]
        string GroupName { get; }

        [Display(Name = "DAccount", ResourceType = typeof(GLTableText))]
        string AccountNumber { get; }

        [StringLength(60)]
        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        string Name { get; set; }

        [StringLength(60)]
        [Display(Name = "Address1", ResourceType = typeof(DCAccountText))]
        string Address1 { get; set; }

        [StringLength(60)]
        [Display(Name = "Address2", ResourceType = typeof(DCAccountText))]
        string Address2 { get; set; }

        [StringLength(60)]
        [Display(Name = "Address3", ResourceType = typeof(DCAccountText))]
        string Address3 { get; set; }

        [Display(Name = "City", ResourceType = typeof(DCAccountText))]
        string City { get; set; }

        [StringLength(10)]
        [Display(Name = "ZipCode", ResourceType = typeof(DCAccountText))]
        string ZipCode { get; set; }

        [Display(Name = "Country", ResourceType = typeof(DCAccountText))]
        CountryCode Country { get; set; }

        [StringLength(20)]
        [Display(Name = "CompanyRegNo", ResourceType = typeof(DCAccountText))]
        string CompanyRegNo { get; set; }

        [AppEnumAttribute(EnumName = "Currencies")]
        [Display(Name = "Currency", ResourceType = typeof(DCAccountText))]
        string Currency { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone", ResourceType = typeof(DCAccountText))]
        string Phone { get; set; }

        [AppEnumAttribute(EnumName = "VatZones")]
        [Display(Name = "VatZone", ResourceType = typeof(DCAccountText))]
        string VatZone { get; set; }

        [Display(Name = "PriceGroup", ResourceType = typeof(DCAccountText))]
        byte PriceGroup { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(DebtorPriceList))]
        [Display(Name = "PriceList", ResourceType = typeof(InvPriceListClientText))]
        string PriceList { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvItemNameGroup))]
        [Display(Name = "ItemNameGroup", ResourceType = typeof(DCAccountText))]
        string ItemNameGroup { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(DebtorLayoutGroup))]
        [Display(Name = "LayoutGroup", ResourceType = typeof(DCAccountText))]
        string LayoutGroup { get; set; }

        [Display(Name = "Blocked", ResourceType = typeof(DCAccountText))]
        bool Blocked { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLDimType1))]
        string Dimension1 { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLDimType2))]
        string Dimension2 { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLDimType3))]
        string Dimension3 { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLDimType4))]
        string Dimension4 { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLDimType5))]
        string Dimension5 { get; set; }

        [AppEnumAttribute(EnumName = "Language")]
        [Display(Name = "Language", ResourceType = typeof(UserClientText))]
        string UserLanguage { get; set; }

        [StringLength(40)]
        [Display(Name = "ContactEmail", ResourceType = typeof(DCAccountText))]
        string ContactEmail { get; set; }

        [StringLength(20)]
        [Display(Name = "MobilPhone", ResourceType = typeof(DCAccountText))]
        string MobilPhone { get; set; }

        [StringLength(40)]
        [Display(Name = "InvoiceEmail", ResourceType = typeof(DCAccountText))]
        string InvoiceEmail { get; set; }

        [StringLength(30)]
        [Display(Name = "ContactPerson", ResourceType = typeof(DCAccountText))]
        string ContactPerson { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        [Display(Name = "Employee", ResourceType = typeof(EmployeeText))]
        string Employee { get; set; }

        [Display(Name = "EAN", ResourceType = typeof(DCAccountText))]
        string EAN { get; set; }

        [Display(Name = "Interests", ResourceType = typeof(DCAccountText))]
        string Interests { get; set; }

        [Display(Name = "Products", ResourceType = typeof(DCAccountText))]
        string Products { get; set; }

        [ReportingAttribute]
        DebtorClient Debtor { get; }

        [ReportingAttribute]
        CrmProspectClient Prospect { get; }

        [ReportingAttribute]
        EmployeeClient EmployeeRef { get; }

        [ReportingAttribute]
        DebtorGroupClient DebtorGroup { get; }

        [ReportingAttribute]
        CrmGroupClient CrmGroupRef { get; }

        [ReportingAttribute]
        CompanyClient CompanyRef { get; }

        IEnumerable<UserDocsClient> Attachments { get; set; }
        IEnumerable<UserNotesClient> Notes { get; set; }
        IEnumerable<ContactClient> Contacts { get; set; }
        IEnumerable<CrmFollowUpClient> Activities { get; set; }

        object CalculatedFields { get; }

        bool IsDebtor { get; }
    }

    public class CrmProspectView : CrmProspectClient, ICrmProspect
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(CrmProspect))]
        [Display(Name = "DAccount", ResourceType = typeof(GLTableText)), Key, Required]
        public string AccountNumber { get { return SeqNumber == 0 ? null : Convert.ToString(SeqNumber); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(CrmGroup))]
        [Display(Name = "CrmGroup", ResourceType = typeof(DCAccountText))]
        public string CrmGroup { get { return _Group; } set { _Group = value; NotifyPropertyChanged("CrmGroup"); } }

        public bool IsDebtor { get { return false; } }

        [ReportingAttribute]
        public DebtorClient Debtor { get { return null; } }
        [ReportingAttribute]
        public CrmProspectClient Prospect { get { return this; } }
    }

    public class CrmDebtorView : DebtorClient, ICrmProspect
    {
        [Display(Name = "SeqNumber", ResourceType = typeof(DCAccountText)), Key]
        public int SeqNumber { get { return RowId; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(CrmGroup))]
        [Display(Name = "Group", ResourceType = typeof(DCGroupText))]
        public string GroupName { get { return ClientHelper.GetName(CompanyId, typeof(Uniconta.DataModel.DebtorGroup), _Group); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        [Display(Name = "DAccount", ResourceType = typeof(GLTableText)), Key, Required]
        public string AccountNumber { get { return _Account; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.DebtorGroup))]
        [Display(Name = "DebGroup", ResourceType = typeof(DCGroupText))]
        public string DebGroup { get { return _Group; } set { _Group = value; NotifyPropertyChanged("DebGroup"); } }

        public bool IsDebtor { get { return true; } }

        [ReportingAttribute]
        public DebtorClient Debtor { get { return this; } }

        [ReportingAttribute]
        public CrmProspectClient Prospect { get { return null; } }
    }
}
