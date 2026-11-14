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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class CreditorPaymentFormatPage2 : FormBasePage
    {
        CreditorPaymentFormatClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }

        public override Type TableType { get { return typeof(CreditorPaymentFormatClient); } }
        public override string NameOfControl { get { return TabControls.CreditorPaymentFormatPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CreditorPaymentFormatClient)value; } }
        public CreditorPaymentFormatPage2(UnicontaBaseEntity sourcedata, bool isEdit = true)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            if (!isEdit)
            {
                editrow = (CreditorPaymentFormatClient)StreamingManager.Clone(sourcedata);
                IdKey idkey = (IdKey)editrow;
                if (idkey.KeyStr != null)
                    idkey.KeyStr = null;
            }
            InitPage(api);
        }

        public CreditorPaymentFormatPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
            FocusManager.SetFocusedElement(txtFormat, txtFormat);
        }

        void InitPage(CrudAPI crudapi)
        {
            layoutControl = layoutItems;
            cmbPaymentGrpg.ItemsSource = AppEnums.PaymentGroupingType.Values;
            leBankAccount.api = crudapi;
            leJournal.api = crudapi;
            if (LoadedRow == null && editrow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow = CreateNew() as CreditorPaymentFormatClient;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.GLAccount));
        }
        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {
            var paymentMethod = (ExportFormatType)editrow._ExportFormat;
            switch (paymentMethod)
            {
                case ExportFormatType.NETS_Norge:
                    CWNets_NorgePaymentSetup dialogNets_Norg = new CWNets_NorgePaymentSetup(this.api, editrow);
                    dialogNets_Norg.Args = new Uniconta.ClientTools.ChildWindowCallerArgs(this, api);
                    dialogNets_Norg.Closing += delegate
                    {
                        if (dialogNets_Norg.DialogResult == true)
                            StreamingManager.Copy(dialogNets_Norg.paymentFormatNet, editrow);
                    };
                    dialogNets_Norg.Show();
                    break;
                case ExportFormatType.ISO20022_DK:
                case ExportFormatType.ISO20022_NO:
                case ExportFormatType.ISO20022_SE:
                case ExportFormatType.ISO20022_NL:
                case ExportFormatType.ISO20022_DE:
                case ExportFormatType.ISO20022_EE:
                case ExportFormatType.ISO20022_UK:
                case ExportFormatType.ISO20022_LT:
                case ExportFormatType.ISO20022_CH:
                    CWISO_PaymentSetup dialogISO = new CWISO_PaymentSetup(this.api, editrow);
                    dialogISO.Args = new Uniconta.ClientTools.ChildWindowCallerArgs(this, api);
                    dialogISO.Closing += delegate
                    {
                        if (dialogISO.DialogResult == true)
                            StreamingManager.Copy(dialogISO.paymentFormatISO, editrow);
                    };
                    dialogISO.Show();
                    break;
                default:
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoOptions"), Uniconta.ClientTools.Localization.lookup("Information"));
                    break;
            }
        }
    }
}
