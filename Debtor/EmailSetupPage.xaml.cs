using Uniconta.API.System;
using Corasau.Client.Models;
using Corasau.Client.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Util;
using System.Windows;
using DevExpress.Xpf.Editors.Controls;
using Uniconta.ClientTools.Controls;

namespace Corasau.Client.Pages
{
    public partial class EmailSetupPage : FormBasePage
    {
        CompanySettingsClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            Utility.OnRefresh(NameOfControl, RefreshParams);         
        }
        public override string NameOfControl { get { return TabControls.EmailSetupPage.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanySettingsClient)value; } }
        CompanyDocumentClient DocClientRow;
        bool IsNew = false;
        public EmailSetupPage()                    
        {
            InitializeComponent();
            layoutControl = layoutItems;
            if (editrow == null)
                editrow = new CompanySettingsClient();
            layoutItems.DataContext = editrow;
            BusyIndicator = busyIndicator;
            ribbonControl = frmRibbon;
            frmRibbon.OnItemClicked+=frmRibbon_OnItemClicked;
            this.Loaded += EmailSetupPage_Loaded;
            BindPage();
        }

        private void EmailSetupPage_Loaded(object sender, RoutedEventArgs e)
        {
            ribbonControl.DisableButtons(new string[] { "Delete" });
        }

        async void BindPage()
        {
            busyIndicator.IsBusy = true;
            var list = await api.Query<CompanySettingsClient>();
            if (list != null && list.Length > 0)
            {
                editrow = list[0];
                bindEmailBody();
                layoutItems.DataContext = editrow;
            }
            else
                IsNew = true;
            
            ClearBusy();
        }

        private async void bindEmailBody()
        {
            var master = new List<UnicontaBaseEntity>();
            master.Add(editrow);
            DocClientRow = new CompanyDocumentClient();
            DocClientRow.UseFor = CompanyDocumentUse.BodyOfEmail;
            await api.Read(DocClientRow);
            if (DocClientRow.DocumentData != null && DocClientRow.DocumentData.Length > 0)
                txtemailbody.Text = System.Text.Encoding.UTF8.GetString(DocClientRow.DocumentData, 0, DocClientRow.DocumentData.Length);
          
        }
        void MoveFocus()
        {
            object element;
#if WPF
            element = FocusManager.GetFocusedElement(Application.Current.Windows[0]);
            if (element is Control)
            {
                var ctrl = element as Control;
                TraversalRequest tReq = new TraversalRequest(FocusNavigationDirection.Down);
                ctrl.MoveFocus(tReq);
            }
#else
                element = FocusManager.GetFocusedElement();
                if (element is SLTextBox)
                {
                    var dp = (element as System.Windows.Controls.TextBox).Tag as DateEditor;
                    if (dp != null)
                        dp.UpdateEditValueSource();
                }
#endif
        }
        private async void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
            {
                MoveFocus();
                ErrorCodes res = ErrorCodes.NoSucces;
                try
                {
                    busyIndicator.IsBusy = true;
                    if (IsNew)
                    {
                        res = await api.Insert(editrow);
                        if (res == ErrorCodes.Succes)
                        {
                            DocClientRow = new CompanyDocumentClient();
                            DocClientRow.DocumentUseFor = CompanyDocumentUse.BodyOfEmail.ToString();                            
                            if (!string.IsNullOrEmpty(txtemailbody.Text))
                                DocClientRow.DocumentData = System.Text.Encoding.UTF8.GetBytes(txtemailbody.Text);
                            res = await api.Insert(DocClientRow);
                        }
                        if (res == ErrorCodes.Succes)
                        {
                            ClearBusy();
                            dockCtrl.CloseDockItem();
                        }
                        else
                            await ShowErrMsg(res);
                    }
                    else
                    {
                        res = await api.Update(editrow);
                        if (res == ErrorCodes.Succes)
                        {
                            DocClientRow.DocumentData = System.Text.Encoding.UTF8.GetBytes(txtemailbody.Text);
                            if (DocClientRow.CompanyId == 0)
                                res = await api.Insert(DocClientRow);
                            else
                                res = await api.Update(DocClientRow);
                        }
                        if (res == ErrorCodes.Succes)
                        {
                            ClearBusy();
                            dockCtrl.CloseDockItem();
                        }
                        else
                            await ShowErrMsg(res);
                    }
                }
                catch (Exception ex)
                {
                    busyIndicator.IsBusy = false;
                    BasePage.session.ReportException(ex, "EmailSetupPage", session.DefaultCompany.RowId);
                    throw ex;
                }
            }
            else
                frmRibbon_BaseActions(ActionType);
        }

        private async System.Threading.Tasks.Task ShowErrMsg(ErrorCodes res)
        {
            
            if (res == ErrorCodes.Exception || res == ErrorCodes.SQLException)
            {
                StandardError[] errors = await api.session.GetErrors();
                ClearBusy();
                UtilDisplay.ShowErrorCode(res, errors);
            }
            else
            {
                ClearBusy();
                UtilDisplay.ShowErrorCode(res);
            }
        }
    }
}

