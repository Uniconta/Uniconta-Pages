using Uniconta.API.System;
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
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwDatevHeaderParams : ChildWindow
    {
        public string Consultant { get; set; }
        public string Client { get; set; }
        string _Path;
        public string Path { get { return _Path; } set { _Path = value; } }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLAccount))]
        public string DefaultAccount { get; set; }
        public string LanguageId { get; set; }
        public DateTime FiscalYearBegin { get; set; }

        public CwDatevHeaderParams(string consultant, string client, string path, string defaultAcc, string language, DateTime fiscalYearBegin, CrudAPI api)
        {
            this.DataContext = this;
            Consultant = consultant;
            Client = client ;
            _Path = path;
            DefaultAccount = defaultAcc;
            LanguageId = language;
            FiscalYearBegin = fiscalYearBegin;
            InitializeComponent();
            leGlAccount.api = api;
            this.Title = Uniconta.ClientTools.Localization.lookup("DatevHeader");
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtConsultant.Focus();
            }));
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
