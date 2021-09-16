using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls.Dialogs;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    
    public partial class DebtorPaymentFormatPage2 : FormBasePage
    {
        DebtorPaymentFormatClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }

        public override Type TableType { get { return typeof(DebtorPaymentFormatClient); } }
        public override string NameOfControl { get { return TabControls.DebtorPaymentFormatPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorPaymentFormatClient)value; } }
        public DebtorPaymentFormatPage2(UnicontaBaseEntity sourcedata, bool isEdit = true)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            if (!isEdit)
            {
                editrow = (DebtorPaymentFormatClient)StreamingManager.Clone(sourcedata);
                IdKey idkey = (IdKey)editrow;
                if (idkey.KeyStr != null)
                    idkey.KeyStr = null;
            }
            InitPage(api);
        }

        public DebtorPaymentFormatPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtFormat, txtFormat);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            layoutControl = layoutItems;
            cmbPaymentMethod.ItemsSource = Enum.GetValues(typeof(DebtorPaymFormatType)); 
            leBankAccount.api = crudapi;
            leJournal.api = crudapi;

            if (LoadedRow == null && editrow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow =CreateNew() as DebtorPaymentFormatClient;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {

            frmRibbon_BaseActions(ActionType);
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

      

            LoadType(new Type[] { typeof(Uniconta.DataModel.GLAccount) });
        }
        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {
            switch (editrow.PaymentMethod)
            {
                case DebtorPaymFormatType.Iceland:
                    CWDebtorPaymentSetupIceland dialogIceland = new CWDebtorPaymentSetupIceland(this.api, editrow);
                    dialogIceland.Closing += delegate
                    {
                        if (dialogIceland.DialogResult == true)
                            StreamingManager.Copy(dialogIceland.paymentFormatIceland, editrow);
                    };
                    dialogIceland.Show();
                    break;
#if !SILVERLIGHT
                case DebtorPaymFormatType.NetsBS:
                    CWDebtorPaymentSetupNets dialogNets = new CWDebtorPaymentSetupNets(this.api, editrow);
                    dialogNets.Closing += delegate
                    {
                        if (dialogNets.DialogResult == true)
                            StreamingManager.Copy(dialogNets.paymentFormatNets, editrow);
                    };
                    dialogNets.Show();
                    break;

                case DebtorPaymFormatType.SEPA:
                    CWDebtorPaymentSetupSEPA dialogSEPA = new CWDebtorPaymentSetupSEPA(this.api, editrow);
                    dialogSEPA.Closing += delegate
                    {
                        if (dialogSEPA.DialogResult == true)
                            StreamingManager.Copy(dialogSEPA.paymentFormatSEPA, editrow);
                    };
                    dialogSEPA.Show();
                    break;
#endif
                default:
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoOptions"), Uniconta.ClientTools.Localization.lookup("Warning"));
                    break;
            }
        }
    }
}
