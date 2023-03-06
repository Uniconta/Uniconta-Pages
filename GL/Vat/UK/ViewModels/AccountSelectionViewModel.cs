using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using UnicontaClient.Pages.GL.Vat.UK.GetVatReport;
using UnicontaClient.Pages.GL.Vat.UK.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.ViewModels
{
    class AccountSelectionViewModel : INotifyPropertyChanged
    {
        CrudAPI _crudApi;
        #region ctor
        public AccountSelectionViewModel(string vatNo, CrudAPI crudAPI)
        {
            _crudApi = crudAPI;
            vrn = vatNo;
            FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime nextMonth = FromDate.AddMonths(1);
            ToDate = nextMonth.AddDays(-1);
            HMRCFromDate = new DateTime(DateTime.Today.Year, 1, 1);
            HMRCToDate = new DateTime(DateTime.Today.Year, 12, 31);
        }
    
        #endregion

        #region properties
        bool isBusy;
        VatObligationsDetailsModel[] obligations;
        string vrn;
        DateTime fromDate = DateTime.Today;
        DateTime toDate = DateTime.Today;
        DateTime hmrcFromDate = DateTime.Today;
        DateTime hmrcToDate = DateTime.Today;
        public VatReturnModel Model { get; set; } = new VatReturnModel();
        public bool IsBusy
        {
            get
            {
                return isBusy;
            }
            set
            {
                isBusy = value;
                NotifyPropertyChanged("IsBusy");
            }
        }
        public VatObligationsDetailsModel[] Obligations
        {
            get { return obligations; }
            set
            {
                obligations = value;
                NotifyPropertyChanged("Obligations");
            }
        }

        public DateTime FromDate
        {
            get
            { return fromDate; }
            set
            {
                fromDate = value;
                NotifyPropertyChanged("FromDate");
            }
        }
        public DateTime ToDate
        {
            get
            { return toDate; }
            set
            {
                toDate = value;
                NotifyPropertyChanged("ToDate");
            }
        }
        public DateTime HMRCFromDate
        {
            get
            { return hmrcFromDate; }
            set
            {
                hmrcFromDate = value;
                NotifyPropertyChanged("HMRCFromDate");
            }
        }
        public DateTime HMRCToDate
        {
            get
            { return hmrcToDate; }
            set
            {
                hmrcToDate = value;
                NotifyPropertyChanged("HMRCToDate");
            }
        }
        #endregion

        #region SubmitVatCommand
        public DelegateCommand<Window> SubmitCommand
        {
            get { return new DelegateCommand<Window>(Submit); }
        }

        /// <summary>
        /// Submits information from the model to HMRC.
        /// </summary>
        /// <param name="window"></param>
        async void Submit(Window window)
        {
            if (string.IsNullOrEmpty(Model.PeriodKey))
            {
                UnicontaMessageBox.Show("Please retrieve your VAT obligations and select one before trying again.", "No Obligation Selected", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //confirmation box
            MessageBoxResult confirmation = MessageBox.Show("When you submit this VAT information you are making a legal declaration that the information is true and complete. A false declaration can result in prosecution. Are you sure you wish to continue? ", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            switch (confirmation)
            {
                case MessageBoxResult.Yes:
                    Model.Finalised = true;
                    break;
                default:
                    return;
            }
            //convert to json
            var jsonModel = JsonConvert.SerializeObject(Model);
            try
            {
                IsBusy = true;
                HMRCConnection.HMRCConnection http = new HMRCConnection.HMRCConnection(vrn, false);
                await http.Post(jsonModel);

            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.Flatten().InnerExceptions)

                {
                    if (e is TaskCanceledException)
                        UnicontaMessageBox.Show("VAT Return cannot continue without your authorisation at HMRC.Please try again.", "Canceled");
                    if (e is TimeoutException)
                        UnicontaMessageBox.Show("Request to HMRC timed out. Try again later.", "Request timed out");
                    if (e is WebException)
                        UnicontaMessageBox.Show(e.Message, "Web Exception");
                    if (e is JsonException)
                        UnicontaMessageBox.Show("An error has occured during conversion of JSON content to or from HMRC. Message: " + e.Message, "JSON Exception");
                    if (e is Exception)
                        UnicontaMessageBox.Show("An unexpected error has occurred. Error details: " + e.Message + "\nStack Trace: " + e.StackTrace, "Unhandled Exception");

                    window.Close();
                    return;
                }
            }
            catch (WebException we)
            {
                MessageBox.Show(we.Message, "Web Exception");
                return;
            }
            finally
            {
                IsBusy = false;
            }

            window?.Close();
        }
        #endregion

        #region CancelCommand
        public DelegateCommand<Window> CancelCommand
        {
            get { return new DelegateCommand<Window>(Cancel); }
        }
        void Cancel(Window window)
        {
            window.Close();
        }
        #endregion

        #region GetVatCommand
        public DelegateCommand GetVatCommand
        {
            get { return new DelegateCommand(GetVat); }
        }

        /// <summary>
        /// Retrieves list of VatReportLine objects and assigns them to their respective VAT 100 box.
        /// </summary>
        async void GetVat()
        {
            if (FromDate == DateTime.MinValue || ToDate == DateTime.MinValue)
            {
                UnicontaMessageBox.Show("From date or to date cannot be empty. Select a date for both and try again.", "Error");
                return;
            }
            if (ToDate < FromDate)
            {
                UnicontaMessageBox.Show("'To date' cannot be earlier than 'from date'.", "Error");
                return;
            }
            if (CheckForExistingData())
            {
                var result = MessageBox.Show("Existing data has been detected in one or more boxes and will be overriden. Do you wish to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        ResetModelVatProperties();
                        break;
                    default:
                        return;
                }
}
try
{
//sets cursor to busy
                IsBusy = true;

                GetVatBoxes vat = new GetVatBoxes(_crudApi, FromDate, ToDate);

                var vatEnumerable = await Task.Run(() => vat.GetVatValues());

                if (vatEnumerable == null)
                {
                    UnicontaMessageBox.Show("Could not find any records between " + FromDate.ToShortDateString() + " and " + toDate.ToShortDateString() + ".", "Transactions Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                List<VatReportLine> vatList = vatEnumerable.ToList();
                AssignValues(vatList, 0);
                AssignValues(vatList, 1);
                Model.CalculateTotals();
            }
            catch (ArgumentNullException ane)
            {
                UnicontaMessageBox.Show("An argument is null. Argument name: " + ane.ParamName, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (IndexOutOfRangeException ioore)
            {
                UnicontaMessageBox.Show("Could not find box position specified in VAT " + ioore.Message + ". Please check that this VAT type is setup correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (ArgumentOutOfRangeException)
            {
                UnicontaMessageBox.Show("Error with checking VAT Box numbers. Check that you have the correct setup for User-defined fields in General Ledger > VAT.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            catch (Exception e)
            {
                UnicontaMessageBox.Show("Unhandled exception. Message:" + e.Message + Environment.NewLine + "Source: " + e.Source + Environment.NewLine + "Stack Trace: " + e.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region GetVat Auxillaries
        /// <summary>
        /// Checks the properties in the model with a BoxNumberAttribute for any non-zero number values.
        /// </summary>
        /// <returns>True if any property contains non-zero value.</returns>
        bool CheckForExistingData()
        {
            var props = GetModelVatProperties();

            foreach (var prop in props)
            {
                var reflValue = prop.GetValue(Model);
                double? number = reflValue as double?;
                if (number == null)
                    throw new ArgumentException("Value" + reflValue.ToString() + "could not be converted to a number.");

                if (number != default(double))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// queries model using Reflection for properties with a Box Number attribute.
        /// </summary>
        /// <returns>Enumerable of PropertyInfo objects.</returns>
        IEnumerable<PropertyInfo> GetModelVatProperties()
        {
            return Model.GetType()
                .GetProperties()
                .Where(x => Attribute.IsDefined(x, typeof(BoxNumberAttribute)));
        }

        /// <summary>
        /// Sets all properties in the model with a BoxNumberAttribute to zero.
        /// </summary>
        void ResetModelVatProperties()
        {
            var props = GetModelVatProperties();
            foreach (var prop in props)
            {
                prop.SetValue(Model, 0d);
            }
        }
        /// <summary>
        /// Assigns valeus from VAT report to the Vat return data model.
        /// </summary>
        /// <param name="input">Vat report with transactional data.</param>
        /// <param name="userField">Flag for assigning either tax or net values.</param>
        /// <remarks>Method will assume that it is assigning the posted tax if the userField is 0, amount without VAT if userField is 1.</remarks>
        void AssignValues(List<VatReportLine> input, int userField)
        {

            //exclusive-nor
            if ((userField == 0) == (userField == 1))
                throw new ArgumentOutOfRangeException("userField");

            foreach (var line in input)
            {
                //find vat and box position number from UDF
                var clientVat = line.Vat as GLVatClient;
                long boxNumber = clientVat.GetUserFieldInt64(userField);

                if (boxNumber == 0 || boxNumber == default(long))
                    continue;

                //assign to property in model by its number
                PropertyInfo currentProp = GetPropertyByBoxNumber(boxNumber);
                if (currentProp == null)
                {
                    throw new IndexOutOfRangeException(clientVat.Vat);
                }
                //set value of property in model, depending on userfield flag
                double tempValue = 0;
                switch (userField)
                {
                    case 0:
                        tempValue = (double)currentProp.GetValue(Model);
                        currentProp.SetValue(Model, tempValue + line.PostedVAT);
                        break;
                    case 1:
                        tempValue = (double)currentProp.GetValue(Model);
                        currentProp.SetValue(Model, tempValue + line.AmountWithout);
                        break;
                    default:
                        return;
                }

            }
        }

        /// <summary>
        /// Finds property in VAT return model via property assigned to it, comparing to the input.
        /// </summary>
        /// <param name="number">Long int that corresponds to target box number.</param>
        /// <returns>Property Info object corresponding to the property. Returns null if none were found.</returns>
        PropertyInfo GetPropertyByBoxNumber(long number)
        {
            //find all props in data model
            var props = GetModelVatProperties();

            //iterate through them
            foreach (var prop in props)
            {
                var attr = (BoxNumberAttribute)Attribute.GetCustomAttribute(prop, typeof(BoxNumberAttribute));
                if (attr.BoxNumber == number)
                    return prop;
            }
            return null;
        }
        #endregion

        #endregion

        #region GetObligationsCommand
        public DelegateCommand GetObligationsCommand
        {
            get { return new DelegateCommand(GetObligations); }
        }

        async void GetObligations()
        {
            HMRCConnection.HMRCConnection http = new HMRCConnection.HMRCConnection(vrn, false);
            DateTime from = hmrcFromDate;
            DateTime to = hmrcToDate;
            string returnJson;
            try
            {
                returnJson = await http.Get(from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"));
            }
            catch (TaskCanceledException)
            {
                UnicontaMessageBox.Show("Authentication has been cancelled. The operation cannot continue.", "Cancelled");
                return;
            }
            catch (WebException we)
            {
                UnicontaMessageBox.Show(we.Message, "Web Exception");
                return;
            }
            catch (ArgumentException ae)
            {
                UnicontaMessageBox.Show(ae.Message, "Argument Exception");
                return;
            }
            catch (Exception e)
            {
                UnicontaMessageBox.Show("Unhandled exception: " + e.Message + "Stack Trace: " + e.StackTrace + "Source: " + e.Source, "Information");
                return;
            }

            if (string.IsNullOrEmpty(returnJson))
                return;

            var deserialised = JsonConvert.DeserializeObject<VatObligationsModel>(returnJson);

            Obligations = deserialised.Obligations;
        }
        #endregion

        #region SetObligationsCommand
        public DelegateCommand<VatObligationsDetailsModel> SetObligationsCommand
        {
            get { return new DelegateCommand<VatObligationsDetailsModel>(SetObligations); }
        }
        void SetObligations(VatObligationsDetailsModel model)
        {

            var cto = new HMRCConnection.CreateTestOrg();
            cto.CreateNewOrg();
            if (model == null)
            {
                MessageBox.Show("No obligation was selected, or an error occured.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //if (model.Status == "Fulfilled")
            //{
            //    MessageBox.Show("The obligation you have selected has already been finalised.");
            //    return;
            //}

            FromDate = model.Start;
            ToDate = model.End;
            Model.PeriodKey = model.PeriodKey;
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
