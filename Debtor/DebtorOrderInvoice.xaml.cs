using UnicontaClient.Models;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorOrderInvoice : BasePage
    {
        public override string NameOfControl { get { return TabControls.DebtorOrderInvoice; } }
        public DebtorOrderInvoice(UnicontaBaseEntity master)
            : base(master, false)
        {
            //invdt = new Invoicedetails();
            setInvoiceDataContext(master);           
                      
        }
        //Invoicedetails invdt;

        async void setInvoiceDataContext(UnicontaBaseEntity data)
        {
            InitializeComponent();
            //invdt.OrderLines = await api.Query<DebtorOrderLineClient>(new UnicontaBaseEntity[] { data }, null);
            //invdt.ComapnyInfo = DebtorInvoicenumberseries.getCompany(api.CompanyEntity);
            //odrInv.InvoiceDetailsData = invdt;
            //InvoiceListViewModel.InvoiceDetails = invdt;
        }
    }
}
