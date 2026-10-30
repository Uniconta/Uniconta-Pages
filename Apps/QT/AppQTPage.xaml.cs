using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AppQTGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(AppQTSQLClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class AppQTPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.AppQTPage; } }

        public AppQTPage(CrudAPI api)
            : base(api, null)
        {
            InitializeComponent();
            InitPage(null);
        }

        void InitPage(UnicontaBaseEntity qt)
        {
            localMenu.dataGrid = dgQTGrid;
            dgQTGrid.api = api;
            dgQTGrid.UpdateMaster(Utilities.Utility.GetCurrentApp());
            SetRibbonControl(localMenu, dgQTGrid);
            dgQTGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

       

        async private void localMenu_OnItemClicked(string ActionType)
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
