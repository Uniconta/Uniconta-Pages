using System;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;

namespace UnicontaClient.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CWCreditorDebtor.xaml
    /// </summary>
    public partial class CWCreditorDebtor : ChildWindow
    {
        CrudAPI Api;
        MaintableAPI maintableApi;
        public CWCreditorDebtor(CrudAPI api, bool isCreditorToDebtor)
        {
            InitializeComponent();
            Api = api;
            maintableApi = new MaintableAPI(api);
            Title = Uniconta.ClientTools.Localization.lookup("JoinCreditorWithDebtor");
            if (isCreditorToDebtor)
            {
                mergeToText.Text = Uniconta.ClientTools.Localization.lookup("Creditor");
                mergeWithText.Text = Uniconta.ClientTools.Localization.lookup("Debtor");

            }
            Loaded += CWCreditorDebtor_Loaded;
        }

        async private void CWCreditorDebtor_Loaded(object sender, RoutedEventArgs e)
        {
            busyIndicator.IsBusy = true;
            await SetSourceMergeTo(Api, typeof(Uniconta.DataModel.Creditor));
            await SetSourceMergeWith(Api, typeof(Uniconta.DataModel.Debtor));
            busyIndicator.IsBusy = false;
        }

        async private Task SetSourceMergeTo(CrudAPI api, Type type)
        {
            var Comp = api.CompanyEntity;
            var Cache = Comp.GetCache(type);
            if (Cache == null)
                Cache = await Comp.LoadCache(type, api);
            lookupMergeTo.ItemsSource = Cache;
        }

        async private Task SetSourceMergeWith(CrudAPI api, Type type)
        {
            var Comp = api.CompanyEntity;
            var Cache = Comp.GetCache(type);
            if (Cache == null)
                Cache = await Comp.LoadCache(type, api);
            lookupMergeWith.ItemsSource = Cache;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var creditor = lookupMergeTo.SelectedItem as Uniconta.DataModel.Creditor;
            var debtor = lookupMergeWith.SelectedItem as Uniconta.DataModel.Debtor;

            if (creditor == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("Creditor"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            if (debtor == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("Debtor"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            var confirmLabel = Uniconta.ClientTools.Localization.lookup("Start");
            var cwTypeConfirmationBox = new CWTypeConfirmationBox(string.Format(Uniconta.ClientTools.Localization.lookup("TypeToStart"), confirmLabel), confirmLabel);
            cwTypeConfirmationBox.Closed += async delegate
            {
                if (cwTypeConfirmationBox.DialogResult == true)
                {
                    busyIndicator.IsBusy = true;
                    var result = await maintableApi.MergeCreditorWithDebtor(creditor, debtor);
                    busyIndicator.IsBusy = false;

                    if (result == Uniconta.Common.ErrorCodes.Succes)
                        SetDialogResult(true);

                    UtilDisplay.ShowErrorCode(result);
                }
            };
            cwTypeConfirmationBox.Show();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                OKButton_Click(null, null);
            else if (e.Key == System.Windows.Input.Key.Escape)
                CancelButton_Click(null, null);
        }
    }
}
