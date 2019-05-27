using UnicontaClient.Pages;
using System;
using System.Collections;
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
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ExchangeRateCompanyGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ExchangeRateCompanyClientLocal); } }
        public override IComparer GridSorting { get { return new ExchangeExchangeRateCompanySort(); } }

        public override bool Readonly { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (ExchangeRateCompanyClient)this.SelectedItem;
            if (selectedItem == null)
                return false;
            return true;
        }
        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            if (dataEntity == null)
                return;
            var ex = (ExchangeRateCompanyClient)dataEntity;
            ex.Date = BasePage.GetSystemDefaultDate();
        }
    }
    public partial class ExchangeRateCompanyPage : GridBasePage
    {
        public ExchangeRateCompanyPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgExchangeRateCompany;
            SetRibbonControl(localMenu, dgExchangeRateCompany);
            dgExchangeRateCompany.api = api;
            dgExchangeRateCompany.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgExchangeRateCompany.AddRow();
                    break;
                case "CopyRow":
                    dgExchangeRateCompany.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgExchangeRateCompany.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }

    public class ExchangeRateCompanyClientLocal : ExchangeRateCompanyClient
    {
        public bool IsEnabled { get { return (this._CCY1 == 0 && this._CCY2==0); } }
    }
}
