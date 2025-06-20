using UnicontaClient.Utilities;
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
using Uniconta.API.System;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using System.Windows;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWReadOnlyCompany : ChildWindow
    {
        public Company selectedCompany { get; set; }
        Company[] companies;
        public CWReadOnlyCompany(Company[] objcompanies)
        {
            companies = objcompanies;
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("ViewDemoCompany");
            this.Loaded += CWReadOnlyCompany_Loaded;
        }

        public CWReadOnlyCompany(Company[] objcompanies, string title): this(objcompanies)
        {
            this.Title = Uniconta.ClientTools.Localization.lookup(title);
        }
        void CWReadOnlyCompany_Loaded(object sender, RoutedEventArgs e)
        {
            BindCompany();
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
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
                    if (CancelButton.IsFocused)
                    {
                        SetDialogResult(false);
                        return;
                    }
                    OKButton_Click(null, null);
                }
        }
        private void BindCompany()
        {
            cbCompany.ItemsSource = companies;
            if (companies != null && companies.Length > 0)
                cbCompany.SelectedIndex = 0;
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (cbCompany.SelectedItem != null)
            {
                selectedCompany = cbCompany.SelectedItem as Company;
                SetDialogResult(true);
            }
            else
                SetDialogResult(false);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}

