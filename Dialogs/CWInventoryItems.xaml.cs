using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
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
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools;
using Uniconta.API.System;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWInventoryItems :  ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.InvItem))]
        public string Item { get; set; }
        public Uniconta.DataModel.InvItem InvItem;
        CrudAPI API;
        public CWInventoryItems(CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();

            this.Title = Uniconta.ClientTools.Localization.lookup("Item");
#if SILVERLIGHT
            UnicontaClient.Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            API = api;
            leInvItem.api = api;
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { leInvItem.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var cache = API.CompanyEntity.GetCache(typeof(Uniconta.DataModel.InvItem));
            InvItem = (Uniconta.DataModel.InvItem)cache?.Get(Item);
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
