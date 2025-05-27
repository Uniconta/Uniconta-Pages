using UnicontaClient.Controls;
using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using DevExpress.XtraMap.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TableHeaderShareGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TableHeaderShareClient); } }
        public override bool Readonly { get { return false; } }
        public override bool CanUpdate { get { return false; } }
    }

    public partial class TableHeaderSharePage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.TableHeaderSharePage.ToString(); }
        }

        public TableHeaderSharePage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            ((TableView)dgTableHeaderShareGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            dgTableHeaderShareGrid.api = api;
            dgTableHeaderShareGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgTableHeaderShareGrid);
            if (master != null)
                dgTableHeaderShareGrid.UpdateMaster(master);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgTableHeaderShareGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            LoadComboControl();
        }

        void LoadComboControl()
        {
            List<Company> compList = new List<Company>();
            var comp = new Company();
            comp._Name = "";
            compList.Add(comp);
            var companies = CWDefaultCompany.loadedCompanies;
            compList.AddRange(companies.ToList());
            cbCompany.ItemsSource = compList.OrderBy(x => x.CompanyId).ToList();
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as TableHeaderShareClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= TableHeaderSharedGrid_PropertyChanged;
            var selectedItem = e.NewItem as TableHeaderShareClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += TableHeaderSharedGrid_PropertyChanged;
        }

        private void TableHeaderSharedGrid_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var rec = sender as TableHeaderShareClient;
            switch (e.PropertyName)
            {
                case "SharedToCompany":
                    CompanyAccess(rec);
                    break;
            }
        }

        async void CompanyAccess(TableHeaderShareClient rec)
        {
            var comp = await BasePage.session.GetCompany(rec.SharedToCompany);
            if (comp == null)
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("UserNoAccessToCompany")), Uniconta.ClientTools.Localization.lookup("Error"));
            else
            {
                rec._CompanyName = comp._Name;
                rec.NotifyPropertyChanged("CompanyName");
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgTableHeaderShareGrid.SelectedItem as TableHeaderShareClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgTableHeaderShareGrid.AddRow();
                    break;
                case "DeleteRow":
                    dgTableHeaderShareGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
