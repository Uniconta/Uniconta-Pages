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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using UnicontaClient.Models;
using Uniconta.Common;

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
        public AllUserDocumentsPage(UnicontaBaseEntity rec, CrudAPI api) : base(api, string.Empty)
        {
            InitPage(rec);
        }

        public AllUserDocumentsPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        void InitPage(UnicontaBaseEntity rec)
        {
            InitializeComponent();
            dgDocsGrid.BusyIndicator = busyIndicator;
            dgDocsGrid.api = api;
            SetRibbonControl(localMenu, dgDocsGrid);
            dgDocsGrid.UpdateMaster(rec);
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

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var doc = dg.SelectedItem as UserDocsClient;
            if (doc != null && dg.CurrentColumn?.Name == "KeyStr")
            {
                switch (doc._TableId)
                {
                    case Uniconta.DataModel.Debtor.CLASSID://50
                        lookup.TableType = typeof(Uniconta.DataModel.Debtor);
                        break;
                    case Uniconta.DataModel.Creditor.CLASSID://51
                        lookup.TableType = typeof(Uniconta.DataModel.Creditor);
                        break;
                    case Uniconta.DataModel.Subscription.CLASSID://308
                        lookup.TableType = typeof(Uniconta.DataModel.Subscription);
                        break;
                    case Uniconta.DataModel.DebtorOrder.CLASSID://71
                        lookup.TableType = typeof(Uniconta.DataModel.DebtorOrder);
                        break;
                    case Uniconta.DataModel.CreditorOrder.CLASSID://72
                        lookup.TableType = typeof(Uniconta.DataModel.CreditorOrder);
                        break;
                    case Uniconta.DataModel.InvItem.CLASSID://23
                        lookup.TableType = typeof(Uniconta.DataModel.InvItem);
                        break;
                }
            }
            return lookup;
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
