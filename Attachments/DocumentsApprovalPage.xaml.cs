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
using System.Windows.Shapes;
using Uniconta.API.GeneralLedger;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Models;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DocumentsApprovalPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VouchersClient); } }
        public override IComparer GridSorting { get { return new SortDocAttached(); } }
    }
    public partial class DocumentsApprovalPage : GridBasePage
    {
        Uniconta.DataModel.Employee employee;
        public DocumentsApprovalPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }
        public DocumentsApprovalPage(BaseAPI API, Uniconta.DataModel.Employee _employee)
           : base(API, string.Empty)
        {
            employee = _employee;
            InitPage();
        }

        void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgVoucherApproveGrid;
            dgVoucherApproveGrid.api = api;
            dgVoucherApproveGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgVoucherApproveGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override bool IsDataChaged { get { return false; } }

        public async override Task InitQuery()
        {
            var docApi = new DocumentAPI(api);
            busyIndicator.IsBusy = true;
            var documents = (VouchersClient[])await docApi.DocumentsForApproval(new VouchersClient(), employee);
            busyIndicator.IsBusy = false;
            dgVoucherApproveGrid.ItemsSource = documents?.ToList();
            dgVoucherApproveGrid.Visibility = Visibility.Visible;
            dgVoucherApproveGrid.BusyIndicator = busyIndicator;
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgVoucherApproveGrid.SelectedItem as VouchersClient;
            if (selectedItem == null)
                return;
            switch (ActionType)
            {
                case "Approve":
                    ApproveDoc(selectedItem);
                    break;
                case "ApproveWithComments":
                case "Reject":
                case "Await":
                    CWCommentsDialogBox commentsDialog = new CWCommentsDialogBox(Uniconta.ClientTools.Localization.lookup("Comment"), false, DateTime.MinValue);
#if !SILVERLIGHT
                    commentsDialog.DialogTableId = 2000000068;
#endif
                    commentsDialog.Closing += async delegate
                    {
                        if (commentsDialog.DialogResult == true)
                        {
                            var result = ErrorCodes.NoSucces;
                            var docApi = new DocumentAPI(api);
                            if(ActionType == "ApproveWithComments")
                                result = await docApi.DocumentSetApprove(selectedItem, commentsDialog.Comments, employee);
                            if (ActionType == "Reject")
                                result = await docApi.DocumentSetReject(selectedItem, commentsDialog.Comments, employee);
                            if (ActionType == "Await")
                                result = await docApi.DocumentSetWait(selectedItem, commentsDialog.Comments, employee);
                            if (result == ErrorCodes.Succes)
                                RemoveRow(selectedItem);
                            else
                                UtilDisplay.ShowErrorCode(result);
                        }
                    };
                    commentsDialog.Show();
                    break;
                case "ViewVoucher":
                    if (selectedItem != null)
                        ViewVoucher(TabControls.VouchersPage3, dgVoucherApproveGrid.syncEntity);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void ApproveDoc(VouchersClient selectedItem)
        {
            var docApi = new DocumentAPI(api);
            var  result = await docApi.DocumentSetApprove(selectedItem, null, employee);
            if (result == ErrorCodes.Succes)
                RemoveRow(selectedItem);
            else
                UtilDisplay.ShowErrorCode(result);
        }

        void RemoveRow(VouchersClient selectedItem)
        {
            int selectedRowHandle = dgVoucherApproveGrid.GetSelectedRowHandles()[0];
            dgVoucherApproveGrid.tableView.DeleteRow(selectedRowHandle);
        }
    }
}
