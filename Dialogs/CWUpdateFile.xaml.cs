using System;
using System.Windows;
using System.Windows.Input;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.Common;

namespace UnicontaClient.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CWUpdateFile.xaml
    /// </summary>
    public partial class CWUpdateFile : ChildWindow
    {
        public byte[] Contents;
        public string Url;
        public FileextensionsTypes fileExtensionType;
        public bool Compress;

        public CWUpdateFile(FileextensionsTypes fileExtension, string title = null)
        {
            InitializeComponent();

            Title = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Modify"), Uniconta.ClientTools.Localization.lookup("FloatWindow"));

            if (!string.IsNullOrEmpty(title))
                txtFileType.Text = string.Format("{0} {1}:", Uniconta.ClientTools.Localization.lookup("Upload"), title);
            else
                txtFileType.Text = Uniconta.ClientTools.Localization.lookup("Upload") + ":";

            fileExtensionType = fileExtension;
            fileBrowseCtrl.CompressVisibility = Visibility.Visible;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (fileBrowseCtrl.FileBytes != null)
            {
                Contents = fileBrowseCtrl.FileBytes;
                Compress = fileBrowseCtrl.Compress;
                Url = browseUrl;
                fileExtensionType = DocumentConvert.GetDocumentType(fileBrowseCtrl.FileExtension);

                if (chkIncludeOnlyReference.IsChecked.GetValueOrDefault())
                    Contents = null;
                else
                    Url = null;

                SetDialogResult(true);
            }
            else if (!string.IsNullOrEmpty(txtUrl.Text))
            {
                var url = txtUrl.Text;
                FileextensionsTypes fileExt = FileextensionsTypes.UNK; 
                string fileName = null;

                if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase) || url.StartsWith("www", StringComparison.OrdinalIgnoreCase))
                {
                    int indexOfExtention = url.LastIndexOf('.');
                    var ext = DocumentConvert.GetDocumentType(url.Substring(indexOfExtention, url.Length - indexOfExtention));
                    fileExt = ext != FileextensionsTypes.UNK ? ext : FileextensionsTypes.WWW;
                }
                else if (!Utilities.Utility.TryParseUrl(url, false, ref fileName, ref fileExt))
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("Invalid"), Uniconta.ClientTools.Localization.lookup("Error"));
                    return;
                }

                if (fileExt != fileExtensionType)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("Invalid"), Uniconta.ClientTools.Localization.lookup("Error"));
                    return;
                }

                fileExtensionType = fileExt;
                Url = url;
                SetDialogResult(true);
            }
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("Invalid"), Uniconta.ClientTools.Localization.lookup("Error"));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
               if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }

        private void chkIncludeOnlyReference_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (chkIncludeOnlyReference.IsChecked == true && string.IsNullOrEmpty(browseUrl))
                browseUrl = fileBrowseCtrl.FilePath;
        }

        string browseUrl;
        private void fileBrowseCtrl_FileSelected()
        {
            if (chkIncludeOnlyReference.IsChecked == true)
                browseUrl = fileBrowseCtrl.FilePath;
        }
    }
}
