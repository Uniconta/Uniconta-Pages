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

    public class CreditorOrderGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorOrderGroupClient); } }
        public override bool Readonly { get { return false; } }
    }
    
    public partial class CreditorOrderGroupPage : GridBasePage
    {
        protected override bool IsLayoutSaveRequired() { return false; }

        public CreditorOrderGroupPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgCreditorOrderGroup;
            SetRibbonControl(localMenu, dgCreditorOrderGroup);
            dgCreditorOrderGroup.api = api;
            dgCreditorOrderGroup.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgCreditorOrderGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgCreditorOrderGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgCreditorOrderGroup.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;

            }
        }
    }
}
