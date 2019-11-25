using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InventoryGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvGroupClient); } }
    }
    public partial class InventoryGroups : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InventoryGroups; } }

        public InventoryGroups(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            Init();
        }
        public InventoryGroups(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgInventoryGroupGrid);
            dgInventoryGroupGrid.api = api;
            dgInventoryGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var Comp = api.CompanyEntity;
            if (!Comp.InvDuty)
                DutyGroup.ShowInColumnChooser = DutyGroup.Visible = false;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.InventoryGroupPage2)
                dgInventoryGroupGrid.UpdateItemSource(argument);
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInventoryGroupGrid.SelectedItem as InvGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.InventoryGroupPage2, api, Uniconta.ClientTools.Localization.lookup("InventoryGroup"), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] param = new object[2];
                    param[0] = selectedItem;
                    param[1] = true;
                    AddDockItem(TabControls.InventoryGroupPage2, param, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("InventoryGroup"), selectedItem.Group, selectedItem.Name));
                    break;
                case "CopyRow":
                    if (selectedItem == null)
                        return;
                    object[] copyParam = new object[2];
                    copyParam[0] = selectedItem;
                    copyParam[1] = false;
                    string header = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem.Group);
                    AddDockItem(TabControls.InventoryGroupPage2, copyParam, header);
                    break;
                case "DebtorGroupPosting":
                    if (selectedItem == null) return;

                    string debtPostingHeader = string.Format("{0}: {1}", string.Format(Uniconta.ClientTools.Localization.lookup("GroupPostingOBJ"), Uniconta.ClientTools.Localization.lookup("Debtor")),
                      selectedItem.Group);
                    AddDockItem(TabControls.DebtorGroupPostingPage, selectedItem, debtPostingHeader);
                    break;
                case "CreditorGroupPosting":
                    if (selectedItem == null) return;

                    string credPostingHeader = string.Format("{0}: {1}", string.Format(Uniconta.ClientTools.Localization.lookup("GroupPostingOBJ"), Uniconta.ClientTools.Localization.lookup("Creditor")),
                      selectedItem.Group);
                    AddDockItem(TabControls.CreditorGroupPostingPage, selectedItem, credPostingHeader);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
