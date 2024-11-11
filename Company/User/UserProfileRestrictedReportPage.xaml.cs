using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections;
using System.Linq;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Corasau.Admin.API;
using System.Collections.Generic;
using System.Windows.Input;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserProfileRestrictedReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserProfileRestrictedReportClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class UserProfileRestrictedReportPage : GridBasePage
    {
        public UserProfileRestrictedReportPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgUserPrflRestReport.UpdateMaster(master);
            SetRibbonControl(localMenu, dgUserPrflRestReport);
            dgUserPrflRestReport.api = api;
            dgUserPrflRestReport.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserPrflRestReport.SelectedItem as UserProfileRestrictedReportClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgUserPrflRestReport.AddRow();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgUserPrflRestReport.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
