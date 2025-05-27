using UnicontaClient.Utilities;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwSystemAccountType : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLAccount))]
        public string GLAcc { get; set; }

        public Uniconta.DataModel.GLAccount GlAccount;

        SQLCache LedgerCache;
        public CwSystemAccountType(CrudAPI api, string label)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup(label);
            lookupGLAccount.api = api;
            InitCaches(api);
            this.Loaded += CW_Loaded;
        }

        async private void InitCaches(CrudAPI api)
        {
            LedgerCache = await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                lookupGLAccount.Focus();
            }));
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OKButton_Click(sender, e);
            else if (e.Key == Key.Escape)
                CancelButton_Click(sender, e);
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            GlAccount = (GLAccount)LedgerCache?.Get(GLAcc);
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
