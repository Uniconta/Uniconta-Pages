using System;
using System.Collections;
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
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AllUserDocumentsPageGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(UserDocsClient); }
        }
        public override IComparer GridSorting { get { return new SortUserDocs(); } }

        public override bool Readonly { get { return true; } }
    }

    public partial class AllUserDocumentsPage : GridBasePage
    {
        public AllUserDocumentsPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            dgDocsGrid.BusyIndicator = busyIndicator;
            dgDocsGrid.api = api;
            SetRibbonControl(localMenu, dgDocsGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDocsGrid.SelectedItem as UserDocsClient;
            switch (ActionType)
            {
                case "ViewDownloadRow":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("View"), Uniconta.ClientTools.Localization.lookup("Documents"));
#if !SILVERLIGHT
                        ViewDocument(dgDocsGrid.syncEntity, header);
#else
                        AddDockItem(TabControls.UserDocsPage3, dgDocsGrid.syncEntity, true, header, ";component/Assets/img/View_16x16.png");
#endif
                    }
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

#if !SILVERLIGHT
        DocumentViewerWindow docViewer;

        void ViewDocument(SynchronizeEntity sourceData, string header = null)
        {
            if (header == null)
                header = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("View"), Uniconta.ClientTools.Localization.lookup("PhysicalVoucher"));
            if (docViewer == null)
            {
                docViewer = new DocumentViewerWindow(sourceData as SynchronizeEntity, this.api, header);
                docViewer.Owner = Application.Current.MainWindow;
                docViewer.Closing += delegate { docViewer.Owner = null; };
                docViewer.Closed += delegate { docViewer = null; };
            }
            if (DocumentViewerWindow.lastHeight != 0)
            {
                docViewer.Width = DocumentViewerWindow.lastWidth;
                docViewer.Height = DocumentViewerWindow.lastHeight;
            }
            if (DocumentViewerWindow.isMaximized)
                docViewer.WindowState = WindowState.Maximized;
            docViewer.Show();
        }
#endif
    }
}
