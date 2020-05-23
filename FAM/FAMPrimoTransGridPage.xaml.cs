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
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class FAMPrimoTransGridClient : FAMTransGridClient
    {
        public override bool Readonly { get { return false; } }
        public override bool SingleBufferUpdate { get { return false; } }
    }
    /// <summary>
    /// Interaction logic for FAMPrimoTransClientGridPage.xaml
    /// </summary>
    public partial class FAMPrimoTransGridPage : GridBasePage
    {
        public override string NameOfControl => TabControls.FAMPrimoTransGridPage;
        public FAMPrimoTransGridPage(CrudAPI baseApi) : base(baseApi, "")
        {
            InitPage();
        }

        public FAMPrimoTransGridPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }

        protected override Filter[] DefaultFilters()
        {
            Filter primoFilter = new Filter() { name = "Primo" };
            primoFilter.parameterType = typeof(int);
            primoFilter.value = "1";
            return new Filter[] { primoFilter };
        }
        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgFAMPrimoTranClient);
            dgFAMPrimoTranClient.api = api;
            if (master != null)
                dgFAMPrimoTranClient.UpdateMaster(master);
            dgFAMPrimoTranClient.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgFAMPrimoTranClient.SelectedItem as FAMTransClient;

            switch (ActionType)
            {
                case "AddRow":
                    dgFAMPrimoTranClient.AddRow();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        dgFAMPrimoTranClient.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgFAMPrimoTranClient.DeleteRow();
                    break;
                case "UndoDelete":
                    dgFAMPrimoTranClient.UndoDeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
