using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for ViewUrlPage.xaml
    /// </summary>
    public partial class PDFViewerPage : ControlBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.PDFViewerPage; }
        }
        byte[] buffer;
        FileextensionsTypes contentType;
        public PDFViewerPage(byte[] buffer, FileextensionsTypes contentType) : base(null)
        {
            this.buffer = buffer;
            this.contentType = contentType;
            InitializeComponent();
            this.documentViewer.Children.Clear();
            this.documentViewer.Children.Add(UtilDisplay.LoadControl(buffer, contentType, false, false));
        }
        public void LoadBuffer()
        {
            this.documentViewer.Children.Clear();
            this.documentViewer.Children.Add(UtilDisplay.LoadControl(buffer, contentType, false, false));
        }

        private void downloadImage_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var saveDialog = UtilDisplay.LoadSaveFileDialog;
            saveDialog.Filter = UtilFunctions.GetFilteredExtensions(contentType);

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent =Uniconta.ClientTools.Localization.lookup("BusyMessage");
            bool? dialogResult = saveDialog.ShowDialog();
            if (dialogResult == true && buffer != null)
            {
                try
                {
                    using (Stream stream = File.Create(saveDialog.FileName))
                    {
                        stream.Write(buffer, 0, buffer.Length);
                        stream.Flush();
                        stream.Close();
                    }
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    return;
                }
            }
            busyIndicator.IsBusy = false;
        }

        private void cancelWindow_Click(object sender, RoutedEventArgs e)
        {
            this.CloseDockItem();
        }
    }
}
