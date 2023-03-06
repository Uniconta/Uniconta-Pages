using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.API.GeneralLedger;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.GetVatReport
{
    class GetVatBoxes
    {
        ReportAPI rApi;
        CrudAPI cApi;

        SQLCache accounts;
        SQLCache vats;
        SQLCache vatTypes;
        DateTime fromDate;
        DateTime toDate;

        public GetVatBoxes(BaseAPI api, DateTime from, DateTime to)
        {
            if (api == null)
                throw new ArgumentNullException("api");
            if (from == null)
                throw new ArgumentNullException("fromDate");
            if (to == null)
                throw new ArgumentNullException("toDate");

            rApi = new ReportAPI(api);
            cApi = new CrudAPI(api);
            fromDate = from;
            toDate = to;
        }

        /// <summary>
        /// Retrieves enumerable of VATReportLine objects generated with information from Uniconta. 
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<VatReportLine>> GetVatValues()
        {
            await GetCache();
            List<VatReportLine> output = new List<VatReportLine>();

            FinancialBalance[] financialLines = await GetUnicontaVatReport();
            if (financialLines.Length == 0)
                return null;

            //the vat types with these vat operation codes have their values in the debit field, unlike the others.
            byte[] debitCodes = new byte[] { 3, 4, 5, 6, 7, 103, 104, 105, 106 };

            var vatEntities = vats.GetRecords as GLVat[];
            //var vatClientEntities = vatEntities.Cast<GLVatClient>().ToArray();
            //iterate over each VAT code
            foreach (var entity in vatEntities)
            {   //check whether there is a number in the vat box position UDF in GL > Maintenance > VAT
                if (entity.GetUserFieldInt64(0) == default(long) && entity.GetUserFieldInt64(1) == default(long))
                    //skip vat if not assigned UDF value
                    continue;
                var filteredLines = financialLines.Where(x => x.VatRowId == entity.RowId).ToList();
                if (filteredLines.Count == 0)
                    continue;

                VatReportLine newLine = new VatReportLine();
                //workaround for debit codes
                if (debitCodes.Any(x => x == entity._VatOperationCode))
                    newLine = ProcessDebitVAT(entity, filteredLines);
                else
                    newLine = ProcessStandardVAT(entity, filteredLines);

                if (newLine == null)
                    continue;

                //these values are unused in the rest of the application, they're for making debugging easier
                newLine.VatType = (byte)entity._VatType;
                newLine.Vat = entity;
                newLine.Text = entity.KeyName;
                newLine._VatOperation = entity._VatOperationCode;
                output.Add(newLine);
                //--
                //reset everything, just in case
                filteredLines = null;
                newLine = null;
            }

            return output;
        }

        /// <summary>
        /// Converts list of FinancialBalance objects into a single VATReportLine object, which is tied to a GLVat object.
        /// </summary>
        /// <remarks>
        /// This method only operates on VAT Operations s20,p20,s9,p9,s5, and p5. This is because there is a difference in the location of information
        /// in FinancialBalance objects tied to these particular VAT operations. FinancialBalance objects with 'Standard' VAT operations 
        /// contain the transactional net value in the AmountBase field, and tax value in the Debit field.
        /// </remarks>
        /// <param name="vat">Uniconta VAT object. The UDFs in the VAT object will determine which VAT box the lines will go into.</param>
        /// <param name="lines">List of financial information with net and tax values linked to a particular VAT object.</param>
        /// <returns></returns>
        VatReportLine ProcessStandardVAT(GLVat vat, IEnumerable<FinancialBalance> lines)
        {
            List<FinancialBalance> linesList = lines.ToList();
            for (int i = linesList.Count - 1; i >= 0; i--)
            {
                //check whether there are GL accounts linked to the financial transaction 
                var account = (GLAccount)accounts.Get(linesList[i].AccountRowId);
                if (account == null)
                {
                    linesList.Remove(linesList[i]);
                    continue;
                }

                if (vat._Account != account._Account)
                {
                    //drop if not
                    linesList.Remove(linesList[i]);
                    continue;
                }
            }

            if (linesList.Count() == 0)
                return null;

            VatReportLine newLine = new VatReportLine();
            switch (vat._VatType)
            {
                case GLVatSaleBuy.Sales: //flip sign for sales values since they start negative                   
                    newLine.AmountWithout = Math.Round(-linesList.Sum(x => x.AmountBase), 2);
                    newLine._PostedVAT = Math.Round(-linesList.Sum(x => x.Debit), 2);
                    break;
                case GLVatSaleBuy.Buy:

                    newLine.AmountWithout = Math.Round(linesList.Sum(x => x.AmountBase), 2);
                    newLine._PostedVAT = Math.Round(linesList.Sum(x => x.Debit), 2);
                    break;
                default:
                    break;
            }
            return newLine;
        }
        /// <summary>
        /// Converts list of FinancialBalance objects into a single VATReportLine object, which is tied to a GLVat object.
        /// </summary>
        /// <remarks>
        /// This method operates on all VAT operations not listed in ProcessStandardVAT() documentation.FinancialBalance objects with 'Debit' VAT operations
        /// contain the net value in the Debit field, and the tax value in the Credit field.
        /// </remarks>
        /// <param name="vat">Uniconta VAT object. The UDFs in the VAT object will determine which VAT box the lines will go into.</param>
        /// <param name="lines">List of financial information with net and tax values linked to a particular VAT object.</param>
        /// <returns></returns>
        VatReportLine ProcessDebitVAT(GLVat vat, IEnumerable<FinancialBalance> lines)
        {
            List<FinancialBalance> linesList = lines.ToList();
            VatReportLine newLine = new VatReportLine();
            switch (vat._VatType)
            {
                case GLVatSaleBuy.Sales: //flip sign for sales values since they start negative                   
                    newLine.AmountWithout = Math.Round(-linesList.Sum(x => x.Debit), 2);
                    newLine._PostedVAT = Math.Round(-linesList.Sum(x => x.Credit), 2);
                    break;
                case GLVatSaleBuy.Buy:
                    if (!string.IsNullOrEmpty(vat._OffsetAccount))
                    {
                        var positiveEntries = linesList.Where(x => x.Debit > 0 && x.Credit > 0);
                        newLine.AmountWithout = Math.Round(positiveEntries.Sum(x => x.Debit), 2);
                        newLine._PostedVAT = Math.Round(positiveEntries.Sum(x => x.Credit), 2);
                    }
                    else
                    {
                        newLine.AmountWithout = Math.Round(linesList.Sum(x => x.Debit), 2);
                        newLine._PostedVAT = Math.Round(linesList.Sum(x => x.Credit), 2);
                    }
                    break;
                default:
                    break;
            }
            return newLine;
        }
        async Task<FinancialBalance[]> GetUnicontaVatReport()
        {
            return await rApi.VatCodeSum(fromDate, toDate);
        }

        /// <summary>
        /// <see langword="async"/> Loads cache if null. 
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        async Task GetCache()
        {
            Company company;
            if (rApi != null)
                company = rApi.CompanyEntity;
            else
                throw new NullReferenceException("ReportAPI is null.");

            //loads caches if null
            accounts = company.GetCache(typeof(GLAccount)) ?? await company.LoadCache(typeof(GLAccount), cApi);
            vats = company.GetCache(typeof(GLVat)) ?? await company.LoadCache(typeof(GLVat), cApi);
            vatTypes = company.GetCache(typeof(GLVatType)) ?? await company.LoadCache(typeof(GLVatType), cApi);
        }
    }
}
