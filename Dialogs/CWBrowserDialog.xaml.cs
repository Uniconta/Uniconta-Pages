using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Security.Policy;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;

namespace UnicontaClient.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CWBrowserDialog.xaml
    /// </summary>
    public partial class CWBrowserDialog : ChildWindow
    {
        Uri source;
        public CWBrowserDialog(string url, string title = null)
        {
            InitializeComponent();
            source = new Uri(url);
            Title = title ?? string.Empty;
            var browserControl = UtilDisplay.LoadWebControl(source);
            layoutGrid.Children.Add(browserControl);
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var childControl = layoutGrid.Children[0];
            if (childControl != null && childControl is UnicontaWebViewer)
            {
                var webViewer = childControl as UnicontaWebViewer;
                webViewer.CloseUnicontaWebViewer();
            }
            base.OnClosing(e);
        }
    }
}
