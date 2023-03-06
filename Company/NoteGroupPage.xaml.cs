using UnicontaClient.Pages;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class NoteGroupDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(NoteGroupClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class NoteGroupPage : GridBasePage
    {
        public NoteGroupPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgNoteGroup;
            dgNoteGroup.api = api;
            dgNoteGroup.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgNoteGroup);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgNoteGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgNoteGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgNoteGroup.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
