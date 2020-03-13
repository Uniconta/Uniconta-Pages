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

    public partial class CWApprovalEmailSetup : ChildWindow
    {
        public Company _CompanyObj { get; set; }
        IList<CompanySMTPClient> smtps;
        CrudAPI api;
        CompanySettingsClient cSetting;
        public CWApprovalEmailSetup(CrudAPI api, IList<CompanySMTPClient> smtps)
        {
            this.DataContext = this;
            this.smtps = smtps;
            this.api = api;
            InitializeComponent();
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#else
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("EmailForOBJ"), Uniconta.ClientTools.Localization.lookup("Approval")); ;
#endif
            this.Loaded += CWApprovalEmailSetup_Loaded;
        }

        private async void CWApprovalEmailSetup_Loaded(object sender, RoutedEventArgs e)
        {
            BindApprover();
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }
        private async System.Threading.Tasks.Task BindApprover()
        {
            leApproverSetup.ItemsSource = smtps;
            cSetting = new CompanySettingsClient();
            var err = await api.Read(cSetting);
            if (err != ErrorCodes.Succes)
                OKButton.IsEnabled = false;
            var approveSmtp = cSetting.ApproveSMTP;
            if (!string.IsNullOrEmpty(approveSmtp))
            {
                var smtp = smtps.Where(s => s.Number == approveSmtp).FirstOrDefault();
                leApproverSetup.SelectedItem = smtp;
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
            var selectedSMTP = leApproverSetup.SelectedItem as CompanySMTPClient;
            var number = selectedSMTP?.Number;
            if (cSetting.ApproveSMTP != number)
            {
                var modified = new CompanySettings();
                StreamingManager.Copy(cSetting, modified);
                modified._ApproveSMTP = number;
                api.UpdateNoResponse(cSetting, modified);
            }
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

