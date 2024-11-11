using System;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class TmJournalLineStartStopPage2 : FormBasePage
    {
        ProjectJournalLineClient editrow;
        SQLCache ProjectCache, JournalCache, payrollCache;
        CompanySettingsClient companySettings;
        bool IsTime;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.TmJournalLineStartStopPage2.ToString(); } }
        public override Type TableType { get { return typeof(ProjectJournalLineClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (ProjectJournalLineClient)value; } }
        public TmJournalLineStartStopPage2(UnicontaBaseEntity sourcedata, bool isEdit = true, bool isTime=true )
            : base(sourcedata, isEdit)
        {
            if (!isEdit)
                editrow = (ProjectJournalLineClient)StreamingManager.Clone(sourcedata);
            IsTime= isTime;
            InitPage(api);
        }

        public TmJournalLineStartStopPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitPage(crudApi);
        }

        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup)
        {
            return false; 
        }

        CrudAPI crudApi;
        void InitPage(CrudAPI _crudapi)
        {
            InitializeComponent();
            crudApi = _crudapi;
            companySettings = new CompanySettingsClient();
            StartLoadCache();
            layoutControl = layoutItems;
            leTask.api= leProject.api = lePayrollCategory.api = lePrCategory.api = leWorkSpace.api = leItem.api = crudApi;
            
            if (LoadedRow == null)
                frmRibbon.DisableButtons("Delete");
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            ProjectCache = crudApi.GetCache(typeof(Uniconta.DataModel.Project)) ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
            JournalCache = crudApi.GetCache(typeof(Uniconta.DataModel.PrJournal)) ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.PrJournal)).ConfigureAwait(false);
            payrollCache = crudApi.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory)) ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory)).ConfigureAwait(false);
            lePayrollCategory.cacheFilter = new EmpPayrollFilter(payrollCache, IsTime);
            await api.Read(companySettings).ConfigureAwait(false);
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save" && editrow.RowId==0)
            {
                TimeFromRounding(editrow);
                editrow.Date= BasePage.GetSystemDefaultDate();
            }
            frmRibbon_BaseActions(ActionType);
        }

        void TimeFromRounding(ProjectJournalLineClient lineClient)
        {
            switch (companySettings._RoundingStart)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    lineClient.TimeFrom = Utility.RoundUp(companySettings._RoundingStart, DateTime.Now);
                    break;
                case 4:
                case 5:
                case 6:
                case 7:
                    lineClient.TimeFrom = Utility.RoundDown(companySettings._RoundingStart, DateTime.Now);
                    break;
            }
        }

        private void leProject_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            setTask(leProject.SelectedItem as ProjectClient);
        }

        private async void leTask_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ProjectCache==null)
                ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project));
            
            if (editrow?._Project != null)
                setTask((ProjectClient)ProjectCache.Get(editrow._Project));
        }

        private void leItem_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var invItem = leItem.SelectedItem as InvItemClient;
            if(invItem!= null)
            {
                editrow.PayrollCategory = invItem._PayrollCategory;
                editrow.PrCategory = invItem._PrCategory;
            }
        }

        private void lePayrollCategory_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var payType = lePayrollCategory.SelectedItem as EmpPayrollCategoryClient;
            if (payType != null)
                editrow.PrCategory = payType._PrCategory;
        }

        async void setTask(ProjectClient project)
        {
            if (project != null)
                editrow.taskSource = project.Tasks ?? await project.LoadTasks(api);
            else
                editrow.taskSource = api.GetCache(typeof(Uniconta.DataModel.ProjectTask));
            editrow.NotifyPropertyChanged("TaskSource");
            leTask.ItemsSource = editrow.TaskSource;
        }

        public class EmpPayrollFilter : SQLCacheFilter
        {
            bool IsTime;
            public EmpPayrollFilter(SQLCache cache, bool isTime) : base(cache)
            {
                IsTime = isTime;
            }
            public override bool IsValid(object rec)
            {
                var pay = ((Uniconta.DataModel.EmpPayrollCategory)rec);
                if (IsTime)
                    return (pay._InternalType != Uniconta.DataModel.InternalType.Mileage);
                else
                    return (pay._InternalType == Uniconta.DataModel.InternalType.Mileage);
            }
        }
    }
}
