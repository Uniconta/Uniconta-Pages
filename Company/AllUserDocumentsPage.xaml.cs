using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            InitPage(new List<UnicontaBaseEntity>() { rec });
        }

        public AllUserDocumentsPage(List<UnicontaBaseEntity> masters, CrudAPI api) : base(api, string.Empty)
        {
            InitPage(masters);
        }

        public AllUserDocumentsPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        void InitPage(List<UnicontaBaseEntity> rec)
        {
            InitializeComponent();
            dgDocsGrid.BusyIndicator = busyIndicator;
            dgDocsGrid.api = api;
            SetRibbonControl(localMenu, dgDocsGrid);
            dgDocsGrid.masterRecords = rec;
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
                        ViewDocument(dgDocsGrid.syncEntity, header);
                    }
                    break;
                case "Export":
                    if (exporting)
                        return;
                    string tableName = "All";
                    if (dgDocsGrid.masterRecord != null)
                    {
                        var type = dgDocsGrid.masterRecord.GetType();
                        var clientTableAttr = type.GetCustomAttributes(typeof(ClientTableAttribute), true);
                        tableName = type.Name;
                        if (clientTableAttr.Length > 0)
                        {
                            var attr = (ClientTableAttribute)clientTableAttr[0];
                            tableName = Uniconta.ClientTools.Localization.lookup(attr.LabelKey);
                        }
                    }
                    ExportDocs(tableName);
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
                lookup.TableType = doc.RefType;
            return lookup;
        }

        DocumentViewerWindow docViewer;

        void ViewDocument(SynchronizeEntity sourceData, string header = null)
        {
            if (header == null)
                header = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("View"), Uniconta.ClientTools.Localization.lookup("PhysicalVoucher"));
            if (docViewer == null)
            {
                docViewer = new DocumentViewerWindow(api, header);
                docViewer.InitViewer(sourceData);
                docViewer.Owner = System.Windows.Application.Current.Windows.OfType<NavigationWindow>().FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;
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
        bool exporting = false;
        async void ExportDocs(string tableName)
        {
            try
            {
                exporting = true;
                busyIndicator.IsBusy = true;
                var files = new Dictionary<string, byte[]>();
                foreach (var row in dgDocsGrid.GetVisibleRows())
                {
                    if (row is UserDocsClient doc)
                    {
                        await api.Read(doc);
                        var buffer = doc._Data;

                        if (buffer != null && buffer.Length > 0)
                        {
                            string fileName = string.Concat(doc.TableId, "_", doc.TableRowId, "_", doc.RowId, "_", doc.KeyStr ?? "", ".", doc._DocumentType.ToString().ToLower());
                            files[fileName] = buffer;
                        }
                    }
                }
                var sfd = UtilDisplay.LoadSaveFileDialog;
                sfd.FileName = string.Concat(tableName,"_Docs.zip");
                sfd.Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.ZIP);
                if (sfd.ShowDialog() == true)
                {
                    using (var zipStream = new FileStream(sfd.FileName, FileMode.Create))
                    {
                        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                        {
                            foreach (var file in files)
                            {
                                var entry = archive.CreateEntry(file.Key, CompressionLevel.Optimal);
                                using (var entryStream = entry.Open())
                                {
                                    entryStream.Write(file.Value, 0, file.Value.Length);
                                }
                            }
                        }
                    }
                }
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SavedOBJ"), "Zip"), Uniconta.ClientTools.Localization.lookup("Succes"));
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show($"Error: {ex.Message}", Uniconta.ClientTools.Localization.lookup("Error"));
            }
            exporting = false;
            busyIndicator.IsBusy = false;

        }
    }
}
