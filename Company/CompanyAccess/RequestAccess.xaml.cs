using UnicontaClient.Utilities;
using Uniconta.API.System;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
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
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools;
using System.Windows;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class RequestAccess : ChildWindow
    {
        public RequestAccess()
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("RequestCompanyAccess");
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }
        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorCodes res = await BasePage.session.RequestCompanyAccess(txtCompanyName.Text, txtCompanyId.EditValue != null ? (int)txtCompanyId.EditValue:0);
            if (res == ErrorCodes.Succes)
                SetDialogResult(true);
            UtilDisplay.ShowErrorCode(res);             
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}

