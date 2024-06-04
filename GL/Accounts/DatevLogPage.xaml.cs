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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System.Collections;
using Uniconta.API.Service;
using System.Globalization;
using Uniconta.ClientTools;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DatevLogGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DatevLogClient); } }
        public override bool Readonly { get { return true; } }

        public override IComparer GridSorting { get { return new LogTimeSort(); } }
    }
    /// <summary>
    /// Interaction logic for DatevLogPage.xaml
    /// </summary>
    public partial class DatevLogPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DatevLogPage; } }

        public DatevLogPage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        public DatevLogPage(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgDatevLogGridClient.api = api;
            dgDatevLogGridClient.BusyIndicator = busyIndicator;
            dgDatevLogGridClient.UpdateMaster(master);
            SetRibbonControl(localMenu, dgDatevLogGridClient);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDatevLogGridClient.SelectedItem as DatevLogClient;
            switch (ActionType)
            {
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
