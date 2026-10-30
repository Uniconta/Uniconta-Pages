using System;
using System.Collections;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AppShareGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(AppShareClient); } }
    }

    public partial class AppSharePage : GridBasePage
    {
        public override string NameOfControl => TabControls.AppSharePage;

        public AppSharePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            dgGrid.api = api;
            dgGrid.UpdateMaster(master);
            dgGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
