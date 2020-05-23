using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UnicontaClient.Pages;
using Microsoft.Win32;
using Uniconta.API.System;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Creditor.Payments;
using UnicontaISO20022CreditTransfer;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    class BankDataPayFormat
    {
        internal const string INDEX01 = "0001";
        internal const string INDEX02 = "0002";


        internal const string TRANSTYPE_IB000000000000 = "IB000000000000";
        internal const string TRANSTYPE_IB999999999999 = "IB999999999999";
        internal const string TRANSTYPE_IB030202000005 = "IB030202000005";
        internal const string TRANSTYPE_IB030204000003 = "IB030204000003";
        internal const string TRANSTYPE_IB030207000002 = "IB030207000002";

        internal const int FOREIGN_STANDARDTRANSFER = 53;
        internal const int FOREIGN_SEPATRANSFER = 97;

        public static void GenerateFile(IEnumerable<CreditorTransPayment> paymentList, IEnumerable<CreditorTransPayment> paymentListTotal, CrudAPI api, 
                                        CreditorPaymentFormat paymentFormat, PaymentReference paymReference, SQLCache bankAccountCache, SQLCache creditorCache, bool glJournalGenerated = false)
        {
            var bankStatement = (BankStatement)bankAccountCache.Get(paymentFormat._BankAccount);
            var fileFormat = new CreateBankDataFileFormatBase();
            var listofBankProperties = new List<DanishFormatFieldBase>();
            var paymentReference = paymReference;

            foreach (var tran in paymentList)
            {
                var creditor = (Uniconta.DataModel.Creditor)creditorCache.Get(tran.Account);

                if (tran._PaymentMethod == PaymentTypes.IBAN)
                {
                    var newProp = fileFormat.CreateForeignFormatField(tran, paymentFormat, bankStatement, creditor, api.CompanyEntity, glJournalGenerated);
                    if (newProp != null)
                        listofBankProperties.Add(newProp);

                    var newProp2 = fileFormat.SecondaryCreateForeignFormatField(tran, paymentFormat, bankStatement, creditor, api.CompanyEntity, glJournalGenerated);
                    if (newProp != null)
                        listofBankProperties.Add(newProp2);
                }
                else if ((tran._PaymentMethod != PaymentTypes.IBAN) && (tran._PaymentMethod != PaymentTypes.VendorBankAccount))
                {
                    var newProp = fileFormat.CreateIndbetalingskortFormatField(tran, paymentFormat, bankStatement, creditor, api.CompanyEntity, glJournalGenerated);

                    if (newProp != null)
                        listofBankProperties.Add(newProp);
                }
                else if (tran._PaymentMethod == PaymentTypes.VendorBankAccount)
                {
                    var swiftCode = tran._SWIFT ?? string.Empty;
                    var countryCode = swiftCode.Length >= 6 ? swiftCode.Substring(4, 2) : string.Empty;

                    if (countryCode != string.Empty && countryCode != BaseDocument.COUNTRY_DK)
                    {
                        var newProp = fileFormat.CreateForeignFormatField(tran, paymentFormat, bankStatement, creditor, api.CompanyEntity, glJournalGenerated);
                        if (newProp != null)
                            listofBankProperties.Add(newProp);

                        var newProp2 = fileFormat.SecondaryCreateForeignFormatField(tran, paymentFormat, bankStatement, creditor, api.CompanyEntity, glJournalGenerated);
                        if (newProp != null)
                            listofBankProperties.Add(newProp2);
                    }
                    else
                    {
                        var newProp = fileFormat.CreateDomesticFormatField(tran, paymentFormat, bankStatement, creditor, api.CompanyEntity, glJournalGenerated);
                        if (newProp != null)
                            listofBankProperties.Add(newProp);

                        var newProp2 = fileFormat.SecondaryCreateDomesticFormatField(tran, paymentFormat, bankStatement, api.CompanyEntity, glJournalGenerated);
                        if (newProp != null)
                            listofBankProperties.Add(newProp2);
                    }
                }
            }

            if (listofBankProperties.Count > 0)
            {
                var sfd = UtilDisplay.LoadSaveFileDialog;
                sfd.Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.TXT);

                var userClickedSave = sfd.ShowDialog();
                if (userClickedSave != true)
                    return;

                try
                {
#if !SILVERLIGHT
                    using (var stream = File.Create(sfd.FileName))
#else
                    using (var stream = sfd.OpenFile())
#endif
                    {
                        using (var sw = new StreamWriter(stream, Encoding.GetEncoding(1252)))
                        {
                            CreateStartAndEnd(listofBankProperties, sw, true);
                            fileFormat.StreamToDomesticFile(listofBankProperties, sw);
                            fileFormat.StreamToForeignFile(listofBankProperties, sw);
                            fileFormat.StreamToIndbetalingskortFile(listofBankProperties, sw);
                            CreateStartAndEnd(listofBankProperties, sw, false);
                        }
                        stream.Close();
                    }

                    paymentReference.InsertPaymentReferenceTask(paymentList.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK).ToList(), 
                                                                paymentListTotal.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK).ToList(), api, glJournalGenerated);
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                }
            }
            else
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("There are no payments!\nPlease check the System info column."), Uniconta.ClientTools.Localization.lookup("Warning"));
            }
        }

        public static void CreateStartAndEnd(List<DanishFormatFieldBase> listofBankProperties, StreamWriter sw, bool isStartOrEnd)
        {
            var endObject = new BankDataFormatFields();

            endObject.TransferDate = BasePage.GetSystemDefaultDate().Date;

            var listOfBankData = listofBankProperties.OfType<BankDataFormatFields>().Where(bankData => bankData.Index == INDEX01).ToList();
            endObject.TotalPayments = listOfBankData.Count;

            var totalAmount = listofBankProperties.OfType<BankDataFormatFields>().Aggregate(0.0, (current, bp) => current + bp.AmountLong);
            var lineamountint = NumberConvert.ToLong(totalAmount);
            endObject.TotalAmount = lineamountint;

            string stringWithEmpty;
            if (isStartOrEnd)
            {
                endObject.TransTypeCommand = TRANSTYPE_IB000000000000;
                stringWithEmpty = new string(' ', 90);
            }
            else
            {
                endObject.TransTypeCommand = TRANSTYPE_IB999999999999;
                stringWithEmpty = new string(' ', 64);
            }
            
            endObject.OtherTransfers = new List<string>()
            {
                stringWithEmpty,
                new string(' ', 255),
                new string(' ', 255),
                new string(' ', 255)
            };

            const char seperator = ',';

            sw.Write("\"{0}\",", endObject.TransTypeCommand);
            sw.Write("\"{0:yyyyMMdd}\"", endObject.TransferDate);
            if (!isStartOrEnd)
            {
                sw.Write(seperator);
                sw.Write("\"{0:D6}\",", endObject.TotalPayments);
                sw.Write("\"{0:D13}+\"", endObject.TotalAmount);
            }
            foreach (var oT in endObject.OtherTransfers)
            {
                sw.Write(seperator);
                sw.Write('"');
                sw.Write(oT);
                sw.Write('"');
            }
            sw.WriteLine();
        }
    }
}
