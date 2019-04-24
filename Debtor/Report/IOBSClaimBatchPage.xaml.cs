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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class IOBSClaimBatchPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(IOBSClaimBatchClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class IOBSClaimBatchPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.IOBSClaimBatchPage.ToString(); }}

        public IOBSClaimBatchPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            ((TableView)dgIOBSClaimBatchPageGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            dgIOBSClaimBatchPageGrid.api = api;
            SetRibbonControl(localMenu, dgIOBSClaimBatchPageGrid);
            dgIOBSClaimBatchPageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgIOBSClaimBatchPageGrid.SelectedItem as IOBSClaimBatchClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgIOBSClaimBatchPageGrid.AddRow();
                    break;
                case "CopyRow":
                    dgIOBSClaimBatchPageGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgIOBSClaimBatchPageGrid.SaveData();
                    break;
                case "DeleteRow":
                    dgIOBSClaimBatchPageGrid.DeleteRow();
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgIOBSClaimBatchPageGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgIOBSClaimBatchPageGrid.syncEntity);
                    break;
                case "ClaimTable":
                    if (selectedItem != null)
                        AddDockItem(TabControls.IOBSClaimTablePage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ClaimTable"), selectedItem.BatchNumber));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
