using Corasau.Client.Models;
using Corasau.Client.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Util;
using System.Windows;

namespace Corasau.Client.Pages
{
    public class SubscriptionCompanyGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(SubscriptionCompanyClient); } }
    }
    public partial class SubscriptionCompanyPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.SubscriptionCompanyPage.ToString(); } }
        protected override bool IsLayoutSaveRequired() { return false; }
        UnicontaBaseEntity master;
        public SubscriptionCompanyPage(UnicontaBaseEntity sourcedata) : base(sourcedata)
        {
            InitializeComponent();
            gridControl = dgSubscriptionCompany;
            dgSubscriptionCompany.api = api;
            dgSubscriptionCompany.UpdateMaster(sourcedata);
            dgSubscriptionCompany.BusyIndicator = busyIndicatorFinanceYearGrid;
            master = sourcedata;
            BindGrid();
            ribbonControl = localMenu;
            localMenu.dataGrid = dgSubscriptionCompany;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgSubscriptionCompany.Filter(propValuePair);
        }
        private void BindGrid()
        {
            var t = Filter(null);
        }
        private async void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgSubscriptionCompany.SelectedItem as SubscriptionCompanyClient;
            switch (ActionType)
            {
                case "AddRow":
                    CWSelectCompany cwSelectCmp = new CWSelectCompany(api);
                    cwSelectCmp.Closing += async delegate
                    {
                        if (cwSelectCmp.DialogResult == true)
                        {
                            var newSubscriptionCompany = new SubscriptionCompanyClient();
                            newSubscriptionCompany.SetMaster(master);
                            newSubscriptionCompany.SetMaster(cwSelectCmp._CompanyObj);
                            var errRes = await api.Insert(newSubscriptionCompany);
                            ShowErrorCode(errRes);
                        }
                    };
                    cwSelectCmp.Show();
                    break;
                case "Delete":
                    if (selectedItem == null)
                        return;
                    if (MessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        var errdelRes = await api.Delete(selectedItem);
                        ShowErrorCode(errdelRes);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        void ShowErrorCode(ErrorCodes errormsg)
        {
            if (errormsg == ErrorCodes.Succes)
                BindGrid();
            else
                UtilDisplay.ShowErrorCode(errormsg);
        }
    }
}
