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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorGroupClient); } }
    }
    public partial class CreditorGroup : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.CreditorGroup.ToString(); }
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        public CreditorGroup(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        public CreditorGroup(BaseAPI API)
            : base(API, string.Empty)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgCreditorGroupGrid);
            dgCreditorGroupGrid.api = api;
            dgCreditorGroupGrid.BusyIndicator = busyIndicator;
            
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorGroupGrid.SelectedItem as CreditorGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.CreditorGroupPage2, api, Uniconta.ClientTools.Localization.lookup("CreditorGroup"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem;
                    EditParam[1] = true;
                    AddDockItem(TabControls.CreditorGroupPage2, EditParam, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("CreditorGroup"), selectedItem.Name));
                    break;
                case "CopyRow":
                    if (selectedItem == null)
                        return;
                    object[] copyParam = new object[2];
                    copyParam[0] = selectedItem;
                    copyParam[1] = false;
                    string header = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem.Group);
                    AddDockItem(TabControls.CreditorGroupPage2, copyParam, header);
                    break;
                case "GroupPosting":
                    if (selectedItem == null) return;
                    string grpPostingHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ItemPosting"), selectedItem.Group);
                    AddDockItem(TabControls.CreditorGroupPostingPage, selectedItem, grpPostingHeader);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorGroupPage2)
                dgCreditorGroupGrid.UpdateItemSource(argument);
        }
        
        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.GLVat) });
        }

        protected override void OnLayoutCtrlLoaded()
        {
            var Comp = api.CompanyEntity;
            if (!Comp.InvDuty)
                this.ExemptDuty.ShowInColumnChooser = false;
            else
                this.ExemptDuty.ShowInColumnChooser = true;
        }
    }
}
