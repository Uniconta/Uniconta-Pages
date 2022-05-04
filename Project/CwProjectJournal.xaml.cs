using System;
using System.Collections.Generic;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.Common.Utility;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Page;
using UnicontaClient.Controls;
using System.ComponentModel.DataAnnotations;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwProjectJournal : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(ProjectJournalClient))]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        public string ProjectJournal { get; set; }

        public CwProjectJournal( CrudAPI api)
        {
            InitializeComponent();
            this.DataContext = this;
            lejournal.api = api;
            this.Title = Uniconta.ClientTools.Localization.lookup("Journal");
            this.Loaded += CW_Loaded;
        }
        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { lejournal.Focus(); }));
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
