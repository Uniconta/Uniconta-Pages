using Uniconta.API.System;
using Uniconta.ClientTools;
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

namespace UnicontaClient.Pages
{
    
    public partial class CwInvJournal : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.InvJournal))]
        public string Journal { get; set; }
        static public DateTime Date { get; set; }
        public Uniconta.DataModel.InvJournal InvJournal;
        CrudAPI API;
        bool isDateTime;
        public bool ClearCounted;
        public CwInvJournal(CrudAPI api, bool showDateTime = false)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("GenerateJournalLines");
            API = api;
            lookupJournal.api = api;
            isDateTime = showDateTime;
            if (Date == DateTime.MinValue)
                Date = BasePage.GetSystemDefaultDate().Date;
            
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            if (isDateTime)
            {
                this.txtDate.Visibility =  this.dpDate.Visibility = txtClearCounted.Visibility = 
                chkClearCounted.Visibility = Visibility.Visible;
            }

            Dispatcher.BeginInvoke(new Action(() => { lookupJournal.Focus(); }));
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
            var cache = API.CompanyEntity.GetCache(typeof(Uniconta.DataModel.InvJournal));
            InvJournal = (Uniconta.DataModel.InvJournal)cache?.Get(Journal);
            ClearCounted = chkClearCounted.IsChecked == true;
            if (isDateTime)
                Date = dpDate.DateTime;

            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
