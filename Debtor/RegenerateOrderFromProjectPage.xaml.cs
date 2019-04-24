using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.API.Project;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class RegenerateOrderFromProjectPageGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectTransClientLocal); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class RegenerateOrderFromProjectPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.RegenerateOrderFromProjectPage; } }

        public RegenerateOrderFromProjectPage(UnicontaBaseEntity master): base(master)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgGenerateOrder);
            dgGenerateOrder.UpdateMaster(master);
            dgGenerateOrder.api = api;
            dgGenerateOrder.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        public override bool IsDataChaged { get { return false; } }
       
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var master = dgGenerateOrder.masterRecords?.First();
            this.ProjectCol.Visible = !(master is Uniconta.DataModel.Project);
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            dgGenerateOrder.Readonly = true;
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "RegenerateOrder":
                    RegenerateOrderFromProjectTrans();
                break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void RegenerateOrderFromProjectTrans()
        {
            var transLst = dgGenerateOrder.GetVisibleRows() as IEnumerable<ProjectTransClientLocal>;
            var excludedTransLst = transLst?.Where(x => x._remove);
            if (excludedTransLst == null || !excludedTransLst.Any())
            {
                UtilDisplay.ShowErrorCode(ErrorCodes.NoLinesFound);
                return;
            }
            busyIndicator.IsBusy = true;
            var invApi = new InvoiceAPI(api);
            var result = await invApi.RegenerateOrderFromProject(dgGenerateOrder.masterRecord as Uniconta.DataModel.DCOrder, excludedTransLst);
            busyIndicator.IsBusy = false;
            UtilDisplay.ShowErrorCode(result);
            if (result == ErrorCodes.Succes)
            {
                globalEvents.OnRefresh(NameOfControl, null);
                dockCtrl.CloseDockItem();
            }
        }
    }

    public class ProjectTransClientLocal : ProjectTransClient
    {
        [Display(Name = "Include", ResourceType = typeof(ProjectTransClientText))]
        internal bool _remove;
        public bool Check { get { return !_remove; } set { _remove = !value; } }
    }
}
