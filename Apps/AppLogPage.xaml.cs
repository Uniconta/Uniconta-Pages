using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using System.Collections;
using Uniconta.API.Service;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class AppLogGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(AppLogClient); } }
        public override bool Readonly { get { return true; } }
        public override bool IsAutoSave => false;
        public override IComparer GridSorting { get { return new AppLogSort(); } }
    }

    public partial class AppLogPage : GridBasePage
    {
        public override string NameOfControl => TabControls.AppLogPage;

        AppClient masterApp;

        public AppLogPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        public AppLogPage(AppClient master)
            : base(master)
        {
            masterApp = master;
            InitPage(master);
        }

        void InitPage(AppClient master = null)
        {
            InitializeComponent();
            dgGrid.api = api;
            dgGrid.UpdateMaster(master);
            dgGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        //public async override Task InitQuery()
        //{
        //    if (masterApp != null)
        //        dgGrid.ItemsSource = await api.Query<AppLogClient>(new List<Unicontaba>)
        //    else
        //        dgGrid.ItemsSource = await api.Query<AppLogClient>();

        //    dgGrid.Visibility = Visibility.Visible;
        //}

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();

            var hasMaster = masterApp != null;
            AppId.Visible = !hasMaster;
            AppName.Visible = !hasMaster;

            CompanyId.Visible = hasMaster;
            CompanyName.Visible = hasMaster;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
