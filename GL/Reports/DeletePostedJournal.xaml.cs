using Uniconta.ClientTools;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DeletePostedJournal : ChildWindow
    {
        public string Comment { get; set; }
        string ConfirmWord;
        public DeletePostedJournal()
        {
            InitializePage();
        }
        public DeletePostedJournal(bool hideComment)
        {            
            InitializePage();
            if (hideComment)
            {
                txtComment.Visibility = Visibility.Collapsed;
                lblComment.Visibility = Visibility.Collapsed;            
            }
        }


        public void InitializePage()
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("Confirmation");
            ConfirmWord = Uniconta.ClientTools.Localization.lookup("Delete");
            lblprompt.Text = string.Format(Uniconta.ClientTools.Localization.lookup("TypeDelete"), Uniconta.ClientTools.Localization.lookup("Delete"));
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Comment = txtComment.Text;
            this.DialogResult = (string.Compare(txtDelete.Text, ConfirmWord, StringComparison.CurrentCultureIgnoreCase) == 0);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

