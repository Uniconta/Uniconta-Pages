using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWInterval : ChildWindow
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int VarianceDays { get; set; }
        public int JournalPostedId { get; set; }

        public CWInterval(DateTime fromdate, DateTime todate, int variantdays = 0, bool isShowVarDays = false, bool showJrPostId = false)
        {
            FromDate = fromdate;
            ToDate = todate;
            VarianceDays = variantdays;
            this.DataContext = this;
            InitializeComponent();
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("Interval");
#endif
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            if (!isShowVarDays)
            {
                RowVarDays.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
                txtVarDays.Visibility = intVarDays.Visibility = Visibility.Collapsed;
            }
            if (!showJrPostId)
            {
                RowJrPostdId.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
                txtJrPostdId.Visibility = intJrPostdId.Visibility = Visibility.Collapsed;
            }

            this.Loaded += CW_Loaded;
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { dpFromDate.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
                OKButton_Click(null, null);
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

