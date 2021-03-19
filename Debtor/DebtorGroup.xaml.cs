using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
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
    public class DebtorGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorGroupClient); } }
    }
    public partial class DebtorGroup : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.DebtorGroup.ToString(); }
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        public DebtorGroup(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        public DebtorGroup(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgDebtorGroupGrid;
            SetRibbonControl(localMenu, dgDebtorGroupGrid);
            dgDebtorGroupGrid.api = api;
            dgDebtorGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        protected override void OnLayoutCtrlLoaded()
        {
            var Comp = api.CompanyEntity;
            if (!Comp.InvDuty)
                this.ExemptDuty.ShowInColumnChooser = false;
            else
                this.ExemptDuty.ShowInColumnChooser = true;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorGroupGrid.SelectedItem as DebtorGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.DebtorGroupPage2, api, Uniconta.ClientTools.Localization.lookup("DebtorGroup"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem;
                    EditParam[1] = true;
                    AddDockItem(TabControls.DebtorGroupPage2, EditParam, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("DebtorGroup"), selectedItem.Name));
                    break;
                case "CopyRow":
                    if (selectedItem == null)
                        return;
                    object[] copyParam = new object[2];
                    copyParam[0] = selectedItem;
                    copyParam[1] = false;
                    string header = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem.Group);
                    AddDockItem(TabControls.DebtorGroupPage2, copyParam, header);
                    break;
                case "GroupPosting":
                    if (selectedItem == null) return;

                    string grpPostingHeader = string.Format("{0}: {1}", string.Format(Uniconta.ClientTools.Localization.lookup("GroupPostingOBJ"), Uniconta.ClientTools.Localization.lookup("Debtor")),
                       selectedItem.Group);

                    AddDockItem(TabControls.DebtorGroupPostingPage, selectedItem, grpPostingHeader);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorGroupPage2)
                dgDebtorGroupGrid.UpdateItemSource(argument);
        }
       
        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.GLVat) });
        }
    }
}
