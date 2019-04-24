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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
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
    /// Interaction logic for CWGeneratePickingList.xaml
    /// </summary>
    public partial class CWGeneratePickingList : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime SelectedDate { get; set; }

        [InputFieldData]
        [Display(Name = "Email", ResourceType = typeof(InputFieldDataText))]
        public string EmailList { get; set; }

        [InputFieldData]
        [Display(Name = "PrintImmediately", ResourceType = typeof(InputFieldDataText))]
        public bool PrintDocument { get; set; }

        [InputFieldData]
        [Display(Name = "Preview", ResourceType = typeof(InputFieldDataText))]
        public bool ShowDocument { get; set; }

        [InputFieldData]
        [Display(Name = "NumberOfCopies", ResourceType = typeof(InputFieldDataText))]
        public short NumberOfPages { get; set; } = 1;

#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CWGeneratePickingList(bool showPageCount = true, bool showEmail = true)
        {
            this.DataContext = this;
            InitializeComponent();
#if !SILVERLIGHT
            rdbShowInvoice.IsChecked = true;
            stkPageNumberCount.Visibility = showPageCount ? Visibility.Visible : Visibility.Collapsed;
#endif
            tbShEmail.Visibility = showEmail ? Visibility.Visible : Visibility.Collapsed;
            txtEmail.Visibility = showEmail ? Visibility.Visible : Visibility.Collapsed;

            Title = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Generate"), Uniconta.ClientTools.Localization.lookup("PickingList"));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                DialogResult = false;
            else if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    DialogResult = false;
                    return;
                }
                OKButton_Click(sender, e);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
