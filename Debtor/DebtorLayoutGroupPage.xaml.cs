using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class DebtorLayoutGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorLayoutGroupClient); } }

    }

    /// <summary>
    /// Interaction logic for DebtorLayoutGroupPage.xaml
    /// </summary>
    public partial class DebtorLayoutGroupPage : GridBasePage
    {
        public DebtorLayoutGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public DebtorLayoutGroupPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgDebtorLayoutGroupGrid);
            dgDebtorLayoutGroupGrid.api = api;
            dgDebtorLayoutGroupGrid.BusyIndicator = busyIndicator;
            
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorLayoutGroupPage2)
                dgDebtorLayoutGroupGrid.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorLayoutGroupGrid.SelectedItem as DebtorLayoutGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    var newItem = dgDebtorLayoutGroupGrid.GetChildInstance();
                    object[] param = new object[2];
                    param[0] = newItem;
                    param[1] = false;
                    AddDockItem(TabControls.DebtorLayoutGroupPage2, param, Uniconta.ClientTools.Localization.lookup("DebtorLayoutgroup"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] para = new object[2];
                    para[0] = selectedItem;
                    para[1] = true;
                    AddDockItem(TabControls.DebtorLayoutGroupPage2, para, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("DebtorLayoutgroup"), selectedItem.Name), null, true);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
