using UnicontaClient.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CreditorLayoutGroupPage2.xaml
    /// </summary>
    public partial class CreditorLayoutGroupPage2 : FormBasePage
    {
        CreditorLayoutGroupClient editRow;
        public CreditorLayoutGroupPage2(CrudAPI crudApi, string dummy) : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtName, txtName);
#endif
        }

        public CreditorLayoutGroupPage2(UnicontaBaseEntity sourcedata, bool isEdit)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            InitPage(api);
        }

        public override string NameOfControl { get { return UnicontaTabs.CreditorLayoutGroupPage2; } }
        public override Type TableType { get { return typeof(CreditorLayoutGroupClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editRow; } set { editRow = (CreditorLayoutGroupClient)value; } }

        private void InitPage(CrudAPI crudApi)
        {
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            SetSource();
            if(editRow==null && LoadedRow==null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editRow = CreateNew() as CreditorLayoutGroupClient;
            }

            layoutItems.DataContext = editRow;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;

            GetCreditorEmailSetup();
        }

        async private void GetCreditorEmailSetup()
        {
            var emailList = await api.Query<Uniconta.DataModel.DebtorEmailSetup>();
            if (emailList != null)
            {
                cmbPrcOrderEmail.ItemsSource = emailList.Where(x => x._EmailType == Uniconta.DataModel.DebtorEmailType.PurchaseOrder).Select(x => x._Name).ToList();
                cmbPrcPacknoteEmail.ItemsSource = emailList.Where(x => x._EmailType == Uniconta.DataModel.DebtorEmailType.PurchasePacknote).Select(x => x._Name).ToList();
                cmbPrcRequisitionEmail.ItemsSource = emailList.Where(x => x._EmailType == Uniconta.DataModel.DebtorEmailType.Requisition).Select(x => x._Name).ToList();
                cmbPrcCreditnoteEmail.ItemsSource =  cmbPrcInvoiceEmail.ItemsSource = emailList.Where(x => x._EmailType == Uniconta.DataModel.DebtorEmailType.PurchaseInvoice).Select(x => x._Name).ToList();
            }
        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        async private void SetSource()
        {
#if !SILVERLIGHT
            cmbPrcOrderRep.ItemsSource = await PrepareSource(typeof(StandardPurchaseOrderClient));
            cmbPrcPackNoteRep.ItemsSource = await PrepareSource(typeof(StandardPurchasePackNoteClient));
            cmbPrcRequisitionRep.ItemsSource = await PrepareSource(typeof(StandardRequisitionClient));
            cmbPrcCreditnoteRep.ItemsSource = cmbPrcInvoiceRep.ItemsSource = await PrepareSource(typeof(StandardPurchaseInvoiceClient));
#endif
        }

        async private Task<string[]> PrepareSource(Type type)
        {
            var instance = Activator.CreateInstance(type) as UnicontaBaseEntity;
            var list = (UserReportDevExpressClient[])await api.Query(instance, null, null);

            return list.Select(p => p.Name).ToArray();
        }

        public override void OnClosePage(object[] refreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, refreshParams);
        }
    }
}
