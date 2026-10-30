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
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using System.Threading.Tasks;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using Uniconta.API.System;
using Uniconta.DataModel;
using System.Windows;
using Uniconta.API.Service;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserAssociateGrid : CorasauDataGridClient
    {
        public override bool Readonly { get { return true; } }

        public override Type TableType => typeof(UserAssociateClient);
    }

    public partial class UserAssociateGridPage : GridBasePage
    {


        public UserAssociateGridPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }


        private void InitPage(UnicontaBaseEntity master)
        {
            this.DataContext = this;
            InitializeComponent();
            SetRibbonControl(localMenu, dgAssociateGrid);
            dgAssociateGrid.BusyIndicator = busyIndicator;
            dgAssociateGrid.api = api;
            dgAssociateGrid.UpdateMaster(master);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            //dgAssociateGrid.RowDoubleClick += dgAssociateGrid_RowDoubleClick;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.UserAssociatePage2)
                dgAssociateGrid.UpdateItemSource(argument);
        }
        protected override void OnLayoutLoaded()
        {
            var rb = localMenu.DataContext as RibbonBase;
            UtilDisplay.RemoveMenuCommand(rb, "CopyRow");
        }
        //void dgAssociateGrid_RowDoubleClick()
        //{
        //    ribbonControl.PerformRibbonAction("EditRow");
        //}

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgAssociateGrid.SelectedItem as UserAssociateClient;

            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.UserAssociatePage2, null, string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("UserAssociate")));
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserAssociatePage2, new object[] { selectedItem, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("UserAssociate"), selectedItem.RowId));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        //private void PrimaryKeyId_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    ribbonControl.PerformRibbonAction("EditRow");
        //}
    }
}
