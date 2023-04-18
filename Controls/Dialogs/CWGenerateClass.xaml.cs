using System.Windows;
using System.Windows.Input;
using Uniconta.ClientTools;
using DevExpress.XtraRichEdit.Services;
using System.Windows.Controls;
using Uniconta.ClientTools.Controls;

namespace UnicontaClient.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CWGenerateClass.xaml
    /// </summary>
    public partial class CWGenerateClass : ChildWindow
    {
        string _displayString;
        public CWGenerateClass(string formattedString)
        {
            InitializeComponent();
            this.DataContext = this;
            this.Title = Uniconta.ClientTools.Localization.lookup("GenerateClass");
            _displayString = formattedString;
            Loaded += CWGenerateClass_Loaded;
            KeyDown += CWGenerateClass_KeyDown;
        }

        private void CWGenerateClass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CancelButton_Click(null, null);
            else if (e.Key == Key.Enter)
                CopyButton_Click(null, null);
        }

        private void CWGenerateClass_Loaded(object sender, RoutedEventArgs e)
        {
#if !SILVERLIGHT
            //txtEditControl.ReplaceService<ISyntaxHighlightService>(new CustomSyntaxHighlightService(this.txtEditControl));
            txtEditControl.Text = _displayString;
#else
            txtEditControl.Selection.Text = _displayString;
#endif
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            string copyToClipBoard = string.Empty;
#if !SILVERLIGHT
            copyToClipBoard = string.IsNullOrEmpty(txtEditControl.Selection) ? _displayString : txtEditControl.Selection;
#else
            copyToClipBoard = _displayString;

#endif
            Clipboard.SetText(copyToClipBoard);
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }


}
