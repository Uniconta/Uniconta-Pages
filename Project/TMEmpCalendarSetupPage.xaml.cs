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
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TMEmpCalendarSetupPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TMEmpCalendarSetupClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class TMEmpCalendarSetupPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.TMEmpCalendarSetupPage; } }
        public TMEmpCalendarSetupPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage(null);
        }

        public TMEmpCalendarSetupPage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            dgTMEmpCalendarSetupPage.UpdateMaster(master);
            localMenu.dataGrid = dgTMEmpCalendarSetupPage;
            SetRibbonControl(localMenu, dgTMEmpCalendarSetupPage);
            dgTMEmpCalendarSetupPage.api = api;
            dgTMEmpCalendarSetupPage.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgTMEmpCalendarSetupPage.ShowTotalSummary();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgTMEmpCalendarSetupPage.SelectedItem as TMEmpCalendarSetupClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgTMEmpCalendarSetupPage.AddRow();
                    break;
                case "CopyRow":
                    dgTMEmpCalendarSetupPage.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgTMEmpCalendarSetupPage.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override Task<ErrorCodes> saveGrid()
        {
            if (!BlankFieldExist())
            {
                var t = base.saveGrid();
                return t;
            }
            else
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("CalenderAndEmployee")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                return null;
            }
        }

        private bool BlankFieldExist()
        {
            foreach (var row in dgTMEmpCalendarSetupPage.GetVisibleRows())
            {
                var item = row as TMEmpCalendarSetupClient;
                if (item != null &&( (item._Calendar == null || item._Employee == null) || (item._Calendar == null && item._Employee == null)))
                    return true;
            }
            return false;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.TMEmpCalendar) });
        }
    }
}
