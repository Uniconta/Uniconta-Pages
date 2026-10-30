using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages.GL.TransactionOrigin.Visuals;
using UnicontaClient.Pages.GL.WhereUsed;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLAccountWhereUsedGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLAccountWhereUsedResult); } }
        public override bool Readonly => true;

    }

    public partial class GLAccountWhereUsedPage : GridBasePage
    {
        private UnicontaBaseEntity editrow;

        public IReadOnlyList<GLAccountWhereUsedResult> Results { get; private set; }

        //public override void OnClosePage(object[] refreshParams) { globalEvents.OnRefresh(NameOfControl, refreshParams); }
        public override string NameOfControl { get { return TabControls.GLAccountWhereUsedPage; } }
        //public override Type TableType { get { return typeof(GLAccountClient); } }
        //public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (GLAccountClient)value; } }

        protected override bool IsLayoutSaveRequired() => false;

        public static Task<IReadOnlyList<GLAccountWhereUsedResult>> FindWhereUsed(CrudAPI api, GLAccountClient account)
        {
            return new GLAccountWhereUsedFinder().FindWhereUsed(api, account);
        }

        public GLAccountWhereUsedPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializePage();
            SetupMaster(syncEntity.Row);
            _ = LoadWhereUsedAsync(editrow);
        }
        
        public override Task InitQuery() { return null; }

        private void InitializePage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgResults;
            SetRibbonControl(localMenu, dgResults);

            dgResults.api = api;
            dgResults.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += Ribbon_OnItemClicked;

            dgResults.Visibility = Visibility.Visible;
        }

        private async Task LoadWhereUsedAsync(UnicontaBaseEntity entity)
        {
            if (entity == null)
            {
                SetResults(null);
                return;
            }

            IReadOnlyList<GLAccountWhereUsedResult> results;
            if (entity is GLAccountClient)
                results = await FindWhereUsed(api, (GLAccountClient)entity).ConfigureAwait(true);
            else if (entity is GLVatClient)
                results = await new GLVatWhereUsedFinder().FindWhereUsed(api, (GLVatClient)entity).ConfigureAwait(true);
            else
                results = Enumerable.Empty<GLAccountWhereUsedResult>().ToList();

            if (ReferenceEquals(editrow, entity))
                SetResults(results);
        }

        private void SetupMaster(UnicontaBaseEntity entity)
        {
            editrow = entity;
            SetHeader(entity is GLVatClient ? GetDockHeader((GLVatClient)entity) : GetDockHeader(entity as GLAccountClient));
            SetResults(null);
        }

        private void SetResults(IEnumerable<GLAccountWhereUsedResult> results)
        {
            Results = (results ?? Enumerable.Empty<GLAccountWhereUsedResult>()).ToList();
            dgResults.ItemsSource = Results;
        }

        private void SourceTableKey_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var row = GetSourceRow(sender as FrameworkElement);
            if (row == null
            ||  row.SourceTable == null
            ||  string.IsNullOrWhiteSpace(row.SourceTableKey))
                return;

            PostingTraceDiagramView.JumpToMainTable(new LookUpTable
            {
                TableType = row.SourceTable,
                KeyStr = row.SourceTableKey
            }, title: GetSourceTitle(row));

            e.Handled = true;
        }


        private GLAccountWhereUsedResult GetSourceRow(FrameworkElement element)
        {
            if (element == null)
                return null;

            var cellData = element.DataContext as DevExpress.Xpf.Grid.GridCellData;
            if (cellData != null)
                return cellData.RowData.Row as GLAccountWhereUsedResult;

            return dgResults.SelectedItem as GLAccountWhereUsedResult;
        }

        private static string GetSourceTitle(GLAccountWhereUsedResult row)
        {
            if (row == null)
                return string.Empty;

            if (string.IsNullOrWhiteSpace(row.SourceTableName))
                return row.SourceTableKey ?? string.Empty;

            return string.IsNullOrWhiteSpace(row.SourceTableKey)
                ? row.SourceTableName
                : string.Format("{0}: {1}", row.SourceTableName, row.SourceTableKey);
        }

        internal static string GetDockHeader(GLAccountClient account)
        {
            if (account == null)
                return Uniconta.ClientTools.Localization.lookup("WhereUsed");

            return string.IsNullOrWhiteSpace(account.Name)
                ? string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("WhereUsed"), account.Account)
                : string.Format("{0}: {1} - {2}", Uniconta.ClientTools.Localization.lookup("WhereUsed"), account.Account, account.Name);
        }

        internal static string GetDockHeader(GLVatClient vat)
        {
            if (vat == null || string.IsNullOrWhiteSpace(vat.Vat))
                return Uniconta.ClientTools.Localization.lookup("WhereUsed");

            return string.IsNullOrWhiteSpace(vat.Name)
                ? string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("WhereUsed"), vat.Vat)
                : string.Format("{0}: {1} - {2}", Uniconta.ClientTools.Localization.lookup("WhereUsed"), vat.Vat, vat.Name);
        }

        protected override async void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            SetupMaster(args);
            await LoadWhereUsedAsync(editrow);
        }

        private void Ribbon_OnItemClicked(string action)
        {
            switch (action)
            {
                case "SyncPage":
                    InSyncMode = !InSyncMode;
                break;

                default:
                    gridRibbon_BaseActions(action);
                break;
            }
        }
    }
}
