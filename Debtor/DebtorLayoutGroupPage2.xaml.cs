using UnicontaClient.Models;
using UnicontaClient.Utilities;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for DebtorLayoutGroupPage2.xaml
    /// </summary>
    public partial class DebtorLayoutGroupPage2 : FormBasePage
    {
        DebtorLayoutGroupClient editrow;
        public DebtorLayoutGroupPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtName, txtName);
#endif
        }

        public DebtorLayoutGroupPage2(UnicontaBaseEntity sourcedata, bool isEdit)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            InitPage(api);

            if(sourcedata == null)
            {
#if !SILVERLIGHT
                FocusManager.SetFocusedElement(txtName, txtName);
#endif
            }
        }
        public override string NameOfControl { get { return TabControls.DebtorLayoutGroupPage2.ToString(); } }
        public override Type TableType { get { return typeof(DebtorLayoutGroupClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorLayoutGroupClient)value; } }
        void InitPage(CrudAPI crudapi)
        {
            txtCreditNote.api = txtInvoice.api = txtOffer.api = txtOrderConfirmation.api = txtPackNote.api = crudapi;
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            SetSource();
            if (editrow == null && LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow =CreateNew() as DebtorLayoutGroupClient;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            GetDebtorEmailSetup();
        }

        async private void SetSource()
        {
#if !SILVERLIGHT
            cmbStRep.ItemsSource = await PrepareSource(typeof(StandardStatementReportClient)); 
            cmbStCurRep.ItemsSource = await PrepareSource(typeof(StandardStatementCurrencyReportClient));
            cmbInvRep.ItemsSource = await PrepareSource(typeof(StandardInvoiceReportClient));
            cmbColRep.ItemsSource = await PrepareSource(typeof(StandardCollectionReportClient));
            cmbIntNoteRep.ItemsSource = await PrepareSource(typeof(StandardInterestNoteReportClient));
            cmbOfferRep.ItemsSource = await PrepareSource(typeof(StandardQuotationReportClient));
            cmbOrdConfRep.ItemsSource = await PrepareSource(typeof(StandardOrderConfirmationClient));
            cmbPckNoteRep.ItemsSource = await PrepareSource(typeof(StandardPackNoteClient));
            cmbCrdNoteRep.ItemsSource = await PrepareSource(typeof(StandardInvoiceReportClient));
            cmbColCurRep.ItemsSource = await PrepareSource(typeof(StandardCollectionCurrencyClient));
            cmbIntNoteCurRep.ItemsSource = await PrepareSource(typeof(StandardInterestNoteCurrencyClient));
            cmbPickingListRep.ItemsSource = await PrepareSource(typeof(StandardSalesPickingListClient));
#endif
        }

        async private Task<string[]> PrepareSource(Type type)
        {
            var instance = Activator.CreateInstance(type) as UnicontaBaseEntity;
            var list = (UserReportDevExpressClient[])await api.Query(instance, null, null);

            return list.Select(p => p.Name).ToArray();
        }

        async void GetDebtorEmailSetup()
        {
            var emailList = await api.Query<Uniconta.DataModel.DebtorEmailSetup>();
            if (emailList == null)
                return;
            cmbInvoiceEmail.ItemsSource = emailList.Where(x => x._EmailType == DebtorEmailType.Invoice).Select(x => x._Name).ToList();
            cmbCreditnoteEmail.ItemsSource = emailList.Where(x => x._EmailType == DebtorEmailType.Invoice).Select(x => x._Name).ToList();
            cmbPacknoteEmail.ItemsSource = emailList.Where(x => x._EmailType == DebtorEmailType.Packnote).Select(x => x._Name).ToList();
            cmbOrderConfirmationEmail.ItemsSource = emailList.Where(x => x._EmailType == DebtorEmailType.OrderConfirmation).Select(x => x._Name).ToList();
            cmbOfferEmail.ItemsSource = emailList.Where(x => x._EmailType == DebtorEmailType.Offer).Select(x => x._Name).ToList();
            cmbStatementEmail.ItemsSource = emailList.Where(x => x._EmailType == DebtorEmailType.AccountStatement).Select(x => x._Name).ToList();
            cmbStatementCurEmail.ItemsSource = emailList.Where(x => x._EmailType == DebtorEmailType.Requisition).Select(x => x._Name).ToList();
            cmbCollectionEmail.ItemsSource = emailList.Where(x => x._EmailType == DebtorEmailType.Collection || x._EmailType == DebtorEmailType.CollectionLetter1 || 
            x._EmailType == DebtorEmailType.CollectionLetter2 || x._EmailType == DebtorEmailType.CollectionLetter3 || x._EmailType == DebtorEmailType.PaymentReminder).Select(x => x._Name).ToList();
            cmbInterestNoteEmail.ItemsSource = emailList.Where(x => x._EmailType == DebtorEmailType.InterestNote).Select(x => x._Name).ToList();
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
        public override void OnClosePage(object[] refreshParams)
        {
           globalEvents.OnRefresh(NameOfControl, refreshParams);
        }
    }
}
