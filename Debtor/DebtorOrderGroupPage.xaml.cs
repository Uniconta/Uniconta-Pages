using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections;
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
    public class DebtorOrderGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorOrderGroupClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override bool Readonly { get { return false; } }
    }
  
    public partial class DebtorOrderGroupPage : GridBasePage
    {
        protected override bool IsLayoutSaveRequired() { return false; }

        public DebtorOrderGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgDebtorOrderGroup);
            dgDebtorOrderGroup.api = api;
            dgDebtorOrderGroup.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgDebtorOrderGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgDebtorOrderGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgDebtorOrderGroup.SaveData();                  
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
