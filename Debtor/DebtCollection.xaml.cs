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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtCollectionGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorInvoiceClient); } }
        public override IComparer GridSorting { get { return new DCInvoiceSort(); } }
    }

    /// <summary>
    /// Interaction logic for DebtCollection.xaml
    /// </summary>
    public partial class DebtCollection : GridBasePage
    {
        public DebtCollection(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        public DebtCollection(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            InitPage();
        }

        public DebtCollection(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            dgDebtCollectionGrid.UpdateMaster(master);
            SetUserFields();
            SetRibbonControl(localMenu, dgDebtCollectionGrid);
            dgDebtCollectionGrid.api = api;
            dgDebtCollectionGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgDebtCollectionGrid.ShowTotalSummary();
            var company = api.CompanyEntity;

        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            
        }

        private void SetUserFields()
        {
            throw new NotImplementedException();
        }

        protected override void OnLayoutLoaded()
        {
            
        }


    }
}
