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
    public class AttachmentGroupDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(AttachmentGroupClient); } }
        
        public override bool Readonly { get { return false; } }
    }
    public partial class AttachmentGroupPage : GridBasePage
    {
        public AttachmentGroupPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgAttachmentGroup;
            dgAttachmentGroup.api = api;
            dgAttachmentGroup.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgAttachmentGroup);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgAttachmentGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgAttachmentGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgAttachmentGroup.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
