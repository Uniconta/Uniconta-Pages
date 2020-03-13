using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class GLBudgetBudgetGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLBudgetBudgetClient); } }

        protected override IList ToList(UnicontaBaseEntity[] Arr)
        {
            var lstInvStandardVariantCombiClient = new List<GLBudgetBudgetClient>((GLBudgetBudgetClient[])Arr);
            foreach (GLBudgetBudgetClient objBudgetClient in lstInvStandardVariantCombiClient)
            {
                objBudgetClient.IsEditable = false;
            }
            return lstInvStandardVariantCombiClient;
        }
        public override bool Readonly { get { return false; } }
    }

    public partial class GLBudgetBudgetPage : GridBasePage
    {

        public override string NameOfControl { get { return TabControls.GLBudgetBudgetPage; } }
        List<UnicontaBaseEntity> masterList;
        public GLBudgetBudgetPage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
           
            if (master != null)
                masterList = new List<UnicontaBaseEntity>() { master };
            InitControls();
        }
        private void InitControls()
        {
            localMenu.dataGrid = dgGLBudgetBudget;
            SetRibbonControl(localMenu, dgGLBudgetBudget);
            dgGLBudgetBudget.masterRecords = masterList;
            dgGLBudgetBudget.api = api;
            dgGLBudgetBudget.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked; 
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgGLBudgetBudget.AddRow();
                    break;          
                case "DeleteRow":
                    dgGLBudgetBudget.DeleteRow();
                    break;
                case "SaveGrid":
                    if (ValidateDataForDuplicacy())
                    {
                        var lstStandardVariantCombi = (List<GLBudgetBudgetClient>)dgGLBudgetBudget.ItemsSource;
                        foreach (GLBudgetBudgetClient objCombiClient in lstStandardVariantCombi)// changing the IsEditable property to false so that after saving data user cannot edit
                        {
                            objCombiClient.IsEditable = false;
                        }
                        dgGLBudgetBudget.SaveData();
                    }
                    else
                    {
                        var s = Uniconta.ClientTools.Localization.lookup("DuplicateVariantCombi");
                        UnicontaMessageBox.Show(s, Uniconta.ClientTools.Localization.lookup("Warning"));
                    }
                    break;

            }
        }

        private bool ValidateDataForDuplicacy()
        {
            bool retVal = true;
            var lstGLBudgetBudgetClient = (List<GLBudgetBudgetClient>)dgGLBudgetBudget.ItemsSource;
            var duplicateValues = lstGLBudgetBudgetClient.GroupBy(x => new { Var1 = x.SubBudget }).Where(g => g.Count() > 1).Select(y => y.Key)
              .ToList();

            if (duplicateValues.Count > 0)
            {
                retVal = false;
                int index = 0;
                foreach (var objBudgetBudgetClient in lstGLBudgetBudgetClient)
                {
                    if (objBudgetBudgetClient.SubBudget == duplicateValues[0].Var1 && objBudgetBudgetClient.IsEditable == true)
                        break;
                    index++;
                }
                dgGLBudgetBudget.View.FocusedRowHandle = index;
            }
            else
            {
                retVal = true;
            }
            return retVal;
        }
    }

    public class SubBudgetTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate LookupTemplate { get; set; }


        /// <summary>
        /// Method Returns the Template
        /// </summary>
        /// <param name="item"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var data = item as EditGridCellData;
            var row = data.RowData.Row as GLBudgetBudgetClient;

            if (row?.IsEditable == true)
                return LookupTemplate;
            else
                return TextTemplate;
        }
    }
}
