using System;
using System.ComponentModel;
using System.Net;
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

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
                SetDialogResult(false);
        }
    }
}
