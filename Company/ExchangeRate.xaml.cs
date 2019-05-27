using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
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
using Uniconta.ClientTools.Controls;
using DevExpress.Xpf.Editors.Settings;
using Uniconta.Common.User;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ExchangeRateGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ExchangeRateClient); } }
        public override IComparer GridSorting { get { return new ExchangeSort(); } }

        public override bool Readonly { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (ExchangeRateClient)this.SelectedItem;
            if (selectedItem == null)
                return false;
            return true;
        }
        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            if (dataEntity == null)
                return;
            var ex = (ExchangeRateClient)dataEntity;
            ex.Date = BasePage.GetSystemDefaultDate();
        }
    }
    
    public partial class ExchangeRate :GridBasePage
    {
        protected override Filter[] DefaultFilters()
        {
            Filter dateFilter = new Filter();
            dateFilter.name = "Date";
            dateFilter.value = String.Format("{0:d}..", BasePage.GetSystemDefaultDate().Date.AddDays(-1));
            return new Filter[] { dateFilter };
        }
        public override string NameOfControl { get { return TabControls.ExchangeRate; } }
        public ExchangeRate(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgExchangeRate;
            SetRibbonControl(localMenu, dgExchangeRate);
            dgExchangeRate.api = api;
            dgExchangeRate.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            var role = BasePage.session.User._Role;
            if (role < (byte)UserRoles.Distributor)
            {
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "AddRow", "CopyRow", "DeleteRow", "SaveGrid" });
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgExchangeRate.AddRow();
                    break;
                case "CopyRow":
                    dgExchangeRate.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgExchangeRate.DeleteRow();
                    break;             
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgExchangeRate.Filter(propValuePair);

        }
       
        public async override Task InitQuery()
        {
			await Filter();
            for (int i = 2; i < 9; i++)
            {
                var source = dgExchangeRate.ItemsSource as IList;
                if (source == null || source.Count == 0)
                {
                    var prop = PropValuePair.GenereteWhereElements("Date", typeof(DateTime), String.Format("{0:d}..", BasePage.GetSystemDefaultDate().Date.AddDays(-i)));
                    await Filter(new List<PropValuePair>() { prop });
                }
                else
                    break;
            }
		}
	}
}


