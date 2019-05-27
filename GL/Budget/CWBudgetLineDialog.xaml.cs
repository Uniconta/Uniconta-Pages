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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools;
using Uniconta.API.System;
using UnicontaClient.Utilities;
using Uniconta.Common;
using Uniconta.DataModel;
using System.ComponentModel.DataAnnotations;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class GLBudgetLineClientWithFromToAct: GLBudgetLineClient
    {
        private string _fromAccount;
        private string _toAccount;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        [Display(Name = "FromAccount", ResourceType = typeof(GLBudgetClientText))]
        public string FromAccount
        {
            get { return _fromAccount; }
            set { _fromAccount = value; NotifyPropertyChanged("FromAccount"); }
        }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        [Display(Name = "ToAccount", ResourceType = typeof(GLBudgetClientText))]
        public string ToAccount
        {
            get { return _toAccount; }
            set { _toAccount = value; NotifyPropertyChanged("ToAccount"); }
        }

    }


    /// <summary>
    /// Interaction logic for CWBudgetLineDialog.xaml
    /// </summary>
    public partial class CWBudgetLineDialog : ChildWindow
    {
        public GLBudgetLineClientWithFromToAct editrow;
        public string fromAccount, toAccount;

        public CWBudgetLineDialog(CrudAPI crudapi, DateTime fromDate, DateTime toDate)
        {
            InitializeComponent();
            this.DataContext = this;
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("DefaultValues");
#else
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            editrow = new GLBudgetLineClientWithFromToAct();
            editrow.Date = fromDate;
            editrow.ToDate = toDate;
            layoutItems.DataContext = editrow;
            dim1lookupeditior.api = dim2lookupeditior.api = dim3lookupeditior.api = dim4lookupeditior.api = dim5lookupeditior.api = toAccountlookupeditior.api = fromAccountlookupeditior.api = crudapi;
            Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, dim1lookupeditior, dim2lookupeditior, dim3lookupeditior, dim4lookupeditior, dim5lookupeditior, layoutGroupDimensions);

            this.Loaded += CWBudgetLineDialog_Loaded;
        }

        private void CWBudgetLineDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrWhiteSpace(deDate.Text))
                    deDate.Focus();
                else
                    OKButton.Focus();
            }));
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            fromAccount = fromAccountlookupeditior.Text;
            toAccount = toAccountlookupeditior.Text;

            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
