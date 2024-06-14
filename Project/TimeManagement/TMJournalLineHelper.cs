using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages;
using Uniconta.Common.Utility;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Project.TimeManagement
{
    public class TMJournalLineError
    {
        public string Message;
        public int RowId;
    }

    public class TMJournalLineHelper
    {
        #region Constants
        public const string VALIDATE_OK = "Ok";
        #endregion

        #region Variables
        Task LoadingTask;
        Company comp;
        bool err;
        List<TMJournalLineError> checkErrors;
        DateTime startDate;
        DateTime endDate;
        CrudAPI api;

        SQLCache projectCache, employeeCache, projectGrpCache, prCategoryCache, prTaskCache, prWorkspaceCache;
        SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> payrollCategoryCache;

        Uniconta.DataModel.Employee employee;
        Uniconta.DataModel.Project proj;
        Uniconta.DataModel.ProjectTask prTask;
        Uniconta.DataModel.PrCategory prCategory;
        Uniconta.DataModel.EmpPayrollCategory payrollCat;
        Uniconta.DataModel.ProjectGroup projGroup;
        Uniconta.DataModel.PrWorkSpace workspace;

        #endregion

        public enum TMJournalActionType { Close, Open, Approve, Validate }

        public TMJournalLineHelper(CrudAPI api, Uniconta.DataModel.Employee employee)
        {
            this.api = api;
            this.employee = employee;
            LoadingTask = LoadBaseData(api);
        }

        public TMJournalLineHelper(CrudAPI api)
        {
            this.api = api;
            LoadingTask = LoadBaseData(api);
        }

        public Task EmployeeChanged(Uniconta.DataModel.Employee employee)
        {
            this.employee = employee;
            return LoadingTask = LoadBaseData(api);
        }

        async Task LoadBaseData(QueryAPI api)
        {
            this.comp = api.CompanyEntity;

            payrollCategoryCache = api.GetCache<Uniconta.DataModel.EmpPayrollCategory>() ?? await api.LoadCache<Uniconta.DataModel.EmpPayrollCategory>().ConfigureAwait(false);
            projectCache = comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
            employeeCache = comp.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Employee)).ConfigureAwait(false);
            projectGrpCache = comp.GetCache(typeof(Uniconta.DataModel.ProjectGroup)) ?? await api.LoadCache(typeof(Uniconta.DataModel.ProjectGroup)).ConfigureAwait(false);
            prCategoryCache = comp.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? await api.LoadCache(typeof(Uniconta.DataModel.PrCategory)).ConfigureAwait(false);
            prTaskCache = comp.GetCache(typeof(Uniconta.DataModel.ProjectTask)) ?? await api.LoadCache(typeof(Uniconta.DataModel.ProjectTask)).ConfigureAwait(false);
            prWorkspaceCache = comp.GetCache(typeof(Uniconta.DataModel.PrWorkSpace)) ?? await api.LoadCache(typeof(Uniconta.DataModel.PrWorkSpace)).ConfigureAwait(false);

            LoadingTask = null; // we are done
        }


        #region Prevalidate
        public async Task<List<TMJournalLineError>> PreValidate(TMJournalActionType actionType, double valNormHours, double valRegHours)
        {
            var t = LoadingTask;
            if (t != null && !t.IsCompleted)
                await t;

            checkErrors = new List<TMJournalLineError>();

            PreValidateEmployee();
            PreValidateCalendar(valNormHours, valRegHours, actionType);
            PreValidatePayrollCategory();

            return checkErrors;
        }

        /// <summary>
        /// Validate - Employee
        /// </summary>
        void PreValidateEmployee()
        {
            if (employee._TMCloseDate < employee._TMApproveDate && employee._TMCloseDate != DateTime.MinValue && employee._TMApproveDate != DateTime.MinValue)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = string.Format("Close Date ('{0}') is less than Approve Date ('{1}'). This is not allowed. Please change the dates for the Employee.", employee._TMCloseDate.ToShortDateString(), employee._TMApproveDate.ToShortDateString()),
                });
            }
        }

        /// <summary>
        /// Validate - Norm Hours
        /// </summary>
        void PreValidateCalendar(double valNormHours, double valRegHours, TMJournalActionType actionType)
        {
            if ((actionType == TMJournalActionType.Validate || actionType == TMJournalActionType.Close) && (Math.Round(valRegHours - valNormHours, 2)) < 0)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = Uniconta.ClientTools.Localization.lookup("NormHoursNotFulfilled"),
                });
            }
        }

        // <summary>
        // Validate - Payroll category
        // </summary>
        void PreValidatePayrollCategory()
        {
            if (payrollCategoryCache == null)
                return;

            foreach (var rec in payrollCategoryCache)
            {
                if (rec._InternalType != 0 && rec._InternalType != InternalType.Mileage && rec._InternalProject == null)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {
                        Message = String.Format("Payroll category '{0}' must have specified an Internal Project", rec.KeyStr),
                        RowId = rec.RowId
                    });

                    err = true;
                }
            }
        }

        #endregion

        #region Validate Lines
        public async Task<List<TMJournalLineError>> ValidateLines(List<TMJournalLineClient> lines, 
                                                                  DateTime startDate, 
                                                                  DateTime endDate,
                                                                  Uniconta.DataModel.Employee employee)
        {
            var t = LoadingTask;
            if (t != null && !t.IsCompleted)
                await t;

            checkErrors = new List<TMJournalLineError>();

            this.employee = employee;
            var approveDate = employee._TMApproveDate;
            this.startDate = approveDate >= startDate ? approveDate.AddDays(1) : startDate;
            this.endDate = endDate;
            string lastProjNo = null;
            string lastPayroll = null;
            string lastTaskId = null;
            string lastWorkspace = null;

            int dayOfStart = this.startDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)this.startDate.DayOfWeek;
            int dayOfEnd = this.endDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)this.endDate.DayOfWeek;

            foreach (var rec in lines)
            {
                double hrsInPeriod = 0;
                for (int x = dayOfStart; x <= dayOfEnd; x++)
                {
                    hrsInPeriod += rec.GetHoursDayN(x);
                }

                rec.LogType = LogTypeEnum.Info;
                rec.ErrorInfo = string.Empty;
                err = false;

                if (lastPayroll != rec.PayrollCategory)
                {
                    lastPayroll = rec.PayrollCategory;
                    payrollCat = (EmpPayrollCategory)payrollCategoryCache.Get(lastPayroll);
                    prCategory = (Uniconta.DataModel.PrCategory)prCategoryCache.Get(payrollCat?._PrCategory);
                }

                if (rec.WorkSpace != null && lastWorkspace != rec.WorkSpace)
                {
                    lastWorkspace = rec.WorkSpace;
                    workspace = (PrWorkSpace)prWorkspaceCache.Get(lastWorkspace);
                }

                if (lastProjNo != rec._Project)
                {
                    lastProjNo = rec._Project;
                    proj = (Uniconta.DataModel.Project)projectCache.Get(lastProjNo);
                    if (proj?._Group != null)
                        projGroup = (Uniconta.DataModel.ProjectGroup)projectGrpCache.Get(proj._Group);

                    if (proj != null && comp.ProjectTask && proj.Tasks == null)
                        await proj.LoadTasks(api);
                }

                if (rec._Project != null && rec._Task != null && lastTaskId != string.Concat(rec._Project, rec._Task))
                {
                    lastTaskId = string.Concat(rec._Project, rec._Task);
                    prTask = proj.Tasks != null ? proj.Tasks.FirstOrDefault(s => s.Task == rec.Task) : null;
                }

                ValidateGeneral(rec);
                ValidateProject(rec, hrsInPeriod);
                ValidateTask(rec);
                ValidateProjectCategory(rec);
                ValidatePrice(rec);
                ValidateWorkspace(rec);

                if (!err)
                    rec.ErrorInfo = VALIDATE_OK;
                else
                    rec.LogType = LogTypeEnum.Error;
            }

            return checkErrors;
        }


        /// <summary>
        /// Validate - General
        /// </summary>
        void ValidateGeneral(TMJournalLineClient rec)
        {
            if (err)
                return;

            if (rec.Project == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = fieldCannotBeEmpty("ProjectNumber"),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (projGroup == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = fieldCannotBeEmpty("ProjectGroup"),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (rec.PayrollCategory == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = fieldCannotBeEmpty("PayrollCategory"),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (rec._RegistrationType == RegistrationType.Mileage)
            {
                if (rec.Text == null)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {
                        Message = fieldCannotBeEmpty("Purpose"),
                        RowId = rec.RowId
                    });

                    err = true;
                }

                if (rec.VechicleRegNo == null)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {
                        Message = fieldCannotBeEmpty("VechicleRegNo"),
                        RowId = rec.RowId
                    });

                    err = true;
                }
            }
        }

        /// <summary>
        /// Validate - Prices
        /// </summary>
        void ValidatePrice(TMJournalLineClient line)
        {
            if (!err)
            {
                int dayOfWeekStart = startDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)startDate.DayOfWeek;
                int dayOfWeekEnd = endDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)endDate.DayOfWeek;
                var isMileageTrans = payrollCat._InternalType == InternalType.Mileage ? true : false;

                for (int x = dayOfWeekStart; x <= dayOfWeekEnd; x++)
                {
                    if (line.GetHoursDayN(x) == 0)
                        continue;

                    var salesPrice = line.GetSalesPricesDayN(x);
                    var costPrice = line.GetCostPricesDayN(x);

                    if (salesPrice == 0)
                    {
                        if (costPrice == 0)
                        {
                            checkErrors.Add(new TMJournalLineError()
                            {
                                Message = Uniconta.ClientTools.Localization.lookup("NoRatesEmployee"),
                                RowId = line.RowId
                            });
                            err = true;
                        }
                        else if (line._Invoiceable && !isMileageTrans)
                        {
                            checkErrors.Add(new TMJournalLineError()
                            {
                                Message = string.Format("{0} ({1}: {2})",
                                Uniconta.ClientTools.Localization.lookup("NoSalesPrice"),
                                Uniconta.ClientTools.Localization.lookup("Date"),
                                startDate.AddDays(x - 1).ToString("dd.MM.yyyy")),
                                RowId = line.RowId
                            });
                            err = true;
                        }
                    }
                }
            }
        }

        static string fieldCannotBeEmpty(string field)
        {
            return String.Format("{0} ({1})",
                    Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"),
                    Uniconta.ClientTools.Localization.lookup(field));
        }

        /// <summary>
        /// Validate - Project
        /// </summary>
        void ValidateProject(TMJournalLineClient rec, double hrsInPeriod)
        {
            if (err || proj == null || hrsInPeriod == 0)
                return;

            if (proj._Blocked)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = Uniconta.ClientTools.Localization.lookup("ProjectIsBlocked"),
                    RowId = rec.RowId
                });

                err = true;
            }
        }

        void ValidateWorkspace(TMJournalLineClient rec)
        {
            if (err || rec.WorkSpace == null)
                return;

            if (workspace._Blocked)
            {
                checkErrors.Add(new TMJournalLineError()
                {   
                    Message = Uniconta.ClientTools.Localization.lookup("WorkspaceIsBlocked"),
                    RowId = rec.RowId
                });
                err = true;
            }
            else if (workspace._Warning)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = string.Concat(Uniconta.ClientTools.Localization.lookup("Warning"),":",string.Format(Uniconta.ClientTools.Localization.lookup("IsClosedOBJ"), Uniconta.ClientTools.Localization.lookup("Workspace"))),
                    RowId = rec.RowId,
                });
                rec.LogType = LogTypeEnum.Warning;
            }
        }

        /// <summary>
        /// Validate - Task
        /// </summary>
        void ValidateTask(TMJournalLineClient rec)
        {
            if (err)
                return;

            if (prTask != null)
            {
                if (prTask._Ended)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {   
                        Message = Uniconta.ClientTools.Localization.lookup("TaskIsEnded"),
                        RowId = rec.RowId
                    });

                    err = true;
                }

                if (!err && rec._WorkSpace != null && prTask._WorkSpace != null && rec._WorkSpace != prTask._WorkSpace)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {
                        Message = string.Concat(Uniconta.ClientTools.Localization.lookup("CombinationNotAllowed"), " ",
                                                Uniconta.ClientTools.Localization.lookup("Workspace"), "/",
                                                Uniconta.ClientTools.Localization.lookup("Task")),
                        RowId = rec.RowId
                    });

                    err = true;
                }
            }
        }

        /// <summary>
        /// Validate - Project Category
        /// </summary>
        void ValidateProjectCategory(TMJournalLineClient rec)
        {
            if (err)
                return;

            if (payrollCat?._PrCategory == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {   
                    Message = fieldCannotBeEmpty("ProjectCategory"),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (err)
                return;

            if (rec._RegistrationType == RegistrationType.Hours)
            {
                if (projGroup._Invoiceable && !prCategory._Invoiceable)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {
                        Message = string.Concat(Uniconta.ClientTools.Localization.lookup("CombinationNotAllowed"), " ",
                                                Uniconta.ClientTools.Localization.lookup("Project"), " (",
                                                Uniconta.ClientTools.Localization.lookup("Invoiceable"), ")/",
                                                Uniconta.ClientTools.Localization.lookup("ProjectCategory"), " (",
                                                Uniconta.ClientTools.Localization.lookup("NotInvoiceable"), ")"),
                        RowId = rec.RowId
                    });

                    err = true;
                }
                else if (!projGroup._Invoiceable && prCategory._Invoiceable)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {   
                        Message = string.Concat(Uniconta.ClientTools.Localization.lookup("CombinationNotAllowed"), " ",
                                                Uniconta.ClientTools.Localization.lookup("Project"), " (",
                                                Uniconta.ClientTools.Localization.lookup("NotInvoiceable"), ")/",
                                                Uniconta.ClientTools.Localization.lookup("ProjectCategory"), " (",
                                                Uniconta.ClientTools.Localization.lookup("Invoiceable"), ")"),
                        RowId = rec.RowId
                    });

                    err = true;
                }
                else if (!projGroup._Invoiceable && !prCategory._Invoiceable && rec._Invoiceable)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {
                        Message = string.Concat(Uniconta.ClientTools.Localization.lookup("Project"), " (",
                                                Uniconta.ClientTools.Localization.lookup("NotInvoiceable"), ")"),
                        RowId = rec.RowId
                    });

                    err = true;
                }


                if (payrollCat._InternalType == InternalType.Mileage)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {
                        Message = string.Format(Uniconta.ClientTools.Localization.lookup("ActionNotAllowedObj"),
                                                Uniconta.ClientTools.Localization.lookup("ProjectRegistration"),
                                                Uniconta.ClientTools.Localization.lookup("Mileage").ToLower()),
                        RowId = rec.RowId
                    });

                    err = true;
                }

                if (payrollCat._InternalType != 0 && rec.Project != payrollCat._InternalProject)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {
                        Message = string.Concat(Uniconta.ClientTools.Localization.lookup("CombinationNotAllowed"), " ",
                                                Uniconta.ClientTools.Localization.lookup("Project"), "/",
                                                Uniconta.ClientTools.Localization.lookup("PayrollCategory")),
                        RowId = rec.RowId
                    });
                    err = true;
                }
            }
            else
            {
                if (payrollCat._Rate == 0)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {
                        Message = string.Format(Uniconta.ClientTools.Localization.lookup("MissingOBJ"), string.Concat(Uniconta.ClientTools.Localization.lookup("Rate"), " ", Uniconta.ClientTools.Localization.lookup("Mileage").ToLower())),
                        RowId = rec.RowId
                    });

                    err = true;
                }

                if (payrollCat._InternalType != InternalType.Mileage)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {
                        Message = string.Format(Uniconta.ClientTools.Localization.lookup("Interntype"), "=",
                                                Uniconta.ClientTools.Localization.lookup("Mileage")),
                        RowId = rec.RowId
                    });

                    err = true;
                }

                if (payrollCat._InternalProject != null && rec._Project != payrollCat._InternalProject)
                {
                    checkErrors.Add(new TMJournalLineError()
                    {
                        Message = string.Format(Uniconta.ClientTools.Localization.lookup("ActionNotAllowedObj"),
                                                string.Concat(Uniconta.ClientTools.Localization.lookup("Project"), "'", rec._Project, "'"),
                                                Uniconta.ClientTools.Localization.lookup("Mileage").ToLower()),
                        RowId = rec.RowId
                    });

                    err = true;
                }
            }
        }
        #endregion

        public static string GetPeriod(DateTime Date)
        {
            var dayOffset = DayOfWeek.Thursday - Date.DayOfWeek;
            if (dayOffset > 0)
                Date = Date.AddDays(dayOffset);

            var week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(Date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString();

            return string.Format("{0}_{1}", Date.Year, week.PadLeft(2, '0'));
        }

        public static DateTime GetWeekMonday(DateTime Date)
        {
            int diff = (7 + (Date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return Date.AddDays(-1 * diff);
        }

        public static string GetTimeStamp()
        {
            return DateTime.Now.ToString("dd.MM.yy HH:mm");
        }

        public static void AddToList(ref HashSet<string> checkList, string value)
        {
            if (value == null)
                return;
            if (checkList == null)
                checkList = new HashSet<string>();
            checkList.Add(value);
        }

        public static double GetDistance(string fromAddress, string toAddress, bool avoidFerries = true, int decimals = 1)
        {
            return Uniconta.ClientTools.GoogleMaps.GetDistance(fromAddress, toAddress, avoidFerries, decimals);
        }
    }
}
