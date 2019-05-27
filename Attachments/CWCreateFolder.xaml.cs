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
namespace UnicontaClient.Pages.CustomPage.Attachments
{
    public partial class CWCreateFolder : ChildWindow
    {
        public string FolderName;
        public string ContentType;
        public CWCreateFolder()
        {
            InitializeComponent();
            this.DataContext = this;
            cmbContentTypes.ItemsSource = AppEnums.ContentTypes.Values;
            cmbContentTypes.SelectedIndex = 0;
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("Folder"));
            this.KeyDown += CWCreateFolder_KeyDown;
            this.Loaded += CWCreateFolder_Loaded;
        }

        private void CWCreateFolder_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                if (string.IsNullOrWhiteSpace(txtFolder.Text))
                    txtFolder.Focus();
                else
                    SaveButton.Focus();
            }));
        }

        private void CWCreateFolder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    this.DialogResult = false;
                    return;
                }
                SaveButton_Click(null, null);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FolderName = txtFolder.Text;
            ContentType = Convert.ToString(cmbContentTypes.EditValue);
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void txtFolder_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            SaveButton.IsEnabled = true;
            if (string.IsNullOrWhiteSpace(txtFolder.Text))
            {
                SaveButton.IsEnabled = false;
            }
        }
    }
}

