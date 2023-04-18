using System.Windows;
using System.Windows.Input;
using Uniconta.ClientTools;
using DevExpress.XtraRichEdit.Services;
using System.Windows.Controls;
using Uniconta.ClientTools.Controls;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWGenerateClassType : ChildWindow
    {
        public bool GenerateByName;
        public CWGenerateClassType()
        {
            InitializeComponent();
            this.DataContext = this;
            this.Title = Uniconta.ClientTools.Localization.lookup("GenerateClass");
            KeyDown += CWGenerateClass_KeyDown;
            chkClassType.IsChecked = ClassGenerator.generateByName;
        }

        private void CWGenerateClass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CancelButton_Click(null, null);
            else if (e.Key == Key.Enter)
                OKButton_Click(null, null);
        }



        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateByName = chkClassType.IsChecked.Value;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }


}
