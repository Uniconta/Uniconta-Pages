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
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Utilities;
using UnicontaClient.Models;
using Uniconta.ClientTools.Util;
using System.Windows;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class UserDocsPage3 : FormBasePage
    {
        UserDocsClient userDocClient;

        public UserDocsPage3(SynchronizeEntity master)
            : base(true, master)
        {
            this.DataContext = this;
            InitializeComponent();
            var corasauMaster = master.Row;
            InitMaster(corasauMaster, true);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            InitMaster(args, false);
        }

        async private void InitMaster(UnicontaBaseEntity corasauMaster, bool setFocus)
        {
            if (corasauMaster == null)
                return;

            ModifiedRow = corasauMaster;

            if (userDocClient._Data == null)
            {
                busyIndicator.IsBusy = true;
                await api.Read(userDocClient);
            }

            busyIndicator.IsBusy = false;
            this.documentViewer.Children.Clear();

            try
            {
                this.documentViewer.Children.Add(UtilDisplay.LoadControl(userDocClient.UserDocument, userDocClient.DocumentType, false, setFocus));
            }
            catch
            {
                this.documentViewer.Children.Add(UtilDisplay.LoadDefaultControl(Uniconta.ClientTools.Localization.lookup("InvalidDocSave")));
            }
            SetMetaInfo(userDocClient);
        }

        private void SetMetaInfo(UserDocsClient userDocsClient)
        {
            metaInfoCtrl.SetlValues(userDocClient.Text, userDocClient.Created.ToString("g"), userDocClient.DocumentType.ToString(), userDocClient.UserName);
        }

        public override Type TableType { get { return typeof(UserDocsClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return userDocClient; } set { userDocClient = (UserDocsClient)value; } }

        public override void OnClosePage(object[] refreshParams) { globalEvents.OnRefresh(NameOfControl, refreshParams); }

        public override string NameOfControl { get { return TabControls.UserDocsPage3.ToString(); } }

        private void saveImage_Click(object sender, RoutedEventArgs e)
        {
            busyIndicator.IsBusy = true;
            UtilDisplay.SaveData(userDocClient.UserDocument, userDocClient.DocumentType);
            busyIndicator.IsBusy = false;
        }

        private void cancelWindow_Click(object sender, RoutedEventArgs e)
        {
            frmRibbon_BaseActions("Cancel");
        }

        private void ViewProgram_Click(object sender, RoutedEventArgs e)
        {
            if (userDocClient?._Data == null)
                return;
            busyIndicator.IsBusy = true;
            VouchersPage3.ViewInProgram(userDocClient.UserDocument, userDocClient.DocumentType);
            busyIndicator.IsBusy = false;
        }
    }
}
