using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using UnicontaClient.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ReportLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLReportLineClient); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort
        {
            get
            {
                return false;
            }
        }
        public override bool Readonly { get { return false; } }
    }

    public partial class GLReportLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLReportLine; } }

        public GLReportLinePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            dgGLReportLine.api = api;
            localMenu.dataGrid = dgGLReportLine;
            dgGLReportLine.UpdateMaster(master);
            SetRibbonControl(localMenu, dgGLReportLine);
            dgGLReportLine.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGLReportLine.SelectedItem as GLReportLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgGLReportLine.AddRow();
                    break;
                case "CopyRow":
                    dgGLReportLine.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgGLReportLine.DeleteRow();
                    break;
                case "Script":
                    if (selectedItem == null || !selectedItem._ExpressionSum)
                        return;
                    var cwAddScript = new CWAddScript(api, selectedItem, selectedItem.Accounts);
                    cwAddScript.Closed += delegate
                    {
                        if (cwAddScript.DialogResult == true)
                        {
                            dgGLReportLine.SetLoadedRow(selectedItem);
                            selectedItem.Accounts = cwAddScript.txtScript.Text;
                            dgGLReportLine.SetModifiedRow(selectedItem);
                            var currentCol = dgGLReportLine.CurrentColumn;
                            dgGLReportLine.tableView.PostEditor();
                            dgGLReportLine.CurrentColumn = currentCol;
                            dgGLReportLine.tableView.ShowEditor();
                        }
                    };
                    cwAddScript.Show();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgGLReportLine.Filter(propValuePair);
        }

        public async override Task InitQuery()
        {
            await Filter(null);
            var itemSource = (IList)dgGLReportLine.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgGLReportLine.AddFirstRow();
        }
    }
}
