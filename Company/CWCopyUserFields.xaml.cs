using Uniconta.API.System;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using System.Windows;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CustomTableFieldsClient : TableFieldsClient
    {
        bool isSelected;
        [Display(Name = "IsSelected", ResourceType = typeof(TableFieldsText))]
        public bool IsSelected { get { return isSelected; } set { isSelected = value; NotifyPropertyChanged("IsSelected"); } }
    }
    public partial class CWCopyUserFields : ChildWindow
    {
        List<UnicontaBaseEntity> masterList;
        CrudAPI API;
        Company selectedCompany;
        List<CustomTableFieldsClient> listClient;
        bool IsCreateTable = false;
        List<TableHeaderClient> listtbl;
        TableHeaderClient table;
        CrudAPI newapi;
        UnicontaBaseEntity master;
        UnicontaBaseEntity masterWithCompanyId;
        public CWCopyUserFields(UnicontaBaseEntity sourcedata, CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#else
            this.Title = Uniconta.ClientTools.Localization.lookup("UserFields");
#endif
            API = api;
            this.master = StreamingManager.Clone(sourcedata);
            this.masterWithCompanyId = sourcedata;
            masterList = new List<UnicontaBaseEntity>();
            masterList.Add(sourcedata);
            listClient = new List<CustomTableFieldsClient>();
            this.Loaded += CWCopyUserFields_Loaded;
            this.Height += 40;
            rowh.Height = new GridLength(270);
        }

        public CWCopyUserFields(CrudAPI api)
        {
            this.DataContext = this;
#if SILVERLIGHT
           
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            InitializeComponent();
            API = api;
            IsCreateTable = true;
            masterList = new List<UnicontaBaseEntity>();
            listClient = new List<CustomTableFieldsClient>();
            this.Loaded += CWCopyUserFields_Loaded;
            this.Height += 40;
            rowh.Height = new GridLength(270);
        }
        async void CWCopyUserFields_Loaded(object sender, RoutedEventArgs e)
        {
            await BindCompany();
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    this.DialogResult = false;
                    return;
                }
                OKButton_Click(null, null);
            }
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Savefields();
        }

        async void Savefields()
        {
            ErrorCodes res = ErrorCodes.NoSucces;
            if (IsCreateTable)
            {
                if (table == null)
                {
                    this.DialogResult = false;
                    return;
                }
                table.SetMaster(API.CompanyEntity);
                res = await API.Insert(table);
            }
            else
                res = ErrorCodes.Succes;
            var selected = (from selectedlist in listClient where selectedlist.IsSelected == true select selectedlist).ToList();
            if (selected != null)
            {
                if (res == ErrorCodes.Succes)
                {
                    selected.All(a =>
                    {
                        if (IsCreateTable && table != null)
                            a.SetMaster(table);
                        else
                            a.SetMaster(masterWithCompanyId);
                        return true;
                    });
                    res = await API.Insert(selected);
                    this.DialogResult = true;
                }
                else
                {
                    UtilDisplay.ShowErrorCode(res);
                    this.DialogResult = false;
                }
            }
            else
                this.DialogResult = false;
        }       

        private async System.Threading.Tasks.Task BindCompany()
        {
            Company[] companies = await BasePage.session.GetCompanies();
            companies = companies.Where(a => a.CompanyId != Utilities.Utility.GetDefaultCompany().CompanyId).ToArray();
            cbCompany.ItemsSource = companies;
            if (companies != null && companies.Length > 0)
                cbCompany.SelectedIndex = 0;
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private async void cbCompany_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cbCompany.SelectedItem != null)
            {
                masterList = new List<UnicontaBaseEntity>();
                if (this.master != null)
                    masterList.Add(master);
                dgUserFields.ItemsSource = null;
                selectedCompany = cbCompany.SelectedItem as Company;
                newapi = new CrudAPI(BasePage.session, selectedCompany);
                if (newapi != null)
                {
                    var arrytbl = await newapi.Query<TableHeaderClient>();
                    listtbl = arrytbl?.ToList();
                    cbtable.ItemsSource = listtbl;
                    if (listtbl?.Count > 0)
                    {
                        if (master != null)
                            cbtable.SelectedItem = arrytbl?.Where(t => t.Name == (master as TableHeaderClient)?.Name).FirstOrDefault();
                        else
                            cbtable.SelectedIndex = -1;
                    }
                }
                setGridItemsSource();
            }
        }
        async void setGridItemsSource()
        {
            if (masterList.Count == 0)
                return;

            var list = await newapi.Query<CustomTableFieldsClient>(masterList, null);
            if (list != null)
            {
                foreach (var item in list)
                {
                    if (!listClient.Contains(item))
                        listClient.Add(item);
                }
                dgUserFields.ItemsSource = null;
                dgUserFields.ItemsSource = listClient;
            }
            else
                dgUserFields.ItemsSource = null;
        }

        private void cbtable_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cbtable.SelectedItem != null)
            {
                table = cbtable.SelectedItem as TableHeaderClient;
                if (!masterList.Contains(table))
                {
                    masterList.Add(table);
                    if (newapi != null)
                        setGridItemsSource();
                }
            }
        }

        private void CheckEditor_Checked(object sender, RoutedEventArgs e)
        {
            listClient.ForEach(s => s.IsSelected = true);
        }

        private void CheckEditor_Unchecked(object sender, RoutedEventArgs e)
        {
            listClient.ForEach(s => s.IsSelected = false);

        }
    }
}
