using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Uniconta.ClientTools.Util;

using Uniconta.API.GeneralLedger;
using System.Text;
using UnicontaClient.Utilities;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class PrTypedGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(PrTypeClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class ProjectTypePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectType; } }
        public ProjectTypePage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public ProjectTypePage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            dgPrTypeGrid.api = api;
            SetRibbonControl(localMenu, dgPrTypeGrid);
            localMenu.dataGrid = dgPrTypeGrid;
            dgPrTypeGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            PrTypeClient selectedItem = dgPrTypeGrid.SelectedItem as PrTypeClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgPrTypeGrid.AddRow();
                    break;
                case "CopyRow":
                    dgPrTypeGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgPrTypeGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }

        }
    }
}
