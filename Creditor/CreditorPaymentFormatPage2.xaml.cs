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
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtFormat, txtFormat);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            layoutControl = layoutItems;
            cmbPaymentMethod.ItemsSource = Enum.GetValues(typeof(ExportFormatType));
            cmbPaymentGrpg.ItemsSource = AppEnums.PaymentGroupingType.Values;
            leBankAccount.api = crudapi;
            leJournal.api = crudapi;
            if (LoadedRow == null && editrow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow =CreateNew() as CreditorPaymentFormatClient;
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
            switch (editrow.PaymentMethod)
            {
                case ExportFormatType.NETS_Norge:
                    CWNets_NorgePaymentSetup dialogNets_Norg = new CWNets_NorgePaymentSetup(this.api, editrow);
                    dialogNets_Norg.Closing += delegate
                    {
                        if (dialogNets_Norg.DialogResult == true)
                            StreamingManager.Copy(dialogNets_Norg.paymentFormatNet, editrow);
                    };
                    dialogNets_Norg.Show();
                    break;
#if !SILVERLIGHT
                case ExportFormatType.ISO20022_DK:
                    CWISODK_PaymentSetup dialogISODK = new CWISODK_PaymentSetup(this.api, editrow);
                    dialogISODK.Closing += delegate
                    {
                        if (dialogISODK.DialogResult == true)
                            StreamingManager.Copy(dialogISODK.paymentFormatISODK, editrow);
                    };
                    dialogISODK.Show();
                    break;

                case ExportFormatType.ISO20022_NO:
                    CWISONO_PaymentSetup dialogISONO = new CWISONO_PaymentSetup(this.api, editrow);
                    dialogISONO.Closing += delegate
                    {
                        if (dialogISONO.DialogResult == true)
                            StreamingManager.Copy(dialogISONO.paymentFormatISONO, editrow);
                    };
                    dialogISONO.Show();
                    break;

                case ExportFormatType.ISO20022_NL:
                    CWISONL_PaymentSetup dialogISONL = new CWISONL_PaymentSetup(this.api, editrow);
                    dialogISONL.Closing += delegate
                    {
                        if (dialogISONL.DialogResult == true)
                            StreamingManager.Copy(dialogISONL.paymentFormatISONL, editrow);
                    };
                    dialogISONL.Show();
                    break;
                
                case ExportFormatType.ISO20022_DE:
                    CWISODE_PaymentSetup dialogISODE = new CWISODE_PaymentSetup(this.api, editrow);
                    dialogISODE.Closing += delegate
                    {
                        if (dialogISODE.DialogResult == true)
                            StreamingManager.Copy(dialogISODE.paymentFormatISODE, editrow);
                    };
                    dialogISODE.Show();
                    break;

                case ExportFormatType.ISO20022_EE:
                    CWISOEE_PaymentSetup dialogISOEE = new CWISOEE_PaymentSetup(this.api, editrow);
                    dialogISOEE.Closing += delegate
                    {
                        if (dialogISOEE.DialogResult == true)
                            StreamingManager.Copy(dialogISOEE.paymentFormatISOEE, editrow);
                    };
                    dialogISOEE.Show();
                    break;

                case ExportFormatType.ISO20022_SE:
                    CWISOSE_PaymentSetup dialogISOSE = new CWISOSE_PaymentSetup(this.api, editrow);
                    dialogISOSE.Closing += delegate
                    {
                        if (dialogISOSE.DialogResult == true)
                            StreamingManager.Copy(dialogISOSE.paymentFormatISOSE, editrow);
                    };
                    dialogISOSE.Show();
                    break;
                case ExportFormatType.ISO20022_UK:
                    CWISOUK_PaymentSetup dialogISOUK = new CWISOUK_PaymentSetup(this.api, editrow);
                    dialogISOUK.Closing += delegate
                    {
                        if (dialogISOUK.DialogResult == true)
                            StreamingManager.Copy(dialogISOUK.paymentFormatISOUK, editrow);
                    };
                    dialogISOUK.Show();
                    break;
                case ExportFormatType.ISO20022_LT:
                    CWISOLT_PaymentSetup dialogISOLT = new CWISOLT_PaymentSetup(this.api, editrow);
                    dialogISOLT.Closing += delegate
                    {
                        if (dialogISOLT.DialogResult == true)
                            StreamingManager.Copy(dialogISOLT.paymentFormatISOLT, editrow);
                    };
                    dialogISOLT.Show();
                    break;
                case ExportFormatType.ISO20022_CH:
                    CWISOCH_PaymentSetup dialogISOCH = new CWISOCH_PaymentSetup(this.api, editrow);
                    dialogISOCH.Closing += delegate
                    {
                        if (dialogISOCH.DialogResult == true)
                            StreamingManager.Copy(dialogISOCH.paymentFormatISOCH, editrow);
                    };
                    dialogISOCH.Show();
                    break;
#endif
                default:
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoOptions"), Uniconta.ClientTools.Localization.lookup("Information"));
                    break;
            }
        }
    }
}
