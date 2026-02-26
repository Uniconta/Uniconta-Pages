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
using Uniconta.API.Crm;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls;
using UnicontaClient.Utilities;
using static DevExpress.DataProcessing.InMemoryDataProcessor.AddSurrogateOperationAlgorithm;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class CwPostAdjustments : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrJournal))]
        [InputFieldData]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        public string Journal { get { return _Journal; } set { _Journal = value; } }

        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime AdjustmentDate { get { return _AdjustmentDate; } set { _AdjustmentDate = value; } }

        [InputFieldData]
        [Display(Name = "AdjustmentCategory", ResourceType = typeof(InputFieldDataText))]
        public string AdjustmentCategory { get { return _AdjustmentCategory; } set { _AdjustmentCategory = value; } }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        [Display(Name = "Employee", ResourceType = typeof(InputFieldDataText))]
        public string Employee { get { return _Employee; } set { _Employee = value; } }

        [InputFieldData]
        [Display(Name = "Text", ResourceType = typeof(InputFieldDataText))]
        public string Comment { get { return _Comment; } set { _Comment = value; } }

        [InputFieldData]
        [Display(Name = "Simulation", ResourceType = typeof(InputFieldDataText))]
        public bool IsSimulation { get { return simulation; } set { simulation = value; } }

        bool simulation;
        static string _Journal, _AdjustmentCategory, _Comment, _Employee;
        static DateTime _AdjustmentDate;


        protected override int DialogId { get { return 2000000980; } }
        protected override bool ShowTableValueButton { get { return true; } }

        CrudAPI api;
        SQLTableCache<Uniconta.DataModel.PrCategory> categories;

        public CwPostAdjustments(CrudAPI crudApi)
        {
            this.DataContext = this;
            InitializeComponent();

            api = crudApi;

            this.Title = Uniconta.ClientTools.Localization.lookup("PostAdjustments");
            this.SizeToContent = SizeToContent.Height;

            lookupJournal.api = cmbEmployee.api = cmbRegCategory.api = crudApi;
            this.Loaded += CW_Loaded;

            txtComment.Text = _Comment ?? Uniconta.ClientTools.Localization.lookup("Adjustment");
            dtAdjustmentDate.DateTime = _AdjustmentDate == DateTime.MinValue ? Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate() : _AdjustmentDate;

            categories = crudApi.GetCache<Uniconta.DataModel.PrCategory>();

            SetItemSource(api);
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { lookupJournal.Focus(); }));
        }


        async void SetItemSource(QueryAPI api)
        {
            if (categories == null)
                categories = await api.LoadCache<Uniconta.DataModel.PrCategory>();

            if (categories != null)
            {
                var catlst = categories.Where(s => s._CatType == CategoryType.Adjustment).ToList();
                cmbRegCategory.ItemsSource = catlst;

                var category = catlst.FirstOrDefault(s => s._Default) ?? catlst.FirstOrDefault();
                cmbRegCategory.SelectedItem = category;
                AdjustmentCategory = category._Number;
            }
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
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Journal))
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("Journal"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            if (string.IsNullOrEmpty(AdjustmentCategory))
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("AdjustmentCategory"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

    }
}
