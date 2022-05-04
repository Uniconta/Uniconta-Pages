using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class PaymentTermsPage2 : FormBasePage
    {
        PaymentTermClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.PaymentTermsPage2.ToString(); } }
        public override Type TableType { get { return typeof(PaymentTermClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (PaymentTermClient)value; } }
        public PaymentTermsPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public PaymentTermsPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtPayment, txtPayment);
#endif
        }
        void InitPage(CrudAPI crudapi)
        {
            leAccount.api = leOffsetAccount.api = DebtorAccount.api = crudapi;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow =CreateNew() as PaymentTermClient;
            }
            layoutItems.DataContext = editrow;
            liPostOnDC.Visibility = Visibility.Collapsed;
            SetVisibility();
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        private void ComboBoxEditor_SelectedIndexChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            SetVisibility();
        }

        private void SetVisibility()
        {
            lItemOffsetAccount.Visibility = itemPct1.Visibility = grpDays.Visibility = grpDebtor.Visibility = grpCashDisCount.Visibility =
            grpAccount.Visibility = liPostOnDC.Visibility = Visibility.Visible;
            switch (editrow._PaymentMethod)
            {
                case PaymentMethodTypes.NetCash:
                    lItemOffsetAccount.Visibility =
                    itemPct1.Visibility = grpDays.Visibility = grpDebtor.Visibility = grpCashDisCount.Visibility = Visibility.Collapsed;
                    grpAccount.Visibility = Visibility.Visible;
                    liPostOnDC.Visibility = Visibility.Visible;
                    break;
                case PaymentMethodTypes.PrePayment:
                    itemPct1.Visibility = grpDebtor.Visibility = liPostOnDC.Visibility = Visibility.Collapsed;
                    lItemOffsetAccount.Visibility =
                    grpDays.Visibility = grpAccount.Visibility = grpCashDisCount.Visibility = Visibility.Visible;
                    break;
                case PaymentMethodTypes.Creditcard:
                    lItemOffsetAccount.Visibility = grpDays.Visibility =  liPostOnDC.Visibility = Visibility.Collapsed;
                    grpDebtor.Visibility = grpAccount.Visibility = itemPct1.Visibility = grpCashDisCount.Visibility = Visibility.Visible;
                    break;
                case PaymentMethodTypes.Factoring:
                    grpDays.Visibility =  liPostOnDC.Visibility = Visibility.Collapsed;
                    grpDebtor.Visibility = grpAccount.Visibility = itemPct1.Visibility = grpCashDisCount.Visibility = Visibility.Visible;
                    break;
                case PaymentMethodTypes.EndMonth:
                    grpAccount.Visibility = grpDebtor.Visibility = liPostOnDC.Visibility = Visibility.Collapsed;
                    grpCashDisCount.Visibility = Visibility.Visible;
                    break;
                case PaymentMethodTypes.NetDays:
                    grpDebtor.Visibility = grpAccount.Visibility = liPostOnDC.Visibility = Visibility.Collapsed;
                    grpDays.Visibility = grpCashDisCount.Visibility = Visibility.Visible;
                    break;
                case PaymentMethodTypes.EndWeek:
                    grpDebtor.Visibility = grpAccount.Visibility = liPostOnDC.Visibility = Visibility.Collapsed;
                    grpDays.Visibility = grpCashDisCount.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
