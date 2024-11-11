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
    public class UserRestrictedReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserRestrictedReportClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class UserRestrictedReportPage : GridBasePage
    {
        public UserRestrictedReportPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgUserRestReport.UpdateMaster(master);
            SetRibbonControl(localMenu, dgUserRestReport);
            dgUserRestReport.api = api;
            dgUserRestReport.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserRestReport.SelectedItem as UserRestrictedReportClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgUserRestReport.AddRow();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgUserRestReport.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
