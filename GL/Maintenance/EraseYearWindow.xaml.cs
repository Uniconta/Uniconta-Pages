using Uniconta.ClientTools;
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
using Uniconta.DataModel;
using System.Windows;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Maintenance
{
    public partial class EraseYearWindow : ChildWindow
    {
        string text;
        string ConfirmWord;

        public EraseYearWindow(string deleteName, bool useStartDialog)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("Confirmation");
            lblHeader.Text = deleteName;

            string dialog;
            if (useStartDialog)
            {
                ConfirmWord = Uniconta.ClientTools.Localization.lookup("Start");
                dialog = "TypeToStart";
            }
            else
            {
                ConfirmWord = Uniconta.ClientTools.Localization.lookup("Delete");
                dialog = "TypeDelete";
            }
            lblprompt.Text = string.Format(Uniconta.ClientTools.Localization.lookup(dialog), ConfirmWord);
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { txtComment.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
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
            this.DialogResult = (string.Compare(txtComment.Text, ConfirmWord, StringComparison.CurrentCultureIgnoreCase) == 0);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}

