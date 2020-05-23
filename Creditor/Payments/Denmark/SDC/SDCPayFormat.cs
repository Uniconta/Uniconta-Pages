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
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    class SDCPayFormat
    {
        internal const string RECORDTYPE_3 = "3"; //Konto til konto med kort advis
        internal const string RECORDTYPE_K006 = "K006"; //Giroindbetaling kortart 01, 04 og 15
        internal const string RECORDTYPE_K020 = "K020"; //FIK71
        internal const string RECORDTYPE_K073 = "K073"; //FIK73        
        internal const string RECORDTYPE_K075 = "K075"; //FIK75
        internal const string RECORDTYPE_K037 = "K037"; //Overførsel til udlandet

        public static void GenerateFile(IEnumerable<CreditorTransPayment> paymentList, IEnumerable<CreditorTransPayment> paymentListTotal, CrudAPI api,
                                        CreditorPaymentFormat paymentFormat, PaymentReference paymReference, SQLCache bankAccountCache, SQLCache creditorCache, bool glJournalGenerated = false)
        {
            var bankStatement = (BankStatement)bankAccountCache.Get(paymentFormat._BankAccount);
            var fileFormat = new CreateSDCFileFormatBase();
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
                }
                else if ((tran._PaymentMethod != PaymentTypes.IBAN) && (tran._PaymentMethod != PaymentTypes.VendorBankAccount))
                {
                    var newProp = fileFormat.CreateFIKFormatField(tran, paymentFormat, bankStatement, creditor, api.CompanyEntity, glJournalGenerated);

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
                    }
                    else
                    {
                        var newProp = fileFormat.CreateDomesticFormatField(tran, paymentFormat, bankStatement, creditor, api.CompanyEntity, glJournalGenerated);
                        if (newProp != null)
                            listofBankProperties.Add(newProp);
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
                        var sw = new StreamWriter(stream, Encoding.Default);
                        fileFormat.StreamToDomesticFile(listofBankProperties, sw);
                        fileFormat.StreamToForeignFile(listofBankProperties, sw);
                        fileFormat.StreamToFIKFile(listofBankProperties, sw);
                        sw.Flush();
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
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("There are no payments!\nPlease check the System info column."), Uniconta.ClientTools.Localization.lookup("Message"));
            }
        }
    }
}
