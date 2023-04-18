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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Page;
using Uniconta.Common.Utility;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWSetDate : ChildWindow
    {
        public string Text { get; set; }
        public DateTime Date { get; set; }
        public int Days { get; set; }
        public CWSetDate(bool setDate = false)
        {
            this.DataContext = this;
            InitializeComponent();
            if (setDate == true)
            {
                this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("SetOBJ"),Uniconta.ClientTools.Localization.lookup("Date"));
                rowText.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
            }
            else
            {
                this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("SetOBJ"), Uniconta.ClientTools.Localization.lookup("Text"));
                rowDate.Height = new GridLength(0);
                rowDays.Height = new GridLength(0);
                double h = this.Height - 60;
                this.Height = h;
            }

            if (Date == DateTime.MinValue)
                Date = BasePage.GetSystemDefaultDate().Date;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Text = teText.Text;
            Date = date.DateTime;
            Days = (int)NumberConvert.ToInt(txtDays.Text);
            SetDialogResult(true);
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SaveButton_Click(sender, e);
            else if (e.Key == Key.Escape)
                CancelButton_Click(sender, e);
        }
    }
}
