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
    public class BECPayFormat
    {
        public const string TRANSTYPE_ERH356 = "ERH356"; //Domestic Transaction
        public const string TRANSTYPE_ERH351 = "ERH351"; //FIK71
        public const string TRANSTYPE_ERH352 = "ERH352"; //FIK04
        public const string TRANSTYPE_ERH357 = "ERH357"; //FIK73
        public const string TRANSTYPE_ERH358 = "ERH358"; //FIK75
        public const string TRANSTYPE_ERH400 = "ERH400"; //Foreign Transaction


        public static bool GenerateFile(IEnumerable<CreditorTransPayment> paymentList, Company company, CreditorPaymentFormat paymentFormat, SQLCache bankAccountCache, SQLCache creditorCache, bool glJournalGenerated = false)
        {
            var bankStatement = (BankStatement)bankAccountCache.Get(paymentFormat._BankAccount);
            var fileFormat = new CreateBECFileFormatBase();
            var listofBankProperties = new List<DanishFormatFieldBase>();

            foreach (var tran in paymentList)
            {
                var creditor = (Uniconta.DataModel.Creditor)creditorCache.Get(tran.Account);

                if (tran.Paid || tran.OnHold || tran.ErrorInfo != BaseDocument.VALIDATE_OK)
                    continue;

                var newProp = fileFormat.CreateFormatField(tran, paymentFormat, bankStatement, creditor, company, glJournalGenerated);
                if (newProp != null)
                    listofBankProperties.Add(newProp);
            }

            if (listofBankProperties.Count > 0)
            {
                var sfd = UtilDisplay.LoadSaveFileDialog;
                sfd.Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.CSV);

                var userClickedSave = sfd.ShowDialog();
                if (userClickedSave != true)
                    return false;

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
                    return true;
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                }
            }
            else
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecords"), Uniconta.ClientTools.Localization.lookup("Message"));
            }
            return false;
        }
    }
}
