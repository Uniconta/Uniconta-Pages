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
        private Company comp;
        private bool err;
        private List<TMJournalLineError> checkErrors;
        private IList<EmpPayrollCategoryEmployeeClient> empPriceLst;
        private DateTime startDate;
        private DateTime endDate;
        private CrudAPI api;
        public Uniconta.DataModel.Employee employee;
        private SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> empPayrollCatList;
        private SQLTableCache<Uniconta.DataModel.ProjectGroup> projGroupList;
        private SQLTableCache<Uniconta.ClientTools.DataModel.ProjectClient> projCache;


        #endregion

        public enum TMJournalActionType { Close, Open, Approve, Validate }

        public TMJournalLineHelper(CrudAPI api, Uniconta.DataModel.Employee employee)
        {
            this.api = api;
            this.employee = employee;
        }

        public TMJournalLineHelper(CrudAPI api)
        {
            this.api = api;
        }

        #region Prevalidate
        public List<TMJournalLineError> PreValidate(TMJournalActionType actionType, double valNormHours, double valRegHours, SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> empPayrollCatList)
        {
            checkErrors = new List<TMJournalLineError>();

            this.empPayrollCatList = empPayrollCatList;

            PreValidateEmployee();
            PreValidateCalendar(valNormHours, valRegHours, actionType);
            PreValidatePayrollCategory();

            return checkErrors;
        }


        /// <summary>
        /// Validate - Employee
        /// </summary>
        private void PreValidateEmployee()
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
        private void PreValidateCalendar(double valNormHours, double valRegHours, TMJournalActionType actionType)
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
        private void PreValidatePayrollCategory()
        {
            if (empPayrollCatList == null)
                return;

            foreach (var rec in empPayrollCatList)
            {
                if (rec._InternalType != InternalType.None && rec._InternalType != InternalType.Mileage && rec._InternalProject == null)
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

        #region Validate Hours
        public List<TMJournalLineError> ValidateLinesHours(IEnumerable<TMJournalLineClientLocal> lines, 
                                                           DateTime startDate, 
                                                           DateTime endDate,
                                                           IList<EmpPayrollCategoryEmployeeClient> empPriceLst,
                                                           SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> empPayrollCatList,
                                                           SQLTableCache<Uniconta.ClientTools.DataModel.ProjectClient> projCache,
                                                           SQLTableCache<Uniconta.DataModel.ProjectGroup> projGroupList,
                                                           Uniconta.DataModel.Employee employee)
        {
            checkErrors = new List<TMJournalLineError>();

            this.empPayrollCatList = empPayrollCatList;
            this.projGroupList = projGroupList;
            this.projCache = projCache;
            this.comp = api.CompanyEntity;
            this.empPriceLst = empPriceLst;
            this.employee = employee;
            var approveDate = employee._TMApproveDate;
            this.startDate = approveDate >= startDate ? approveDate.AddDays(1) : startDate;
            this.endDate = endDate;

            foreach (var rec in lines)
            {
                rec.ErrorInfo = string.Empty;
                err = false;

                ValidateHoursGeneral(rec);
                ValidateHoursProject(rec);
                ValidateHoursProjectCategory(rec);

                if (!err)
                    rec.ErrorInfo = VALIDATE_OK;
            }

            if (!err)
                ValidateHoursPrice(lines);

            return checkErrors;
        }


        /// <summary>
        /// Validate - General
        /// </summary>
        private void ValidateHoursGeneral(TMJournalLineClientLocal rec)
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

            if (rec.PayrollCategory == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = fieldCannotBeEmpty("PayrollCategory"),
                    RowId = rec.RowId
                });

                err = true;
            }
        }

        /// <summary>
        /// Validate - Prices
        /// </summary>
        private void ValidateHoursPrice(IEnumerable<TMJournalLineClientLocal> lines)
        {
            SetEmplPrice(lines, 
                         empPriceLst,
                         empPayrollCatList,
                         projCache,
                         startDate, 
                         endDate, 
                         this.employee,
                         true);
        }

        private static string fieldCannotBeEmpty(string field)
        {
            return String.Format("{0} ({1})",
                    Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"),
                    Uniconta.ClientTools.Localization.lookup(field));
        }

        /// <summary>
        /// Validate - Project
        /// </summary>
        private void ValidateHoursProject(TMJournalLineClientLocal rec)
        {
            if (err)
                return;

            var projRef = rec.ProjectRef;
            if (rec._Project != null && projRef != null && projRef._Blocked)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = Uniconta.ClientTools.Localization.lookup("ProjectIsBlocked"),
                    RowId = rec.RowId
                });

                err = true;
            }
        }

        /// <summary>
        /// Validate - Project Category
        /// </summary>
        private void ValidateHoursProjectCategory(TMJournalLineClientLocal rec)
        {
            if (err)
                return;

            var proj = projCache.Get(rec._Project);
            var projGroup = projGroupList.Get(proj?._Group);
            var payrollCat = empPayrollCatList.Get(rec._PayrollCategory);
            if (payrollCat?._PrCategory == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = String.Format("Payroll category '{0}' need to be updated with a Project category.", rec._PayrollCategory),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (projGroup != null && payrollCat._Invoiceable && !projGroup._Invoiceable)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = String.Format("Invoiceable Payroll category '{0}' cant be posted to a none Invoiceable project", rec._PayrollCategory),
                    RowId = rec.RowId
                });

                err = true;
            }


            if (projGroup != null && !payrollCat._Invoiceable && projGroup._Invoiceable && payrollCat._InternalProject != null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = String.Format("None invoiceable Payroll category '{0}' cant be posted to a Invoiceable project", rec._PayrollCategory),
                    RowId = rec.RowId
                });

                err = true;
            }


            if (payrollCat._InternalType == InternalType.Mileage)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = String.Format("Mileage must be registrered in Mileage section"),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (payrollCat._InternalType != InternalType.None && rec.Project != payrollCat._InternalProject)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = String.Format("Project '{0}' is not allowed for Payroll category '{1}' due to setup", rec.Project, rec._PayrollCategory),
                    RowId = rec.RowId
                });
                err = true;
            }
        }
        #endregion
        
        #region Validate Mileage
        public List<TMJournalLineError> ValidateLinesMileage(IEnumerable<TMJournalLineClientLocal> lines,
                                                             DateTime startDate,
                                                             DateTime endDate,
                                                             IList<EmpPayrollCategoryEmployeeClient> empPriceLst,
                                                             SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> empPayrollCatList,
                                                             SQLTableCache<Uniconta.ClientTools.DataModel.ProjectClient> projCache,
                                                             SQLTableCache<Uniconta.DataModel.ProjectGroup> projGroupList,
                                                             Uniconta.DataModel.Employee employee)
        {
            checkErrors = new List<TMJournalLineError>();

            this.empPayrollCatList = empPayrollCatList;
            this.projGroupList = projGroupList;
            this.projCache = projCache;
            this.comp = api.CompanyEntity;
            this.empPriceLst = empPriceLst;
            this.employee = employee;
            var approveDate = employee._TMApproveDate;
            this.startDate = approveDate >= startDate ? approveDate.AddDays(1) : startDate;
            this.endDate = endDate;

            foreach (var rec in lines)
            {
                rec.ErrorInfo = string.Empty;
                err = false;

                ValidateMileageGeneral(rec);
                ValidateMileageProject(rec);
                ValidateMileageProjectCategory(rec);

                if (!err)
                    rec.ErrorInfo = VALIDATE_OK;
            }

            if (!err)
                ValidateHoursPrice(lines);

            return checkErrors;
        }

        /// <summary>
        /// Validate - General
        /// </summary>
        private void ValidateMileageGeneral(TMJournalLineClientLocal rec)
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
            
            if (rec.PayrollCategory == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = fieldCannotBeEmpty("PayrollCategory"),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (rec.AddressFrom == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = fieldCannotBeEmpty("AddressFrom"),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (rec.AddressTo == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = fieldCannotBeEmpty("AddressTo"),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (rec.Text == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = fieldCannotBeEmpty("Purpose"),
                    RowId = rec.RowId
                });

                err = true;
            }

            //TODO:Disabled until VechicleRegNo can be registered in table.Employee
            //if (rec.VechicleRegNo == null)
            //{
            //    checkErrors.Add(new TMJournalLineError()
            //    {
            //        Message = fieldCannotBeEmpty("VechicleRegNo"),
            //        RowId = rec.RowId
            //    });

            //    err = true;
            //}

        }

        /// <summary>
        /// Validate - Project
        /// </summary>
        private void ValidateMileageProject(TMJournalLineClientLocal rec)
        {
            if (err)
                return;

            if (rec.ProjectRef.Blocked)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = Uniconta.ClientTools.Localization.lookup("ProjectIsBlocked"),
                    RowId = rec.RowId
                });

                err = true;
            }
        }

        /// <summary>
        /// Validate - Project Category
        /// </summary>
        private void ValidateMileageProjectCategory(TMJournalLineClientLocal rec)
        {
            if (err)
                return;

            var payrollCat = empPayrollCatList?.Get(rec.PayrollCategory);

            if (payrollCat._PrCategory == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = String.Format("Payroll category '{0}' need to be updated with a Project category.", rec.PayrollCategory),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (payrollCat._Rate == 0)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = String.Format("Payroll category '{0}' need to be specified with kilometer rate.", rec.PayrollCategory),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (payrollCat._InternalType != InternalType.Mileage)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = String.Format("Payroll category '{0}' must have Internaltype '{1}'", rec.PayrollCategory, InternalType.Mileage),
                    RowId = rec.RowId
                });

                err = true;
            }

            if (payrollCat._InternalProject != null && rec._Project != payrollCat._InternalProject)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = String.Format("Only project '{0}' is allowed for mileage registration", payrollCat._InternalProject),
                    RowId = rec.RowId
                });

                err = true;
            }

        }
        #endregion

        #region SetEmplPrice

        private EmpPayrollCategoryEmployeeClient PriceMatrix(DateTime date, int dayIdx, Uniconta.DataModel.Project project, string payrollCat, bool continueSearch = false)
        {
            if (payrollCat == null)
                return null;

            var ProjectNr = project?._Number;
            var ProjectAcc = project?._DCAccount;
            EmpPayrollCategoryEmployeeClient found = null;
            var dt = date.AddDays(dayIdx - 1);
            foreach (var rate in empPriceLst)
            {
                if (rate._ValidFrom != DateTime.MinValue && rate._ValidFrom > dt)
                    continue;
                if (rate._ValidTo != DateTime.MinValue && rate._ValidTo < dt)
                    continue;
                if (rate._PayrollCategory != payrollCat)
                    continue;

                if (rate._Project == ProjectNr)
                    return rate; // best fit, we exit
                if (rate._Project != null) // no match
                    continue;
                if (rate._DCAccount == ProjectAcc)
                {
                    if (found?._DCAccount == null) // better fit
                        found = rate;
                }
                else if (rate._DCAccount == null && found == null && !continueSearch)
                    found = rate; // a fit, but weakest fit.
            }
            return found;
        }


        public void SetEmplPrice(IEnumerable<TMJournalLineClientLocal> lst,
                                 IList<EmpPayrollCategoryEmployeeClient> empPriceLst,
                                 SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> empPayrollCatLst,
                                 SQLTableCache<Uniconta.ClientTools.DataModel.ProjectClient> projLst,
                                 DateTime startDate, 
                                 DateTime endDate,
                                 Uniconta.DataModel.Employee employee,
                                 bool validate = false)
        {
#if !SILVERLIGHT
            bool foundErr;
           
            this.employee = employee;
            this.empPriceLst = empPriceLst;
            int dayOfWeekStart = startDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)startDate.DayOfWeek;
            int dayOfWeekEnd = endDate.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)endDate.DayOfWeek;

            var defaultPayrollCategory = empPayrollCatLst.Where(s => s._PrCategory == null && s.KeyStr == "Default").FirstOrDefault();

            if (empPriceLst != null && empPriceLst.Any(s => s._Employee != employee._Number)) // it contains other employees, remove them
                empPriceLst = empPriceLst.Where(s => s._Employee == employee._Number).ToList();

            foreach (var trans in lst)
            {
                var isMileageTrans = trans._RegistrationType == RegistrationType.Mileage ? true : false;
                if (isMileageTrans)
                {
                    var projGroup = projGroupList.Get(trans.ProjectRef.Group);
                    if (projGroup != null)
                        trans._Invoiceable = projGroup._Invoiceable;
                }

                var payrollCat = empPayrollCatLst.Get(trans._PayrollCategory);
                var Proj = projLst.Get(trans._Project);

                foundErr = false;
                for (int x = dayOfWeekStart; x <= dayOfWeekEnd; x++)
                {
                    if (trans.GetHoursDayN(x) == 0)
                        continue;

                    double salesPrice = 0, costPrice = 0;

                    EmpPayrollCategoryEmployeeClient prices = null;
                    if (empPriceLst != null && empPriceLst.Count > 0)
                    {
                        prices = PriceMatrix(startDate, x, Proj, trans._PayrollCategory);

                        if (prices != null && prices._Project == null && prices._DCAccount == null && !isMileageTrans)
                            prices = PriceMatrix(startDate, x, Proj, defaultPayrollCategory?.KeyStr, true) ?? prices;
                        else if (prices == null && defaultPayrollCategory != null && !isMileageTrans)
                            prices = PriceMatrix(startDate, x, Proj, defaultPayrollCategory.KeyStr);

                        if (prices != null)
                        {
                            salesPrice = prices._SalesPrice;
                            costPrice = prices._CostPrice;
                        }
                    }

                    if (payrollCat != null)
                    {
                        if (salesPrice == 0)
                            salesPrice = payrollCat._SalesPrice;
                        if (costPrice == 0)
                            costPrice = payrollCat._Rate;
                    }

                    if (!isMileageTrans) //Always fallback to Employee for cost and sales prices
                    {
                        if (salesPrice == 0)
                            salesPrice = employee._SalesPrice;
                        if (costPrice == 0)
                            costPrice = employee._CostPrice;
                    }

                    trans.SetPricesDayN(x, trans._Invoiceable ? salesPrice : 0, costPrice);

                    if (validate && !foundErr)
                    {
                        if (salesPrice == 0 && costPrice == 0)
                        {
                            if (startDate.AddDays(x - 1) >= employee._Hired)
                            {
                                checkErrors.Add(new TMJournalLineError()
                                {
                                    Message = Uniconta.ClientTools.Localization.lookup("NoRatesEmployee"),
                                    RowId = trans.RowId
                                });

                                trans.ErrorInfo = string.Empty;
                                err = true;
                                foundErr = true;
                            }
                        }
                        else
                        {
                            if (!foundErr && salesPrice == 0 && trans._Invoiceable && !isMileageTrans)
                            {
                                checkErrors.Add(new TMJournalLineError()
                                {
                                    Message = string.Format("{0} ({1}: {2})",
                                    Uniconta.ClientTools.Localization.lookup("NoSalesPrice"),
                                    Uniconta.ClientTools.Localization.lookup("Date"),
                                    startDate.AddDays(x - 1).ToString("dd.MM.yyyy")),
                                    RowId = trans.RowId
                                });

                                trans.ErrorInfo = string.Empty;
                                err = true;
                                foundErr = true;
                            }
                        }
                    }
                }
            }
#endif
        }
        #endregion

        #region GetEmplPrice
        public Tuple<double, double> GetEmplPrice(IList<EmpPayrollCategoryEmployeeClient> empPriceLst,
                                                  SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> empPayrollCatLst,
                                                  SQLTableCache<Uniconta.DataModel.ProjectGroup> projGroupLst,
                                                  Uniconta.DataModel.Employee employee,
                                                  Uniconta.DataModel.Project project,
                                                  DateTime priceDate,
                                                  string payrollCategory = null)
        {
#if !SILVERLIGHT
                       
            if (employee == null)
                return new Tuple<double, double>(0, 0);

            this.empPriceLst = empPriceLst;
            var projGroup = projGroupLst.Get(project._Group);

            EmpPayrollCategory payrollCat;
            bool isMileagePrice;
            bool invoiceable;
            if (payrollCategory == null)
            {
                payrollCat = null;
                isMileagePrice = false;
                invoiceable = projGroup._Invoiceable;
                payrollCategory = string.Empty;
            }
            else
            {
                payrollCat = empPayrollCatLst.Get(payrollCategory);
                isMileagePrice = payrollCat._InternalType == InternalType.Mileage;
                invoiceable = projGroup._Invoiceable && payrollCat._Invoiceable;
            }


            if (empPriceLst != null && empPriceLst.Count > 0 && empPriceLst.Any(s => s._Employee != employee._Number)) // it contains other employees, remove them
                empPriceLst = empPriceLst.Where(s => s._Employee == employee._Number).ToList();

            if (isMileagePrice)
                invoiceable = projGroup._Invoiceable;

            double salesPrice = 0, costPrice = 0;

            if (empPriceLst != null && empPriceLst.Count > 0)
            {
                var defaultPayrollCategory = empPayrollCatLst.Where(s => s._PrCategory == null && s.KeyStr == "Default").FirstOrDefault();

                var prices = PriceMatrix(priceDate, 1, project, payrollCategory);

                if (prices != null && prices._Project == null && prices._DCAccount == null)
                    prices = PriceMatrix(priceDate, 1, project, defaultPayrollCategory?.KeyStr, true) ?? prices;
                else if (prices == null && defaultPayrollCategory != null)
                    prices = PriceMatrix(priceDate, 1, project, defaultPayrollCategory.KeyStr);

                if (prices != null)
                {
                    salesPrice = prices._SalesPrice;
                    costPrice = prices._CostPrice;
                }
            }

            if (payrollCat != null)
            {
                if (salesPrice == 0)
                    salesPrice = payrollCat._SalesPrice;
                if (costPrice == 0)
                    costPrice = payrollCat._Rate;
            }

            if (! isMileagePrice) //Always fallback to Employee for cost and sales prices
            {
                if (salesPrice == 0)
                    salesPrice = employee._SalesPrice;
                if (costPrice == 0)
                    costPrice = employee._CostPrice;
            }

            return new Tuple<double, double>(costPrice, invoiceable ? salesPrice : 0);
#endif
            return new Tuple<double, double>(0, 0);
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

#if !SILVERLIGHT
        public static double GetDistance(string fromAddress, string toAddress, bool avoidFerries = true, int decimals = 1)
        {
            return Uniconta.ClientTools.GoogleMaps.GetDistance(fromAddress, toAddress, avoidFerries, decimals);
        }
#endif

#region TMJournalLineClientPage
        public class TMJournalLineClientLocal : TMJournalLineClient 
        {
            internal object _projectSource;
            public object ProjectSource { get { return _projectSource; } }

            internal object _payrollSource;
            public object PayrollSource { get { return _payrollSource; } }

            internal object _projectTaskSource;
            public object ProjectTaskSource { get { return _projectTaskSource; } }

            private string _ErrorInfo;
            [Display(Name = "SystemInfo", ResourceType = typeof(DCTransText))]
            public string ErrorInfo { get { return _ErrorInfo; } set { _ErrorInfo = value; NotifyPropertyChanged("ErrorInfo"); } }

            private Double _SalesPriceDay1;
            [Display(Name = "SalesPriceDay1")]
            public Double SalesPriceDay1 { get { return _SalesPriceDay1; } set { _SalesPriceDay1 = value; NotifyPropertyChanged("SalesPriceDay1"); } }

            private Double _SalesPriceDay2;
            [Display(Name = "SalesPriceDay2")]
            public Double SalesPriceDay2 { get { return _SalesPriceDay2; } set { _SalesPriceDay2 = value; NotifyPropertyChanged("SalesPriceDay2"); } }

            private Double _SalesPriceDay3;
            [Display(Name = "SalesPriceDay3")]
            public Double SalesPriceDay3 { get { return _SalesPriceDay3; } set { _SalesPriceDay3 = value; NotifyPropertyChanged("SalesPriceDay3"); } }

            private Double _SalesPriceDay4;
            [Display(Name = "SalesPriceDay4")]
            public Double SalesPriceDay4 { get { return _SalesPriceDay4; } set { _SalesPriceDay4 = value; NotifyPropertyChanged("SalesPriceDay4"); } }

            private Double _SalesPriceDay5;
            [Display(Name = "SalesPriceDay5")]
            public Double SalesPriceDay5 { get { return _SalesPriceDay5; } set { _SalesPriceDay5 = value; NotifyPropertyChanged("SalesPriceDay5"); } }

            private Double _SalesPriceDay6;
            [Display(Name = "SalesPriceDay6")]
            public Double SalesPriceDay6 { get { return _SalesPriceDay6; } set { _SalesPriceDay6 = value; NotifyPropertyChanged("SalesPriceDay6"); } }

            private Double _SalesPriceDay7;
            [Display(Name = "SalesPriceDay7")]
            public Double SalesPriceDay7 { get { return _SalesPriceDay7; } set { _SalesPriceDay7 = value; NotifyPropertyChanged("SalesPriceDay7"); } }

            private Double _CostPriceDay1;
            [Display(Name = "CostPriceDay1")]
            public Double CostPriceDay1 { get { return _CostPriceDay1; } set { _CostPriceDay1 = value; NotifyPropertyChanged("CostPriceDay1"); } }

            private Double _CostPriceDay2;
            [Display(Name = "CostPriceDay2")]
            public Double CostPriceDay2 { get { return _CostPriceDay2; } set { _CostPriceDay2 = value; NotifyPropertyChanged("CostPriceDay2"); } }

            private Double _CostPriceDay3;
            [Display(Name = "CostPriceDay3")]
            public Double CostPriceDay3 { get { return _CostPriceDay3; } set { _CostPriceDay3 = value; NotifyPropertyChanged("CostPriceDay3"); } }

            private Double _CostPriceDay4;
            [Display(Name = "CostPriceDay4")]
            public Double CostPriceDay4 { get { return _CostPriceDay4; } set { _CostPriceDay4 = value; NotifyPropertyChanged("CostPriceDay4"); } }

            private Double _CostPriceDay5;
            [Display(Name = "CostPriceDay5")]
            public Double CostPriceDay5 { get { return _CostPriceDay5; } set { _CostPriceDay5 = value; NotifyPropertyChanged("CostPriceDay5"); } }

            private Double _CostPriceDay6;
            [Display(Name = "CostPriceDay6")]
            public Double CostPriceDay6 { get { return _CostPriceDay6; } set { _CostPriceDay6 = value; NotifyPropertyChanged("CostPriceDay6"); } }

            private Double _CostPriceDay7;
            [Display(Name = "CostPriceDay7")]
            public Double CostPriceDay7 { get { return _CostPriceDay7; } set { _CostPriceDay7 = value; NotifyPropertyChanged("CostPriceDay7"); } }

            public double TotalCostPrice
            {
                get
                {
                    return _Day1 * _CostPriceDay1 + _Day2 * _CostPriceDay2 + _Day3 * _CostPriceDay3 + _Day4 * _CostPriceDay4 + _Day5 * _CostPriceDay5 + _Day6 * _CostPriceDay6 + _Day7 * _CostPriceDay7;
                }
            }
            public double TotalSalesPrice
            {
                get
                {
                    return _Day1 * _SalesPriceDay1 + _Day2 * _SalesPriceDay2 + _Day3 * _SalesPriceDay3 + _Day4 * _SalesPriceDay4 + _Day5 * _SalesPriceDay5 + _Day6 * _SalesPriceDay6 + _Day7 * _SalesPriceDay7;
                }
            }

            public int StatusDay1 { get { return GetStatus(_Date); } }
            public int StatusDay2 { get { return GetStatus(_Date.AddDays(1)); } }
            public int StatusDay3 { get { return GetStatus(_Date.AddDays(2)); } }
            public int StatusDay4 { get { return GetStatus(_Date.AddDays(3)); } }
            public int StatusDay5 { get { return GetStatus(_Date.AddDays(4)); } }
            public int StatusDay6 { get { return GetStatus(_Date.AddDays(5)); } }
            public int StatusDay7 { get { return GetStatus(_Date.AddDays(6)); } }

            public void SetPricesDayN(int day, double salesPrice, double costPrice)
            {
                switch (day)
                {
                    case 1: _SalesPriceDay1 = salesPrice; _CostPriceDay1 = costPrice; break;
                    case 2: _SalesPriceDay2 = salesPrice; _CostPriceDay2 = costPrice; break;
                    case 3: _SalesPriceDay3 = salesPrice; _CostPriceDay3 = costPrice; break;
                    case 4: _SalesPriceDay4 = salesPrice; _CostPriceDay4 = costPrice; break;
                    case 5: _SalesPriceDay5 = salesPrice; _CostPriceDay5 = costPrice; break;
                    case 6: _SalesPriceDay6 = salesPrice; _CostPriceDay6 = costPrice; break;
                    case 7: _SalesPriceDay7 = salesPrice; _CostPriceDay7 = costPrice; break;
                }
            }

            public Tuple<double, double> GetPricesDayN(int day)
            {
                switch (day)
                {
                    case 1: return new Tuple<double, double>(_SalesPriceDay1, _CostPriceDay1);
                    case 2: return new Tuple<double, double>(_SalesPriceDay2, _CostPriceDay2);
                    case 3: return new Tuple<double, double>(_SalesPriceDay3, _CostPriceDay3);
                    case 4: return new Tuple<double, double>(_SalesPriceDay4, _CostPriceDay4);
                    case 5: return new Tuple<double, double>(_SalesPriceDay5, _CostPriceDay5);
                    case 6: return new Tuple<double, double>(_SalesPriceDay6, _CostPriceDay6);
                    case 7: return new Tuple<double, double>(_SalesPriceDay7, _CostPriceDay7);
                    default: return new Tuple<double, double>(0, 0);
                }
            }

            public int IsEditable
            {
                get
                {
                    if (_RowId > 0 && !AllSevenDaysStatus(_Date, true))
                        return 1;
                    else
                        return 0;
                }
            }

            int GetStatus(DateTime date)
            {
                var EmployeeRef = this.EmployeeRef;
                if (_Employee == null || EmployeeRef == null)
                    return 0;
                if (EmployeeRef._Hired != DateTime.MinValue && date < EmployeeRef._Hired)
                    return 3;
                else if (EmployeeRef._TMCloseDate == DateTime.MinValue || date > EmployeeRef._TMCloseDate)
                    return 0; //Editable
                else if (date <= EmployeeRef._TMApproveDate)
                    return 2; //Non Edittable green
                else if (date <= EmployeeRef._TMCloseDate)
                    return 1; //Non Edittable yellow
                return 0;
            }

            bool AllSevenDaysStatus(DateTime Date, bool isStartdate = false)
            {
                var EmployeeRef = this.EmployeeRef;
                if (this._Employee != null && EmployeeRef != null)
                {
                    if (isStartdate && Date > EmployeeRef._TMCloseDate)
                        return true;
                    if (isStartdate && Date <= EmployeeRef._TMCloseDate && (Total == 0d || IsFieldEditable()))
                        return true;
                }
                return false;
            }

            DateTime FirstDayOfWeek(DateTime selectedDate)
            {
                var dt = selectedDate;
                int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
                return dt.AddDays(-diff).Date;
            }

            bool IsFieldEditable()
            {
                double sum = 0d;
                var status = StatusDay1;
                if (status == 1 || status == 2)
                    sum +=_Day1;
                status = StatusDay2;
                if (status == 1 || status == 2)
                    sum += _Day2;
                status = StatusDay3;
                if (status == 1 || status == 2)
                    sum += _Day3;
                status = StatusDay4;
                if (status == 1 || status == 2)
                    sum += _Day4;
                status = StatusDay5;
                if (status == 1 || status == 2)
                    sum += _Day5;
                status = StatusDay6;
                if (status == 1 || status == 2)
                    sum += _Day6;
                status = StatusDay7;
                if (status == 1 || status == 2)
                    sum += _Day7;
                return (sum == 0);
            }

            public bool IsMatched
            {
                get
                {
                    if (_RegistrationType == Uniconta.DataModel.RegistrationType.Hours)
                        if (!string.IsNullOrEmpty(PayrollCategory) && !string.IsNullOrEmpty(InternalType))
                            return true;
                    return false;
                }
            }
        }
#endregion
    }
}
