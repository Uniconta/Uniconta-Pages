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
    
    public partial class CWSelectCompany : ChildWindow
    {
        public Company _CompanyObj { get; set; }
        public CWSelectCompany(CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#else
             this.Title = Uniconta.ClientTools.Localization.lookup("Company");
#endif
            this.Loaded += CWSelectCompany_Loaded;
        }

        private async void CWSelectCompany_Loaded(object sender, RoutedEventArgs e)
        {
            await BindCompany();
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }
        private async System.Threading.Tasks.Task BindCompany()
        {
            Company[] companies = await BasePage.session.GetCompanies();            
            cbCompany.ItemsSource = companies;
            if (companies != null && companies.Length > 0)
                cbCompany.SelectedIndex = 0;
        }
        private  void cbCompany_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cbCompany.SelectedItem != null)
            {
                _CompanyObj = cbCompany.SelectedItem as Company;              
            }
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
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

