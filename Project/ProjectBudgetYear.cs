using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Project Budget Year Sort
    /// </summary>
    public class ProjectBudgetYearSort : IComparer
    {
        public int Compare(object x, object y)
        {
            return Compare((ProjectBudgetYearClient)x, (ProjectBudgetYearClient)y);
        }

        public int Compare(ProjectBudgetYearClient x, ProjectBudgetYearClient y)
        {
            var val = string.Compare(x.Project, y.Project, StringComparison.OrdinalIgnoreCase);

            if (val == 0)
            {
                val = string.Compare(x.Employee, y.Employee, StringComparison.OrdinalIgnoreCase);

                if (val == 0)
                {
                    val = string.Compare(x.PrCategory, y.PrCategory, StringComparison.OrdinalIgnoreCase);

                    if (val == 0)
                    {
                        val = string.Compare(x.PayrollCategory, y.PayrollCategory, StringComparison.OrdinalIgnoreCase);

                        if (val == 0)
                        {
                            val = string.Compare(x.WorkSpace, y.WorkSpace, StringComparison.OrdinalIgnoreCase);

                            if (val == 0)
                                val = string.Compare(x.Task, y.Task, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            return val;
        }
    }

    /// <summary>
    /// Project Budget Year Text
    /// </summary>
    public class ProjectBudgetYearText
    {
        public static string Month1 { get { return Uniconta.ClientTools.Localization.lookup("January"); } }
        public static string Month2 { get { return Uniconta.ClientTools.Localization.lookup("February"); } }
        public static string Month3 { get { return Uniconta.ClientTools.Localization.lookup("March"); } }
        public static string Month4 { get { return Uniconta.ClientTools.Localization.lookup("April"); } }
        public static string Month5 { get { return Uniconta.ClientTools.Localization.lookup("May"); } }
        public static string Month6 { get { return Uniconta.ClientTools.Localization.lookup("June"); } }
        public static string Month7 { get { return Uniconta.ClientTools.Localization.lookup("July"); } }
        public static string Month8 { get { return Uniconta.ClientTools.Localization.lookup("August"); } }
        public static string Month9 { get { return Uniconta.ClientTools.Localization.lookup("September"); } }
        public static string Month10 { get { return Uniconta.ClientTools.Localization.lookup("October"); } }
        public static string Month11 { get { return Uniconta.ClientTools.Localization.lookup("November"); } }
        public static string Month12 { get { return Uniconta.ClientTools.Localization.lookup("December"); } }
        public static string Total { get { return Uniconta.ClientTools.Localization.lookup("Total"); } }
    }

    /// <summary>
    /// Project Budget Year Client
    /// </summary>
    public class ProjectBudgetYearClient : INotifyPropertyChanged
    {
        private double _MonthQty1, _MonthQty2, _MonthQty3, _MonthQty4, _MonthQty5, _MonthQty6, _MonthQty7, _MonthQty8, _MonthQty9, _MonthQty10, _MonthQty11, _MonthQty12;
        //private bool _IsEditable = false;
        private string _Project, _Employee, _PrCategory, _WorkSpace, _Task, _PayrollCategory;
        public readonly int CompanyId;

        // Budget lines this row was loaded from. Used on save to update/delete them, also when key fields have been changed
        internal List<ProjectBudgetLineClient> OriginalLines;

        // Changed since it was loaded, or new. Used to see if there is still something to save after a row is deleted
        internal bool IsModified;

        // Key fields as loaded. The original lines get the new keys during save, so we can not compare against them
        internal readonly string OrgProject, OrgEmployee, OrgPayrollCategory, OrgPrCategory, OrgWorkSpace, OrgTask;

        internal bool IsKeyChanged
        {
            get
            {
                return _Project != OrgProject || _Employee != OrgEmployee || _PayrollCategory != OrgPayrollCategory ||
                    _PrCategory != OrgPrCategory || _WorkSpace != OrgWorkSpace || _Task != OrgTask;
            }
        }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "Project", ResourceType = typeof(ProjectTransClientText))]
        public string Project
        {
            get { return _Project; }
            set
            {
                _Project = value;
                NotifyPropertyChanged(nameof(Project));
                NotifyPropertyChanged(nameof(ProjectName));
                NotifyPropertyChanged(nameof(ProjectRef));
                NotifyPropertyChanged(nameof(ProjectTaskRef));
            }
        }

        [Display(Name = "ProjectName", ResourceType = typeof(ProjectTransClientText))]
        public string ProjectName { get { return ClientHelper.GetName(CompanyId, typeof(Uniconta.DataModel.Project), _Project); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        [Display(Name = "Employee", ResourceType = typeof(ProjectTransClientText))]
        public string Employee 
        { 
            get { return _Employee; }
            set
            {
                _Employee = value;
                NotifyPropertyChanged(nameof(Employee));    
                NotifyPropertyChanged(nameof(EmployeeName));
                NotifyPropertyChanged(nameof(EmployeeRef));
            }
        }

        [Display(Name = "EmployeeName", ResourceType = typeof(ProjectTransClientText))]
        public string EmployeeName { get { return ClientHelper.GetName(CompanyId, typeof(Uniconta.DataModel.Employee), _Employee); } }


        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrCategory))]
        [Display(Name = "PrCategory", ResourceType = typeof(ProjectTransClientText))]
        public string PrCategory
        {
            get { return _PrCategory; }
            set
            {
                _PrCategory = value;
                NotifyPropertyChanged(nameof(PrCategory));
                NotifyPropertyChanged(nameof(PrCategoryName));
                NotifyPropertyChanged(nameof(PrCategoryRef));
            }
        }

        [Display(Name = "CategoryName", ResourceType = typeof(ProjectTransClientText))]
        public string PrCategoryName { get { return ClientHelper.GetName(CompanyId, typeof(Uniconta.DataModel.PrCategory), _PrCategory); } }

        public object PayrollSource { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.EmpPayrollCategory))]
        [Display(Name = "PayrollCategory", ResourceType = typeof(ProjectTransClientText))]
        public string PayrollCategory
        {
            get { return _PayrollCategory; }
            set
            {
                _PayrollCategory = value;
                PrCategory = ((Uniconta.DataModel.EmpPayrollCategory)ClientHelper.GetRef(CompanyId, typeof(Uniconta.DataModel.EmpPayrollCategory), _PayrollCategory))?._PrCategory;
                NotifyPropertyChanged(nameof(PayrollCategory));
                NotifyPropertyChanged(nameof(PayrollCategoryName));
                NotifyPropertyChanged(nameof(PayrollCategoryRef));
            }
        }

        [Display(Name = "PayrollCategoryName", ResourceType = typeof(ProjectTransClientText))]
        public string PayrollCategoryName { get { return ClientHelper.GetName(CompanyId, typeof(Uniconta.DataModel.EmpPayrollCategory), _PayrollCategory); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrWorkSpace))]
        [Display(Name = "WorkSpace", ResourceType = typeof(ProjectTransClientText))]
        public string WorkSpace
        {
            get { return _WorkSpace; }
            set
            {
                _WorkSpace = value;
                NotifyPropertyChanged(nameof(WorkSpace));
                NotifyPropertyChanged(nameof(WorkSpaceRef));
            }
        }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.ProjectTask))]
        [Display(Name = "Task", ResourceType = typeof(ProjectTransClientText))]
        public string Task
        {
            get { return _Task; }
            set
            {
                _Task = value;
                NotifyPropertyChanged(nameof(Task));
                NotifyPropertyChanged(nameof(ProjectTaskRef));
            }
        }

        [Display(Name = "Month1", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty1 { get { return _MonthQty1; } set { _MonthQty1 = value; NotifyPropertyChanged(nameof(MonthQty1)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Month2", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty2 { get { return _MonthQty2; } set { _MonthQty2 = value; NotifyPropertyChanged(nameof(MonthQty2)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Month3", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty3 { get { return _MonthQty3; } set { _MonthQty3 = value; NotifyPropertyChanged(nameof(MonthQty3)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Month4", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty4 { get { return _MonthQty4; } set { _MonthQty4 = value; NotifyPropertyChanged(nameof(MonthQty4)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Month5", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty5 { get { return _MonthQty5; } set { _MonthQty5 = value; NotifyPropertyChanged(nameof(MonthQty5)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Month6", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty6 { get { return _MonthQty6; } set { _MonthQty6 = value; NotifyPropertyChanged(nameof(MonthQty6)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Month7", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty7 { get { return _MonthQty7; } set { _MonthQty7 = value; NotifyPropertyChanged(nameof(MonthQty7)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Month8", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty8 { get { return _MonthQty8; } set { _MonthQty8 = value; NotifyPropertyChanged(nameof(MonthQty8)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Month9", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty9 { get { return _MonthQty9; } set { _MonthQty9 = value; NotifyPropertyChanged(nameof(MonthQty9)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Month10", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty10 { get { return _MonthQty10; } set { _MonthQty10 = value; NotifyPropertyChanged(nameof(MonthQty10)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Month11", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty11 { get { return _MonthQty11; } set { _MonthQty11 = value; NotifyPropertyChanged(nameof(MonthQty11)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Month12", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty12 { get { return _MonthQty12; } set { _MonthQty12 = value; NotifyPropertyChanged(nameof(MonthQty12)); NotifyPropertyChanged(nameof(TotalQty)); } }

        [Display(Name = "Total", ResourceType = typeof(ProjectBudgetYearText))]
        public double TotalQty
        {
            get
            {
                return _MonthQty1 + _MonthQty2 + _MonthQty3 + _MonthQty4 + _MonthQty5 + _MonthQty6 + _MonthQty7 + _MonthQty8 + _MonthQty9 + _MonthQty10 + _MonthQty11 + _MonthQty12;
            }
        }

        // Quantity for period 1..12
        internal double GetMonthQty(int period)
        {
            switch (period)
            {
                case 1: return _MonthQty1;
                case 2: return _MonthQty2;
                case 3: return _MonthQty3;
                case 4: return _MonthQty4;
                case 5: return _MonthQty5;
                case 6: return _MonthQty6;
                case 7: return _MonthQty7;
                case 8: return _MonthQty8;
                case 9: return _MonthQty9;
                case 10: return _MonthQty10;
                case 11: return _MonthQty11;
                case 12: return _MonthQty12;
            }
            return 0d;
        }

        internal void SetMonthQty(int period, double qty)
        {
            switch (period)
            {
                case 1: MonthQty1 = qty; break;
                case 2: MonthQty2 = qty; break;
                case 3: MonthQty3 = qty; break;
                case 4: MonthQty4 = qty; break;
                case 5: MonthQty5 = qty; break;
                case 6: MonthQty6 = qty; break;
                case 7: MonthQty7 = qty; break;
                case 8: MonthQty8 = qty; break;
                case 9: MonthQty9 = qty; break;
                case 10: MonthQty10 = qty; break;
                case 11: MonthQty11 = qty; break;
                case 12: MonthQty12 = qty; break;
            }
        }

        internal bool IsMonthEmpty(int period)
        {
            var qty = GetMonthQty(period);
            return double.IsNaN(qty) || qty == 0d;
        }

        // References, so fields from these tables can be added to the grid (Layout - Add nested property)
        [ReportingAttribute]
        public ProjectClient ProjectRef
        {
            get
            {
                return ClientHelper.GetRefClient<ProjectClient>(CompanyId, typeof(Uniconta.DataModel.Project), _Project);
            }
        }

        [ReportingAttribute]
        public EmployeeClient EmployeeRef
        {
            get
            {
                return ClientHelper.GetRefClient<EmployeeClient>(CompanyId, typeof(Uniconta.DataModel.Employee), _Employee);
            }
        }

        [ReportingAttribute]
        public EmpPayrollCategoryClient PayrollCategoryRef
        {
            get
            {
                return ClientHelper.GetRefClient<EmpPayrollCategoryClient>(CompanyId, typeof(Uniconta.DataModel.EmpPayrollCategory), _PayrollCategory);
            }
        }

        [ReportingAttribute]
        public PrCategoryClient PrCategoryRef
        {
            get
            {
                return ClientHelper.GetRefClient<PrCategoryClient>(CompanyId, typeof(Uniconta.DataModel.PrCategory), _PrCategory);
            }
        }

        [ReportingAttribute]
        public PrWorkSpaceClient WorkSpaceRef
        {
            get
            {
                return ClientHelper.GetRefClient<PrWorkSpaceClient>(CompanyId, typeof(Uniconta.DataModel.PrWorkSpace), _WorkSpace);
            }
        }

        [ReportingAttribute]
        public ProjectTaskClient ProjectTaskRef
        {
            get
            {
                if (_Task == null)
                    return null;
                var proj = ClientHelper.GetRef(CompanyId, typeof(Uniconta.DataModel.Project), _Project);
                if (proj != null)
                {
                    var projClient = proj as Uniconta.DataModel.Project;
                    if (projClient != null)
                        return projClient.FindTask(_Task);
                    else
                        return ClientHelper.GetServerRef<ProjectTaskClient>(proj, typeof(Uniconta.DataModel.ProjectTask), _Task);
                }
                return null;
            }
        }

        [ReportingAttribute]
        public CompanyClient CompanyRef { get { return Global.CompanyRef(CompanyId); } }

        public ProjectBudgetYearClient()
        {
        }

        public ProjectBudgetYearClient(ProjectBudgetLineClient projectBudgetLineClient)
        {
            CompanyId = projectBudgetLineClient.CompanyId;
            _Project = projectBudgetLineClient.Project;
            _Employee = projectBudgetLineClient.Employee;
            _PrCategory = projectBudgetLineClient.PrCategory;
            _WorkSpace = projectBudgetLineClient.WorkSpace;
            _Task = projectBudgetLineClient.Task;
            _PayrollCategory = projectBudgetLineClient.PayrollCategory;

            OrgProject = _Project;
            OrgEmployee = _Employee;
            OrgPrCategory = _PrCategory;
            OrgWorkSpace = _WorkSpace;
            OrgTask = _Task;
            OrgPayrollCategory = _PayrollCategory;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Budget compared with the employee's norm hours. Budget below the norm hours is shown red
    static class ProjectBudgetUtilization
    {
        // Budget in percent of the norm hours. Null when there are no norm hours
        internal static double? Pct(double budget, double normHours)
        {
            return normHours > 0d ? budget * 100d / normHours : (double?)null;
        }

        internal static bool IsUnderbooked(double pct) => pct < 99.995d;

        // False when there are no norm hours for the month
        internal static bool IsUnderbooked(double budget, double normHours)
        {
            var pct = Pct(budget, normHours);
            return pct.HasValue && IsUnderbooked(pct.Value);
        }
    }

    // Colours the summary lines from the summary value and item (MultiBinding on Value and Item).
    // The line is found from the item's SerializableTag, which is kept in a saved layout, unlike a style set on the item:
    // TotalMonth1..12/TotalSum is budget minus norm hours, UtilMonth1..12/UtilTotal is budget in percent of norm hours
    public class SummaryValueToForegroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string color = "PrimaryTextColor";
            if (values != null && values.Length == 2 && values[0] is double d)
            {
                var tag = (values[1] as SumColumn)?.SerializableTag;
                if (tag != null)
                {
                    if (tag.StartsWith("TotalMonth") || tag == "TotalSum")
                        color = Math.Round(d, 2) < 0d ? "RedColor" : "GreenColor";
                    else if (tag.StartsWith("UtilMonth") || tag == "UtilTotal")
                        color = ProjectBudgetUtilization.IsUnderbooked(d) ? "RedColor" : "GreenColor";
                }
            }
            return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Application.Current.Resources[color]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class BudgetYearPayrollFilter : SQLCacheFilter
    {
        readonly bool Invoiceable;
        public BudgetYearPayrollFilter(SQLCache cache, bool Invoiceable) : base(cache) { this.Invoiceable = Invoiceable; }

        public override bool IsValid(object rec)
        {
            var p = ((Uniconta.DataModel.EmpPayrollCategory)rec);
                return (p._Invoiceable == this.Invoiceable && p._InternalType < Uniconta.DataModel.InternalType.Mileage);
        }
    }

}
