using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
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
        private bool _IsEditable = false;
        private string _Project, _Employee, _PrCategory, _WorkSpace, _Task, _PayrollCategory;
        public readonly int CompanyId;

        public bool IsEditable
        {
            get { return _IsEditable; }
            set
            {
                _IsEditable = value; NotifyPropertyChanged(nameof(IsEditable));
            }
        }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "Project", ResourceType = typeof(ProjectTransClientText))]
        public string Project
        {
            get { return _Project; }
            set
            {
                if (!_IsEditable)
                    return;

                _Project = value;
                NotifyPropertyChanged(nameof(Project));
                NotifyPropertyChanged(nameof(ProjectName));
            }
        }

        [Display(Name = "ProjectName", ResourceType = typeof(ProjectTransClientText))]
        public string ProjectName { get { return ClientHelper.GetName(CompanyId, typeof(Uniconta.DataModel.Project), _Project); } }

        [Display(Name = "Employee", ResourceType = typeof(ProjectTransClientText))]
        public string Employee { get { return _Employee; } }

        [Display(Name = "EmployeeName", ResourceType = typeof(ProjectTransClientText))]
        public string EmployeeName { get { return ClientHelper.GetName(CompanyId, typeof(Uniconta.DataModel.Employee), _Employee); } }


        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrCategory))]
        [Display(Name = "PrCategory", ResourceType = typeof(ProjectTransClientText))]
        public string PrCategory
        {
            get { return _PrCategory; }
            set
            {
                if (!_IsEditable)
                    return;

                _PrCategory = value;
                NotifyPropertyChanged(nameof(PrCategory));
                NotifyPropertyChanged(nameof(PrCategoryName));
            }
        }

        [Display(Name = "CategoryName", ResourceType = typeof(ProjectTransClientText))]
        public string PrCategoryName { get { return ClientHelper.GetName(CompanyId, typeof(Uniconta.DataModel.PrCategory), _PrCategory); } }


        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.EmpPayrollCategory))]
        [Display(Name = "PayrollCategory", ResourceType = typeof(ProjectTransClientText))]
        public string PayrollCategory
        {
            get { return _PayrollCategory; }
            set
            {
                if (!_IsEditable)
                    return;

                _PayrollCategory = value;
                PrCategory = ((Uniconta.DataModel.EmpPayrollCategory)ClientHelper.GetRef(CompanyId, typeof(Uniconta.DataModel.EmpPayrollCategory), _PayrollCategory))?._PrCategory;
                NotifyPropertyChanged(nameof(PayrollCategory));
                NotifyPropertyChanged(nameof(PayrollCategoryName));
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
                if (!_IsEditable)
                    return;

                _WorkSpace = value;
                NotifyPropertyChanged(nameof(WorkSpace));
            }
        }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.ProjectTask))]
        [Display(Name = "Task", ResourceType = typeof(ProjectTransClientText))]
        public string Task
        {
            get { return _Task; }
            set
            {
                if (!_IsEditable)
                    return;

                _Task = value;
                NotifyPropertyChanged(nameof(Task));
            }
        }

        [Display(Name = "Month1", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty1 { get { return _MonthQty1; } set { _MonthQty1 = value; NotifyPropertyChanged(nameof(MonthQty1)); } }

        [Display(Name = "Month2", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty2 { get { return _MonthQty2; } set { _MonthQty2 = value; NotifyPropertyChanged(nameof(MonthQty2)); } }

        [Display(Name = "Month3", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty3 { get { return _MonthQty3; } set { _MonthQty3 = value; NotifyPropertyChanged(nameof(MonthQty3)); } }

        [Display(Name = "Month4", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty4 { get { return _MonthQty4; } set { _MonthQty4 = value; NotifyPropertyChanged(nameof(MonthQty4)); } }

        [Display(Name = "Month5", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty5 { get { return _MonthQty5; } set { _MonthQty5 = value; NotifyPropertyChanged(nameof(MonthQty5)); } }

        [Display(Name = "Month6", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty6 { get { return _MonthQty6; } set { _MonthQty6 = value; NotifyPropertyChanged(nameof(MonthQty6)); } }

        [Display(Name = "Month7", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty7 { get { return _MonthQty7; } set { _MonthQty7 = value; NotifyPropertyChanged(nameof(MonthQty7)); } }

        [Display(Name = "Month8", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty8 { get { return _MonthQty8; } set { _MonthQty8 = value; NotifyPropertyChanged(nameof(MonthQty8)); } }

        [Display(Name = "Month9", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty9 { get { return _MonthQty9; } set { _MonthQty9 = value; NotifyPropertyChanged(nameof(MonthQty9)); } }

        [Display(Name = "Month10", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty10 { get { return _MonthQty10; } set { _MonthQty10 = value; NotifyPropertyChanged(nameof(MonthQty10)); } }

        [Display(Name = "Month11", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty11 { get { return _MonthQty11; } set { _MonthQty11 = value; NotifyPropertyChanged(nameof(MonthQty11)); } }

        [Display(Name = "Month12", ResourceType = typeof(ProjectBudgetYearText))]
        public double MonthQty12 { get { return _MonthQty12; } set { _MonthQty12 = value; NotifyPropertyChanged(nameof(MonthQty12)); } }

        [Display(Name = "Total", ResourceType = typeof(ProjectBudgetYearText))]
        public double TotalQty
        {
            get
            {
                return _MonthQty1 + _MonthQty2 + _MonthQty3 + _MonthQty4 + _MonthQty5 + _MonthQty6 + _MonthQty7 + _MonthQty8 + _MonthQty9 + _MonthQty10 + _MonthQty11 + _MonthQty12;
            }
        }

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
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
