using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DCPreviousAddressPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DCPreviousAddressClient); } }
        public override bool Readonly { get { return false; } }
        public override bool SingleBufferUpdate { get { return false; } }
    }

    public partial class DCPreviousAddressPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DCPreviousAddressPage; } }

        public DCPreviousAddressPage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgDCPreviousAddress;
            SetRibbonControl(localMenu, dgDCPreviousAddress);
            dgDCPreviousAddress.api = api;
            dgDCPreviousAddress.UpdateMaster(master);
            dgDCPreviousAddress.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgDCPreviousAddress.AddRow();
                    break;
                case "CopyRow":
                    dgDCPreviousAddress.CopyRow();
                    break;
                case "SaveGrid":
                    dgDCPreviousAddress.SaveData();
                    break;
                case "DeleteRow":
                    dgDCPreviousAddress.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
