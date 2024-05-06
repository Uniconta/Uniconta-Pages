using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Windows;
using DevExpress.Xpf.Grid;
using Uniconta.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EmployeeRegistrationLinePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EmployeeRegistrationLineClient); } }
        public override bool Readonly { get { return false; } }

    }

    public partial class EmployeeRegistrationLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EmployeeRegistrationLinePage; } }
        SQLCache ProjectCache, EmployeeCache;
        UnicontaBaseEntity _master;

        public EmployeeRegistrationLinePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            ((TableView)dgEmployeeRegistrationLinePageGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgEmployeeRegistrationLinePageGrid;
            dgEmployeeRegistrationLinePageGrid.api = api;
            _master = master;

            dgEmployeeRegistrationLinePageGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgEmployeeRegistrationLinePageGrid);
            dgEmployeeRegistrationLinePageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgEmployeeRegistrationLinePageGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
        }
        protected override void OnLayoutLoaded()
        {
            if (!api.CompanyEntity.Project)
            {
                Project.Visible = false;
                UtilDisplay.RemoveMenuCommand((RibbonBase)localMenu.DataContext, new string[] { "PrTransaction" });
            }

            if (_master is EmployeeClient)
                Employee.Visible = false;
            base.OnLayoutLoaded();
        }
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Project), api).ConfigureAwait(false);
            EmployeeCache = Comp.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Employee), api).ConfigureAwait(false);
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            EmployeeRegistrationLineClient oldselectedItem = e.OldItem as EmployeeRegistrationLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= SelectedItem_PropertyChanged;

            EmployeeRegistrationLineClient selectedItem = e.NewItem as EmployeeRegistrationLineClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += SelectedItem_PropertyChanged;
            }
        }

        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (EmployeeRegistrationLineClient)sender;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgEmployeeRegistrationLinePageGrid.SelectedItem as EmployeeRegistrationLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgEmployeeRegistrationLinePageGrid.AddRow();
                    break;
                case "CopyRow":
                    var row = dgEmployeeRegistrationLinePageGrid.CopyRow() as EmployeeRegistrationLineClient;
                    if (row != null)
                    {
                        if (row._Date < DateTime.Now && row._Date != DateTime.MinValue)
                            row.Date = DateTime.Now.Date;
                    }
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgEmployeeRegistrationLinePageGrid.DeleteRow();
                    break;
                case "AddMileage":
                    var mileage = new EmployeeRegistrationLineClient();
                    mileage._Activity = InternalType.Mileage;
                    mileage.SetMaster(api.CompanyEntity);
                    mileage.SetMaster(_master);

                    AddDockItem(TabControls.RegisterMileage, new object[2] { mileage, false }, string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Mileage")));
                    break;
                case "EditMileage":
                    if (selectedItem?._Activity == InternalType.Mileage)
                        AddDockItem(TabControls.RegisterMileage, new object[2] { selectedItem, true }, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Mileage"), selectedItem.RowId));
                    break;
                case "PrTransaction":
                    if (selectedItem != null && selectedItem._LineNumber != 0)
                        AddDockItem(TabControls.ProjectTransactionPage, dgEmployeeRegistrationLinePageGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem._Employee));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
