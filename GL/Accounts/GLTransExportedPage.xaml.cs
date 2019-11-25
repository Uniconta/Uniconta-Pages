using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLTransExportedPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransExportedClient); } }
    }

    public partial class GLTransExportedPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLTransExportedPage; } }

        public GLTransExportedPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgGLTransExported;
            SetRibbonControl(localMenu, dgGLTransExported);
            dgGLTransExported.api = api;
            dgGLTransExported.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.GLTransPage)
                dgGLTransExported.UpdateItemSource(argument);
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.GLAccount) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGLTransExported.SelectedItem as GLTransExportedClient;
            switch (ActionType)
            {
                case "NewExport":
                    var today = System.DateTime.Today;
                    var lst = (IEnumerable<Uniconta.DataModel.GLTransExported>)dgGLTransExported.ItemsSource;
                    var maxDate = (lst != null && lst.Count() > 0) ? (from rec in lst select rec._ToDate).Max() : today.AddYears(-1).AddDays(-today.Day);

                    var start = maxDate.AddDays(1);
                    var end = start.AddMonths(1).AddDays(-1);
                    CWInterval winInterval = new CWInterval(start, end);
                    winInterval.Closed += delegate
                    {
                        if (winInterval.DialogResult == true)
                        {
                            var glTransExported = new GLTransExportedClient();
                            glTransExported.SetMaster(api.CompanyEntity);
                            glTransExported._FromDate = winInterval.FromDate;
                            glTransExported._ToDate = winInterval.ToDate;
                            AddDockItem(TabControls.GLTransPage, new object[] { glTransExported, "NewExport" }, Uniconta.ClientTools.Localization.lookup("AccountsTransaction"));
                        }
                    };
                    winInterval.Show();
                    break;
                case "DeleteExport":
                    if (selectedItem != null)
                        DeleteRecord(selectedItem);
                    break;
                case "ShowExport":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLTransPage, new object[] { selectedItem, "ShowExport" }, Uniconta.ClientTools.Localization.lookup("AccountsTransaction"));
                    break;
                case "ShowSupplement":
                    if (selectedItem != null && selectedItem._SuppJournalPostedId != 0)
                        AddDockItem(TabControls.GLTransPage, new object[] { selectedItem, "ShowSupplement" }, Uniconta.ClientTools.Localization.lookup("SupplementaryTransactions"));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void DeleteRecord(GLTransExportedClient selectedItem)
        {
            if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                busyIndicator.IsBusy = true;
                var err = await api.Delete(selectedItem);
                busyIndicator.IsBusy = false;
                if (err != ErrorCodes.Succes)
                    UtilDisplay.ShowErrorCode(err);
                else
                    InitQuery();
            }
        }
    }
}
