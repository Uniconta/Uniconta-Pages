using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.Common;

namespace UnicontaClient.Pages
{
    /// <summary>
    /// Interaction logic for CWPreviewXMLViewer.xaml
    /// </summary>
    public partial class CWPreviewXMLViewer : ChildWindow
    {
        private int currentIndex = -1;
        private XMLContentTuple[] _dataContent;

        public CWPreviewXMLViewer(string xmlData, string title, bool canView)
        {
            InitializeComponent();
            this.DataContext = this;

            Title = title ?? Uniconta.ClientTools.Localization.lookup("Viewer");
            _dataContent = UtilDisplay.GetXMLAttachments(xmlData);
            currentIndex = 0;
            if (canView)
                View();
            else
                LoadDefaultView();
        }

        public CWPreviewXMLViewer(string xmlData, string title = null) : this(xmlData, title, true)
        {

        }

        private void LoadDefaultView()
        {
            contentViewerGrid.Children.Add(UtilDisplay.LoadDefaultControl(Uniconta.ClientTools.Localization.lookup("RecordTooLarge")));
        }
        private void UpdateCounters()
        {
            if (_dataContent.Length > 1)
            {
                prevNextPanel.Visibility = Visibility.Visible;
                totalBlk.Text = _dataContent.Length.ToString();
                currentBlk.Text = (currentIndex + 1).ToString();
            }
            else
                prevNextPanel.Visibility = Visibility.Collapsed;

        }

        private void DisplayContent()
        {
            XMLContentTuple _data = _dataContent[currentIndex];
            DisposeChildren();

            switch (_data.FileExtensionType)
            {
                case FileextensionsTypes.XML:
                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml((string)_data.Data);

                    var html = UtilDisplay.GetHTMLStringFromXml(xmlDocument);
                    if (html != null)
                        contentViewerGrid.Children.Add(UtilDisplay.LoadWebControl(html));
                    else
                    {
                        var webViewer = new UnicontaWebViewer((string)_data.Data, true);
                        contentViewerGrid.Children.Add(webViewer);
                    }
                    break;
                case FileextensionsTypes.WWW:
                    contentViewerGrid.Children.Add(UtilDisplay.LoadWebControl((string)_data.Data));
                    break;
                default:
                    contentViewerGrid.Children.Add(_data.Data != null ? UtilDisplay.LoadControl((byte[])_data.Data, _data.FileExtensionType, true, true) :
                        UtilDisplay.LoadDefaultControl(_data.Text));
                    break;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { btnClose.Focus(); }));
        }

        private void ViewProgram_Click(object sender, RoutedEventArgs e)
        {
            if (_dataContent == null || _dataContent[currentIndex] == null)
                return;

            string fileName = null;

            var ext = _dataContent[currentIndex].FileExtensionType;

            if (ext == FileextensionsTypes.WWW)
                fileName = (string)_dataContent[currentIndex].Data;
            else
            {
                byte[] dataAsBytes;
                if (ext == FileextensionsTypes.XML)
                    dataAsBytes = Encoding.UTF8.GetBytes((string)_dataContent[currentIndex].Data);
                else
                    dataAsBytes = (byte[])_dataContent[currentIndex].Data;

                if (dataAsBytes?.Length > 0)
                {
                    fileName = Path.GetTempFileName() + "." + ext.ToString().ToLower();
                    using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        fs.Write(dataAsBytes, 0, dataAsBytes.Length);
                        fs.Flush();
                        fs.Close();
                    }
                }

            }

            if (string.IsNullOrEmpty(fileName))
                return;
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
        }

        private void fileSave_Click(object sender, RoutedEventArgs e)
        {
            if (_dataContent == null || _dataContent[currentIndex] == null)
                return;

            var ext = _dataContent[currentIndex].FileExtensionType;

            if (ext != FileextensionsTypes.WWW)
            {
                if (ext == FileextensionsTypes.XML)
                    UtilDisplay.SaveData((string)_dataContent[currentIndex].Data, ext);
                else
                    UtilDisplay.SaveData((byte[])_dataContent[currentIndex].Data, ext);
            }

        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            --currentIndex;
            View();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            currentIndex++;
            View();
        }

        private void View()
        {
            if (_dataContent == null && _dataContent.Length == 0)
                return;

            if (currentIndex == 0)
            {
                btnPrev.IsEnabled = false;
                btnNext.IsEnabled = true;
            }
            else if (currentIndex == _dataContent.Length - 1)
            {
                btnPrev.IsEnabled = true;
                btnNext.IsEnabled = false;
            }
            else
            {
                btnPrev.IsEnabled = true;
                btnNext.IsEnabled = true;
            }

            UpdateCounters();
            DisplayContent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            DisposeChildren();
            base.OnClosing(e);
        }

        private void DisposeChildren()
        {
            if (contentViewerGrid.Children.Count == 0)
                return;

            var childControl = contentViewerGrid.Children[0];
            if (childControl != null && childControl is UnicontaWebViewer webViewer)
                webViewer.CloseUnicontaWebViewer();

            contentViewerGrid.Children.Clear();
        }
    }
}
