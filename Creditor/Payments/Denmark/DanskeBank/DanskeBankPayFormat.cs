using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using UnicontaClient.Pages;
using ISO20022CreditTransfer;
using Microsoft.Win32;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaISO20022CreditTransfer;
using System.Linq;
using System.Text;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    class DanskeBankPayFormat
    {
        internal const string TRANSTYPE_CMBO = "CMBO";
        internal const string TRANSTYPE_CMUO = "CMUO";

        public static void GenerateFile(IEnumerable<CreditorTransPayment> paymentList, IEnumerable<CreditorTransPayment> paymentListTotal, CrudAPI api, 
                                        CreditorPaymentFormat paymentFormat, PaymentReference paymReference, SQLCache bankAccountCache, SQLCache creditorCache, bool glJournalGenerated = false)
        {
            var bankStatement = (BankStatement)bankAccountCache.Get(paymentFormat._BankAccount);
            var fileFormat = new CreateDanskeBankFileFormatBase();
            var listofBankProperties = new List<DanishFormatFieldBase>();
            var paymentReference = paymReference;

            foreach (var tran in paymentList)
            {
                var swiftCode = tran._SWIFT ?? string.Empty;
                var countryCode = swiftCode.Length >= 6 ? swiftCode.Substring(4, 2) : string.Empty;
                var creditor = (Uniconta.DataModel.Creditor)creditorCache.Get(tran.Account);

                if (countryCode != string.Empty && countryCode != BaseDocument.COUNTRY_DK)
                {
                    var newProp = fileFormat.CreateForeignFormatField(tran, paymentFormat, bankStatement, creditor, api.CompanyEntity, glJournalGenerated);
                    if (newProp != null)
                        listofBankProperties.Add(newProp);
                }
                else
                {
                    var newProp = fileFormat.CreateDanishFormatField(tran, paymentFormat, bankStatement, creditor, api.CompanyEntity, glJournalGenerated);
                    if (newProp != null)
                        listofBankProperties.Add(newProp);
                }
            }

            if (listofBankProperties.Count > 0)
            {
                var sfd = UtilDisplay.LoadSaveFileDialog;
                sfd.Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.CSV);


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
                        using (var sw = new StreamWriter(stream, Encoding.Default))
                        {
                            fileFormat.StreamToDanishFile(listofBankProperties, sw);
                            fileFormat.StreamToForeignFile(listofBankProperties, sw);
                        }
                        stream.Close();
                    }

                    paymentReference.InsertPaymentReferenceTask(paymentList.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK).ToList(),
                                                                paymentListTotal.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK).ToList(), api, glJournalGenerated);
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                }
            }
            else
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecords"), Uniconta.ClientTools.Localization.lookup("Message"));
            }
        }
    }
}