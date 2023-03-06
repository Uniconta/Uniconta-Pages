using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.Client.Pages;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System.Threading.Tasks;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ProjectPostWIP : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectPostWIP; } }

        public ProjectPostWIP(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitializePage();
        }

        void InitializePage()
        {
            SetRibbonControl(localMenu, dgProjectTransaction);
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            dgProjectTransaction.api = api;
            dgProjectTransaction.BusyIndicator = busyIndicator;
            dgProjectTransaction.ShowTotalSummary();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;

            prCat = api.GetCache(typeof(Uniconta.DataModel.PrCategory));
            if (prCat == null)
                StartLoadCache();
        }

        SQLCache prCat;
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            if (prCat == null)
                prCat = await api.LoadCache(typeof(Uniconta.DataModel.PrCategory)).ConfigureAwait(false);
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            txtAddWIP.Text = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("WIP"));
            txtRemWIP.Text = string.Format(Uniconta.ClientTools.Localization.lookup("RemoveOBJ"), Uniconta.ClientTools.Localization.lookup("WIP"));
            dgProjectTransaction.Readonly = false;
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = false;
            return true;
        }

        public override bool IsDataChaged { get { return false; } }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectTransaction.SelectedItem as ProjectTransClient;
            switch (ActionType)
            {
                case "PostWIP":
                    PostWIP();
                    break;
                case "Delete":
                    if (selectedItem != null)
                        dgProjectTransaction.DeleteRow();
                    break;
                case "Filter":
                    break;
                case "ClearFilter":
                    ribbonControl.SavedFilters = null;
                    ribbonControl.SavedSorter = null;
                    break;
                case "RefreshGrid":
                    LoadData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void PostWIP()
        {
            var lines = dgProjectTransaction.GetVisibleRows() as IEnumerable<ProjectTransClient>;
            if (lines.Count() == 0)
                return;
            var cwPostWIP = new UnicontaClient.Pages.CwPostWIP(api);

            cwPostWIP.Closed += async delegate
            {
                if (cwPostWIP.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var postingApi = new UnicontaAPI.Project.API.PostingAPI(api);
                    var result = await postingApi.PostWIP(lines, (bool)chkAddWip.IsChecked, CwPostWIP.NumberSerie, CwPostWIP.TransType, CwPostWIP.PostingDate, CwPostWIP.Comment,
                        CwPostWIP.Simulate, new GLTransClientTotal());
                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                    var simulatedLines = result.SimulatedTrans;
                    if (result == null)
                        return;
                    if (result.Err != ErrorCodes.Succes)
                        Utility.ShowJournalError(result, dgProjectTransaction);
                    else if (CwPostWIP.Simulate && result.SimulatedTrans != null)
                        AddDockItem(TabControls.SimulatedTransactions, simulatedLines as IEnumerable<GLTransClientTotal>, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                    else
                    {
                        string msg;
                        if (result.JournalPostedlId != 0)
                            msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), result.JournalPostedlId);
                        else
                            msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                        dgProjectTransaction.ItemsSource = null;
                    }
                }
            };
            cwPostWIP.Show();
        }

        private void btnSerach_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        void LoadData()
        {
            var addWIP = (bool)chkAddWip.IsChecked;
            var removeWIP = (bool)chkRemoveWIP.IsChecked;
            if (addWIP == removeWIP)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("BothCannotBeChecked"), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }

            var inputs = new List<PropValuePair>(3)
            {
                PropValuePair.GenereteWhereElements("WIPPosted", typeof(bool), addWIP ? "0" : "1"),
                PropValuePair.GenereteWhereElements("Invoiceable", typeof(bool), "1"),
            };
            if (addWIP)
                inputs.Add(PropValuePair.GenereteWhereElements("Invoiced", typeof(bool), "0"));
            else
            {
                string excludeCat = null;
                foreach (var pr in (IEnumerable<Uniconta.DataModel.PrCategory>)prCat.GetNotNullArray)
                {
                    if (pr._CatType == Uniconta.DataModel.CategoryType.Revenue || pr._CatType == Uniconta.DataModel.CategoryType.OnAccountInvoicing)
                    {
                        if (excludeCat == null)
                            excludeCat = "!" + pr._Number;
                        else
                            excludeCat = excludeCat + ";!" + pr._Number;
                    }
                }
                if (excludeCat != null)
                    inputs.Add(PropValuePair.GenereteWhereElements("PrCategory", typeof(string), excludeCat));
            }

            if (ribbonControl.filterValues != null)
                inputs.AddRange(ribbonControl.filterValues);

            var t = FilterData(inputs, ribbonControl.PropSort);
        }

        private Task FilterData(IEnumerable<PropValuePair> propValuePair, FilterSorter sorter)
        {
            return dgProjectTransaction.Filter(propValuePair, sorter);
        }
    }
}
