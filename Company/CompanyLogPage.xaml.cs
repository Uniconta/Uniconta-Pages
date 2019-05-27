using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
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
using Uniconta.ClientTools.Page;
using System.Collections;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CompanyLogGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CompanyLogClient); } }
        public override bool Readonly { get { return true; } }
        public override IComparer GridSorting { get { return new LogTimeSort(); } }
    }

    /// <summary>
    /// Interaction logic for CompanyLogPage.xaml
    /// </summary>
    public partial class CompanyLogPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.CompanyLogPage; } }
        public CompanyLogPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            ((TableView)dgCompanyLogGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            dgCompanyLogGrid.api = api;
            dgCompanyLogGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCompanyLogGrid);
            localMenu.OnItemClicked += gridRibbon_BaseActions;
        }
    }
}
