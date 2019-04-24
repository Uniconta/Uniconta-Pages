using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
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
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class IOBSClaimLinePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(IOBSClaimLineClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class IOBSClaimLinePage : GridBasePage
    {
        public IOBSClaimLinePage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }

        public IOBSClaimLinePage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            ((TableView)dgIOBSClaimLinePageGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            dgIOBSClaimLinePageGrid.UpdateMaster(master);
            dgIOBSClaimLinePageGrid.api = api;
            SetRibbonControl(localMenu, dgIOBSClaimLinePageGrid);
            dgIOBSClaimLinePageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgIOBSClaimLinePageGrid.SelectedItem as IOBSClaimLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgIOBSClaimLinePageGrid.AddRow();
                    break;
                case "CopyRow":
                    dgIOBSClaimLinePageGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgIOBSClaimLinePageGrid.SaveData();
                    break;
                case "DeleteRow":
                    dgIOBSClaimLinePageGrid.DeleteRow();
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgIOBSClaimLinePageGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgIOBSClaimLinePageGrid.syncEntity);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
