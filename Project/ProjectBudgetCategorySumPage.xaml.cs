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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectBudgetCategorySumGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectBudgetCategorySumClient); } }
      
        public override IComparer GridSorting { get { return new ProjectBudgetCategorySumClientSort(); } }
    }
    /// <summary>
    /// Interaction logic for ProjectBudgetCategorySumPage.xaml
    /// </summary>
    public partial class ProjectBudgetCategorySumPage : GridBasePage
    {
        ItemBase ibase;
        public ProjectBudgetCategorySumPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgProjectBudgetCategorySum);
            dgProjectBudgetCategorySum.api = api;
            dgProjectBudgetCategorySum.UpdateMaster(master);
            dgProjectBudgetCategorySum.BusyIndicator = busyIndicator;

            var Comp = api.CompanyEntity;
            if (Comp.GetCache(typeof(Uniconta.DataModel.PrCategory)) == null)
                Comp.LoadCache(typeof(Uniconta.DataModel.PrCategory), api);

            dgProjectBudgetCategorySum.ShowTotalSummary();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            GetMenuItem();
        }
        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "GroupByCategoryType");
        }
        bool group = true;
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            if (ActionType == "GroupByCategoryType")
            {
              GroupByCatType(group);
            }
        }

        private void GroupByCatType(bool group)
        {
            if (dgProjectBudgetCategorySum.ItemsSource == null) return;
            if (ibase == null) return;
            if (ibase.Caption == string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), Uniconta.ClientTools.Localization.lookup("Type")) && group)
            {
                CatType.GroupIndex = 1;
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("MenuColumnUnGroup"), Uniconta.ClientTools.Localization.lookup("Type"));
                ibase.LargeGlyph = Utility.GetGlyph("Group_by_close-32x32");
                group = false;
            }
            else
            {
                group = true;
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), Uniconta.ClientTools.Localization.lookup("Type"));
                ibase.LargeGlyph = Utility.GetGlyph("Group_by_32x32");
                CatType.GroupIndex = -1;
                CatType.Visible = true;

            }
        }
    }
}
