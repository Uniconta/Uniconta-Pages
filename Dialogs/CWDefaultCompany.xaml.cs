using UnicontaClient.Utilities;
using Uniconta.API.Service;
using Uniconta.ClientTools.Page;
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
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.Common.User;

namespace UnicontaClient.Controls
{
    public partial class CWDefaultCompany : ChildWindow
    {
        static public Company[] loadedCompanies;

        Session currentSession;
        public Company selectedCompany;
        Company setCompany = null;
        public CompanyPermissions companyPermission;
        bool isDefaultCompanySet;
        public CWDefaultCompany(Session session, Company company, bool hideDefault = false, bool isManageCmp = false)
        {
            setCompany = company;
            currentSession = session;
            this.DataContext = this;
            InitializeComponent();

#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("Company");
#else
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            if (loadedCompanies != null)
            {
                cbCompany.ItemsSource = loadedCompanies;
                cbCompany.SelectedItem = company;
            }
            else
                BindCompany();
            if (hideDefault)
            {
                txtDefault.Visibility = chkDefault.Visibility = Visibility.Collapsed;
                txtUserRights.Visibility = cmbUserRights.Visibility = Visibility.Visible;
            }
            else
            {
                var selectedCompany = cbCompany.SelectedItem as Company;
                if (selectedCompany?.CompanyId == currentSession.User._DefaultCompany)
                    chkDefault.IsChecked = isDefaultCompanySet = true;
            }

            if (isManageCmp)
            {
                cbCompany.Visibility = Visibility.Collapsed;
                lblCompany.Visibility = Visibility.Collapsed;
            }

            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cbCompany.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
                {
                    if (OKButton.IsFocused)
                        OKButton_Click(null, null);
                    else if (CancelButton.IsFocused)
                        SetDialogResult(false);
                }
        }
        private async System.Threading.Tasks.Task BindCompany()
        {
            Company[] companies = await BasePage.session.GetCompanies();
            cbCompany.ItemsSource = companies;
            if (companies != null)
                cbCompany.SelectedItem = setCompany;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            selectedCompany = cbCompany.SelectedItem as Company;
            if (selectedCompany != null && chkDefault.Visibility == Visibility.Visible)
            {
                if (chkDefault.IsChecked == true)
                {
                    if (selectedCompany.CompanyId != currentSession.User._DefaultCompany)
                        currentSession.SetDefaultCompany(selectedCompany.CompanyId);
                }
                else if(isDefaultCompanySet)
                    currentSession.SetDefaultCompany(0);
            }
            else
                companyPermission = (CompanyPermissions)cmbUserRights.SelectedIndex;

            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}

