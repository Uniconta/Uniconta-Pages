using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
        private EmpPayrollCategoryEmployeeClient[] empPriceLst;
        private DateTime startDate;
        private CrudAPI api;
        private Uniconta.DataModel.Employee employee;
        private SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> empPayrollCatList;
        #endregion

        public TMJournalLineHelper(CrudAPI api, Uniconta.DataModel.Employee employee)
        {
            this.api = api;
            this.employee = employee;
        }

        #region Prevalidate
        public List<TMJournalLineError> PreValidate(double normHoursTotal, SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> empPayrollCatList)
        {
            checkErrors = new List<TMJournalLineError>();

            this.empPayrollCatList = empPayrollCatList;

            PreValidateNormHours(normHoursTotal);
            PreValidatePayrollCategory();

            return checkErrors;
        }

        /// <summary>
        /// Validate - Norm Hours
        /// </summary>
        private void PreValidateNormHours(double normHoursTotal)
        {
            if (normHoursTotal == 0)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = Uniconta.ClientTools.Localization.lookup("EmployeeNormHoursMissing"),
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

            foreach (var rec in empPayrollCatList.Where(s => s._InternalType != InternalType.None))
            {
                if (rec._InternalProject == null)
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
        public List<TMJournalLineError> ValidateLinesHours(IEnumerable<TMJournalLineClientLocal> lines, DateTime startDate, EmpPayrollCategoryEmployeeClient[] empPriceLst, SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> empPayrollCatList)
        {
            checkErrors = new List<TMJournalLineError>();

            this.empPayrollCatList = empPayrollCatList;
            this.comp = api.CompanyEntity;
            this.empPriceLst = empPriceLst;
            this.startDate = startDate;

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
            SetEmplPrice(lines, empPriceLst, startDate, true);
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
        private void ValidateHoursProjectCategory(TMJournalLineClientLocal rec)
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
                    Message = String.Format("Project '{0}' is not allowed for Payroll category '{1}' due to setup", rec.Project, rec.PayrollCategory),
                    RowId = rec.RowId
                });
                err = true;
            }
        }
        #endregion
        
        #region Validate Mileage
        public List<TMJournalLineError> ValidateLinesMileage(IEnumerable<TMJournalLineClientLocal> lines, DateTime startDate, SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> empPayrollCatList)
        {
            checkErrors = new List<TMJournalLineError>();

            this.empPayrollCatList = empPayrollCatList;
            this.comp = api.CompanyEntity;
            this.startDate = startDate;

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

            if (payrollCat._InternalProject == null)
            {
                checkErrors.Add(new TMJournalLineError()
                {
                    Message = String.Format("Payroll category '{0}' must have an Internal Project", rec.PayrollCategory),
                    RowId = rec.RowId
                });

                err = true;
            }
        }

        #endregion

        public void SetEmplPrice(IEnumerable<TMJournalLineClientLocal> lst, EmpPayrollCategoryEmployeeClient[] empPriceLst, DateTime startDate, bool validate = false)
        {
#if !SILVERLIGHT
            bool foundErr;
            try
            {
                var defaultPayrollCategory = empPayrollCatList.Where(s => s.KeyStr == "Default" && s._PrCategory == null).FirstOrDefault(); 

                foreach (var trans in lst)
                {
                    var payrollCat = empPayrollCatList?.Get(trans.PayrollCategory);

                    foundErr = false;
                    for (int x = 1; x <= 7; x++)
                    {
                        double salesPrice = 0, costPrice = 0;

                        List<EmpPayrollCategoryEmployeeClient> prices = null;
                        if (empPriceLst != null && empPriceLst.Any()) 
                        {
                            prices = empPriceLst.Where(s => s.Employee == trans.Employee &&
                                                                s.PayrollCategory == trans.PayrollCategory &&
                                                                s.Account == null &&
                                                                s.Project == trans.Project &&
                                                                (s.ValidTo >= startDate.AddDays(x - 1) || s.ValidTo == DateTime.MinValue) && s.ValidFrom <= startDate.AddDays(x - 1)).ToList();

                            if (prices.Count == 0)
                                prices = empPriceLst.Where(s => s.Employee == trans.Employee &&
                                                                s.PayrollCategory == trans.PayrollCategory &&
                                                                s.Account == trans.ProjectRef.Account &&
                                                                s.Project == null &&
                                                               (s.ValidTo >= startDate.AddDays(x - 1) || s.ValidTo == DateTime.MinValue) && s.ValidFrom <= startDate.AddDays(x - 1)).ToList();

                            if (prices.Count == 0)
                                prices = empPriceLst.Where(s => s.Employee == trans.Employee &&
                                                                (s.PayrollCategory == trans.PayrollCategory || (defaultPayrollCategory != null && s.PayrollCategory == defaultPayrollCategory.KeyStr)) &&
                                                                s.Account == null &&
                                                                s.Project == null &&
                                                                (s.ValidTo >= startDate.AddDays(x - 1) || s.ValidTo == DateTime.MinValue) && s.ValidFrom <= startDate.AddDays(x - 1)).ToList();
                        }

   
                        if (prices != null && prices.Count() != 0)
                        {
                            salesPrice = trans.Invoiceable ? prices.OrderByDescending(s => s.PrCategory).FirstOrDefault().SalesPrice : 0;
                            costPrice = prices.OrderByDescending(s => s.PrCategory).FirstOrDefault().CostPrice;

                            trans.GetType().GetProperty(string.Format("SalesPriceDay{0}", x)).SetValue(trans, salesPrice);
                            trans.GetType().GetProperty(string.Format("CostPriceDay{0}", x)).SetValue(trans, costPrice);
                        }
                        else
                        {
                            if (trans.Invoiceable)
                                salesPrice = payrollCat._SalesPrice != 0 ? payrollCat._SalesPrice : this.employee._SalesPrice;
                            else
                                salesPrice = 0;

                            costPrice = this.employee._CostPrice;

                            trans.GetType().GetProperty(string.Format("SalesPriceDay{0}", x)).SetValue(trans, salesPrice);
                            trans.GetType().GetProperty(string.Format("CostPriceDay{0}", x)).SetValue(trans, costPrice);
                        }

                        if (validate && foundErr == false)
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
                                if (prices != null && prices.Where(s => defaultPayrollCategory == null || defaultPayrollCategory != null && s.PayrollCategory != defaultPayrollCategory.KeyStr).Count() > 1)
                                {
                                    var emplPriceRecord = prices.Where(s => defaultPayrollCategory == null || defaultPayrollCategory != null && s.PayrollCategory != defaultPayrollCategory.KeyStr).FirstOrDefault();

                                    checkErrors.Add(new TMJournalLineError()
                                    {
                                        Message = string.Format("{0} ({1}:{2}, {3}:{4}, {5}:{6}, {7}:{8}, {9}:{10})",
                                        Uniconta.ClientTools.Localization.lookup("OverlappingPeriodPriceMatrix"),
                                        Uniconta.ClientTools.Localization.lookup("Date"),
                                        startDate.AddDays(x - 1).ToString("dd.MM.yyyy"),
                                        Uniconta.ClientTools.Localization.lookup("Employee"),
                                        emplPriceRecord.Employee,
                                        Uniconta.ClientTools.Localization.lookup("PayrollCategory"),
                                        emplPriceRecord.PayrollCategory,
                                        Uniconta.ClientTools.Localization.lookup("Account"),
                                        emplPriceRecord.Account,
                                        Uniconta.ClientTools.Localization.lookup("Project"),
                                        emplPriceRecord.Project),
                                        RowId = trans.RowId
                                    });

                                    trans.ErrorInfo = string.Empty;
                                    err = true;
                                    foundErr = true;
                                }

                                if (!foundErr && salesPrice == 0 && trans.Invoiceable)
                                {
                                    checkErrors.Add(new TMJournalLineError()
                                    {
                                        Message = string.Format("{0} ({1}:{2})",
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
            }
            catch (Exception ex)
            {
                Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
#endif
        }

        # region Google Maps calculate distance
        public static double GetDistance(string fromAddress, string toAddress, bool avoidFerries = true, int decimals = 1)
        {
            var distance = 0;

            string apiKey = "AIzaSyC1g0oHHskaL5_o059GBCNzKcvhJEsF-Dg";
            string googlemapURL = "https://maps.googleapis.com/maps/api/distancematrix/json?&origins=";

            fromAddress = new String(fromAddress.Select(ch => ch == ' ' || ch == '\n' ? '+' : ch).ToArray());
            toAddress = new String(toAddress.Select(ch => ch == ' ' || ch == '\n' ? '+' : ch).ToArray());

            if (fromAddress == string.Empty || toAddress == string.Empty)
                return 0;

            string requesturl = string.Format("{0}{1}&destinations={2}&mode=driving{3}&key={4}", googlemapURL, fromAddress, toAddress, avoidFerries ? "&avoid=ferries" : string.Empty, apiKey);
            string content = FileGetContents(requesturl);

            JObject o = JObject.Parse(content);
            try
            {
                distance = (int)o.SelectToken("rows[0].elements[0].distance.value");
                return Math.Round((double)distance / 1000, decimals);
            }
            catch
            {
                return distance;
            }
        }
#if !SILVERLIGHT
        private static string FileGetContents(string fileName)
        {
            string sContents = string.Empty;
            try
            {
                System.Net.WebClient wc = new System.Net.WebClient();
                byte[] response = wc.DownloadData(fileName);
                sContents = System.Text.Encoding.ASCII.GetString(response);
            }
            catch
            {
                UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("UnableToConnectTo"), "Google Maps"), Uniconta.ClientTools.Localization.lookup("Error"));
            }

            return sContents;
        }
#else
        private static string FileGetContents(string fileName)
        {
            string sContents = string.Empty;
            //try
            //{
            //    System.Net.WebClient wc = new System.Net.WebClient();
            //    wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(client_DownloadStringCompleted);
            //    wc.DownloadStringAsync(new Uri(fileName, UriKind.RelativeOrAbsolute));
            //    //  sContents = System.Text.Encoding.UTF8.GetString(response);
            //}
            //catch
            //{
            //    UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("UnableToConnectTo"), "Google Maps"), Uniconta.ClientTools.Localization.lookup("Error"));
            //}

            return sContents;
        }
        private static void client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                string response = e.Result;
            }
        }
#endif
        #endregion
    }
}
