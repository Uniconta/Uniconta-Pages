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
using System.Xml;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWPreviewTextViewer : ChildWindow
    {
        public CWPreviewTextViewer(string text, string title = null)
        {
            InitializeComponent();
            this.DataContext = this;
            Title = title ?? Uniconta.ClientTools.Localization.lookup("Text");
            this.Loaded += CW_Loaded;
            txtContents.Text = text;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { btnClose.Focus(); }));
        }
    }
}
