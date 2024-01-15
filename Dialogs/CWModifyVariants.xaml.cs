using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWModifyVariants : ChildWindow
    {
        Company company;
        CrudAPI api;
        InvTrans transactionEntity;
        public CWModifyVariants(CrudAPI Api, UnicontaBaseEntity sourcedata)
        {
            this.DataContext = this;
            InitializeComponent();
#if SILVERLIGHT
            Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("EditOBJ"), Uniconta.ClientTools.Localization.lookup("Variant"));
            api = Api;
            transactionEntity = sourcedata as InvTrans;
            company = api.CompanyEntity;
            this.Loaded += CWModifyVariants_Loaded;
        }

        private void CWModifyVariants_Loaded(object sender, RoutedEventArgs e)
        {
#if SILVERLIGHT
            leVariant.Context = transactionEntity;
#else
            leVariant.DataContext = transactionEntity;
#endif        
            Dispatcher.BeginInvoke(new Action(() => { leVariant.Focus(); }));
        }

        async private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var transApi = new Uniconta.API.Inventory.TransactionsAPI(api);
            var variant1 = transactionEntity._Variant1;
            var variant2 = transactionEntity._Variant2;
            var variant3 = transactionEntity._Variant3;
            var variant4 = transactionEntity._Variant4;
            var variant5 = transactionEntity._Variant5;
            var result = await transApi.ChangeVariant(transactionEntity, variant1, variant2, variant3, variant4, variant5);
            if (result == ErrorCodes.Succes)
                SetDialogResult(true);
            else
                UtilDisplay.ShowErrorCode(result);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

#if SILVERLIGHT
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }
#endif
    }
}

