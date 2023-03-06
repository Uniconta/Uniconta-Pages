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
    public class UserProfileRestrictedMethodDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserProfileRestrictedMethodClientLocal); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class UserProfileRestrictedMethodPage : GridBasePage
    {
        string[] methodNames;
        public UserProfileRestrictedMethodPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgUserPrflRestMthd.UpdateMaster(master);
            localMenu.dataGrid = dgUserPrflRestMthd;
            SetRibbonControl(localMenu, dgUserPrflRestMthd);
            dgUserPrflRestMthd.api = api;
            GetMethodNames();
            dgUserPrflRestMthd.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgUserPrflRestMthd.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
        }

        async void GetMethodNames()
        {
            var serLogApi = new ServerlogAPI(api);
            methodNames = await serLogApi.ServerMethodNames();
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldSelectedItem = e.OldItem as UserProfileRestrictedMethodClientLocal;
            if (oldSelectedItem != null)
            {
                oldSelectedItem._methodNameSource = null;
                oldSelectedItem.NotifyPropertyChanged("MethodNameSource");
            }

            var selectedItem = e.NewItem as UserProfileRestrictedMethodClientLocal;
            if (selectedItem != null)
            {
                if (methodNames == null)
                    GetMethodNames();
                selectedItem._methodNameSource = methodNames;
                selectedItem.NotifyPropertyChanged("MethodNameSource");
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserPrflRestMthd.SelectedItem as UserProfileRestrictedMethodClientLocal;
            switch (ActionType)
            {
                case "AddRow":
                    dgUserPrflRestMthd.AddRow();
                    break;
                case "SaveGrid":
                    dgUserPrflRestMthd.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgUserPrflRestMthd.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }

    public class UserProfileRestrictedMethodClientLocal : UserProfileRestrictedMethodClient
    {
        internal string[] _methodNameSource;

        public string[] MethodNameSource { get { return _methodNameSource; } }
    }
}
