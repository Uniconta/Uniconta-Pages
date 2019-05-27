using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Controls;

#if !SILVERLIGHT
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
#elif SILVERLIGHT
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
#endif
{
    /// <summary>
    /// Interaction logic for CWCreateOrderFromProject.xaml
    /// </summary>
    public partial class CWCreateOrderFromProject : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Category", ResourceType = typeof(InputFieldDataText))]
        public string InvoiceCategory { get; set; }

        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime GenrateDate { get; set; }

        [InputFieldData]
        [Display(Name = "FromDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime FromDate { get; set; }

        [InputFieldData]
        [Display(Name = "ToDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime ToDate { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrCategory))]
        public string PrCategory { get; set; }

        CrudAPI api;
#if !SILVERLIGHT
        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }
#endif

        public CWCreateOrderFromProject(CrudAPI crudApi, DateTime documentGenrateteDate)
        {
            GenrateDate = documentGenrateteDate;
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("Order"));
            dpDate.DateTime = documentGenrateteDate;
            api = crudApi;
            cmbCategory.api = crudApi;
            Loaded += CWCreateOrderFromProject_Loaded;
            SetItemSource(crudApi);
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
        }

        private void CWCreateOrderFromProject_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }

        async void SetItemSource(QueryAPI api)
        {
            var prCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.PrCategory), api);
            cmbCategory.ItemsSource = new PrCategoryRevenueFilter(prCache);
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    this.DialogResult = false;
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            InvoiceCategory = PrCategory;
            GenrateDate = dpDate.DateTime;
            FromDate = fromDate.DateTime;
            ToDate = toDate.DateTime;
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
