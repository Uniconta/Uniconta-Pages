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
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaISO20022CreditTransfer;
using System.Linq;
using System.Text;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{

    public class NordeaPaymentFormat
    {
        internal const string TRANSTYPE_45 = "45"; //Domestic Transaction
        internal const string TRANSTYPE_46 = "46"; //FIK Transaction
        internal const string TRANSTYPE_49 = "49"; //Standard foreign transaction

        internal const string EXPENSECODE_BOTH = "N";
        internal const string TEXTCODE_SHORTADVICE = "100";

        public static bool GenerateFile(IEnumerable<CreditorTransPayment> paymentList, Company company, 
                                        CreditorPaymentFormat paymentFormat, SQLCache bankAccountCache, SQLCache creditorCache, bool glJournalGenerated = false)
        {
            var bankStatement = (BankStatement)bankAccountCache.Get(paymentFormat._BankAccount);
            var fileFormat = new CreateNordeaFileFormatBase();
            var listofNordeaProperties = new List<DanishFormatFieldBase>();
            
            foreach (var tran in paymentList)
            {
                var creditor = (Uniconta.DataModel.Creditor)creditorCache.Get(tran.Account);

                var newProp = fileFormat.CreateFormatField(tran, paymentFormat, bankStatement, creditor, company, glJournalGenerated);
                if (newProp != null)
                    listofNordeaProperties.Add(newProp);
            }

            if (listofNordeaProperties.Count > 0)
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
                        var sw = new StreamWriter(stream, Encoding.ASCII);
                        fileFormat.StreamToFile(listofNordeaProperties, sw);
                        sw.Flush();
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