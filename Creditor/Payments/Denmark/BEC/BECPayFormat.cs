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
using Uniconta.DataModel;
using UnicontaISO20022CreditTransfer;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    class BECPayFormat
    {
        internal const string TRANSTYPE_ERH356 = "ERH356"; //Domestic Transaction
        internal const string TRANSTYPE_ERH351 = "ERH351"; //FIK71
        internal const string TRANSTYPE_ERH352 = "ERH352"; //FIK04
        internal const string TRANSTYPE_ERH357 = "ERH357"; //FIK73
        internal const string TRANSTYPE_ERH358 = "ERH358"; //FIK75
        internal const string TRANSTYPE_ERH400 = "ERH400"; //Foreign Transaction


        public static void GenerateFile(IEnumerable<CreditorTransPayment> paymentList, IEnumerable<CreditorTransPayment> paymentListTotal, CrudAPI api, CreditorPaymentFormat paymentFormat, PaymentReference paymReference, SQLCache bankAccountCache, SQLCache creditorCache, bool glJournalGenerated = false)
        {
            var bankStatement = (BankStatement)bankAccountCache.Get(paymentFormat._BankAccount);
            var fileFormat = new CreateBECFileFormatBase();
            var listofBankProperties = new List<DanishFormatFieldBase>();
            var paymentReference = paymReference;

            foreach (var tran in paymentList)
            {
                var creditor = (Uniconta.DataModel.Creditor)creditorCache.Get(tran.Account);

                if (tran.Paid || tran.OnHold || tran._ErrorInfo != BaseDocument.VALIDATE_OK)
                    continue;

                var newProp = fileFormat.CreateFormatField(tran, paymentFormat, bankStatement, creditor, api.CompanyEntity, glJournalGenerated);
                if (newProp != null)
                    listofBankProperties.Add(newProp);
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
                        using (var sw = new StreamWriter(stream, Encoding.GetEncoding(1252)))
                        {
                            fileFormat.StreamToFile(listofBankProperties, sw);
                        }
                        stream.Close();
                    }

                    paymentReference.InsertPaymentReferenceTask(paymentList.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK).ToList(),
                                                                paymentListTotal.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK).ToList(), api, glJournalGenerated);
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"));
                }
            }
            else
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecords"), Uniconta.ClientTools.Localization.lookup("Message"));
            }
        }
    }
}
