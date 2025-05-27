using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.DataModel;
using DevExpress.Xpf.Printing;
using DevExpress.XtraPrinting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InvoiceSubscription : BasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.InvoiceSubscriptionPage.ToString(); }
        }
        public InvoiceSubscription(SubscriptionInvDetails InvDetails) : base(null, false)
        {
            InitializeComponent();

            var report = new UnicontaClient.Controls.CustomReport(InvDetails, true);
            CreateDocument(report);
        }
       
        public InvoiceSubscription(PartnerInvDetails partnerInvDetails) : base(null, false)
        {
            InitializeComponent();

            var customerReport = new CustomReport(partnerInvDetails);
            CreateDocument(customerReport);
        }

        private void CreateDocument(CustomReport report)
        {
            Previewwin.DocumentSource = report;
            if (!string.IsNullOrEmpty(session.User._Printer))
                report.PrinterName = session.User._Printer;
            report.CreateDocument(true);
        }
    }

}
