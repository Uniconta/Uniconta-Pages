using System;
using System.Collections.Generic;
using System.Linq;
using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Pages.Maintenance;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System.Collections;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading.Tasks;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.Common.User;
using Uniconta.API.Service;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ConfigureFunctionalityPage : FormBasePage
    {
        Company editrow;

        public ConfigureFunctionalityPage(BaseAPI API)
            : this(API.CompanyEntity)
        {
        }
        public ConfigureFunctionalityPage(UnicontaBaseEntity sourceData)
            : base(sourceData, true)
        {
            InitializeComponent();
            SetGroupHeaders();
            layoutControl = layoutItems;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            SetStorage(editrow.Storage);
            if (api.session.User._Role < (byte)Uniconta.Common.User.UserRoles.Accountant)
                Loaded += ConfigureFunctionalityPage_Loaded;

            grpOnlyPosting.IsCollapsed = editrow.FullPackage;

            if (BasePage.GetSystemDefaultDate() < new DateTime(2022, 8, 15))
                grpOnlyPosting.Visibility = Visibility.Collapsed;

            cmbDocumentScanner.ItemsSource = AppEnums.DocumentScanner.Values;
            cmbDocumentScanner.SelectedIndex = (int)editrow.DocumentScanner;
        }

        private void ConfigureFunctionalityPage_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DisclaimerInConfiguration"), Uniconta.ClientTools.Localization.lookup("Information"));
            }));
        }

        private void SetGroupHeaders()
        {
            var addinsText = Uniconta.ClientTools.Localization.lookup("Addins").Split('(');
            grpBoxAddins.Header = addinsText[0];
        }

        private async void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
            {
                editrow._LogTableChanges = editrow.TraceFieldChanges;
                await this.saveForm();
                var MainPageObj = App.MainPageObj;
                MainPageObj.dockCtrl.CloseAllDocuments(false);
                MainPageObj.setLeftmenu();
            }
            else
                frmRibbon_BaseActions(ActionType);
        }

        public override Type TableType { get { return typeof(Company); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (Company)value; } }

        public override void OnClosePage(object[] refreshParams)
        {
        }
        public override string NameOfControl
        {
            get { return TabControls.ConfigureFunctionalityPage; }
        }
        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup)
        {
            return false;
        }
        private void CheckEditor_Checked(object sender, RoutedEventArgs e)
        {
            SetStorage(((CheckEditor)sender).IsChecked == true );
        }
        void SetStorage(bool HasStorage)
        {
            cbStorageOnAll.IsEnabled = HasStorage;
        }

        private void cmbDocumentScanner_SelectedIndexChanged(object sender, RoutedEventArgs e) =>
            editrow.DocumentScanner = (PayableDocumentScanners)cmbDocumentScanner.SelectedIndex;
    }
}
