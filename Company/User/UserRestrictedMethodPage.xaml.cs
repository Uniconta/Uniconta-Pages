using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections;
using System.Linq;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Corasau.Admin.API;
using System.Collections.Generic;
using System.Windows.Input;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserRestrictedMethodDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserRestrictedMethodClientLocal); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class UserRestrictedMethodPage : GridBasePage
    {
        string[] methodNames; 
        public UserRestrictedMethodPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgUserRestMthd.UpdateMaster(master);
            localMenu.dataGrid = dgUserRestMthd;
            SetRibbonControl(localMenu, dgUserRestMthd);
            dgUserRestMthd.api = api;
            GetMethodNames();
            dgUserRestMthd.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgUserRestMthd.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
        }

        async void GetMethodNames()
        {
            var serLogApi = new ServerlogAPI(api);
            methodNames = await serLogApi.ServerMethodNames();
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldSelectedItem = e.OldItem as UserRestrictedMethodClientLocal;
            if (oldSelectedItem != null)
            {
                oldSelectedItem._methodNameSource = null;
                oldSelectedItem.NotifyPropertyChanged("MethodNameSource");
            }

            var selectedItem = e.NewItem as UserRestrictedMethodClientLocal;
            if (selectedItem != null)
            {
                if(methodNames== null)
                    GetMethodNames();
                selectedItem._methodNameSource = methodNames;
                selectedItem.NotifyPropertyChanged("MethodNameSource");
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserRestMthd.SelectedItem as UserRestrictedMethodClientLocal;
            switch (ActionType)
            {
                case "AddRow":
                    dgUserRestMthd.AddRow();
                    break;
                case "SaveGrid":
                    dgUserRestMthd.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgUserRestMthd.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }

    public class UserRestrictedMethodClientLocal : UserRestrictedMethodClient
    {
        internal string [] _methodNameSource;

        public string[] MethodNameSource { get { return _methodNameSource; } }
    }
}
