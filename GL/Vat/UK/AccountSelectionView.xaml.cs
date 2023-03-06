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
using Uniconta.ClientTools.Page;
using UnicontaClient.Pages.GL.Vat.UK.ViewModels;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class AccountSelectionView : BasePage
    {
        public AccountSelectionView(string vatNo)
        {
            DataContext = new AccountSelectionViewModel(vatNo, api);
            InitializeComponent();
        }
    }
}
