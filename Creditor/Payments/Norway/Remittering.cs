using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.API.Plugin;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

namespace Uniconta.WPFClient.Creditor.Payments
{
    public class Remittering: ICreditorPaymentFormatPlugin
    {
        public string Name
        {
          get { return "Remittering"; }
        }

        public FileextensionsTypes FileExtension
        {
            get
            {
                return FileextensionsTypes.TXT;
            }
        }

        public event EventHandler OnExecute;

        public ErrorCodes Execute(UnicontaBaseEntity master,UnicontaBaseEntity record,IEnumerable<UnicontaBaseEntity> source,  string command, string args)
        {
            return ErrorCodes.CouldNotCreate;
        }
        string exceptionMsg;
        public ErrorCodes Generate(List<CreditorTransOpenClient> transactions, CompanyPaymentSetup paymentSetup, Stream stream)
        {
            try
            {
                generateFile(transactions, paymentSetup, stream);
                return ErrorCodes.Succes;
            }
            catch (Exception ex)
            {
                exceptionMsg = ex.Message;
                return ErrorCodes.Exception;
            }
        }

        public void generateFile(List<CreditorTransOpenClient> Trans, CompanyPaymentSetup setup, Stream stream)
        {
            string formatcode = "NY";
            string typeoftransmission = "00";
            string datarecipient = "00008080";

            string recordtype;
            string servicecode;

            string datasender = setup._DataSender; //skal hentes fra opsætning på aftalen i Uniconta, eller muligvis CVR nummeret fra firmaoplysninger.
            string agreementid = setup._AgreementId; //skal hentes fra opsætning på aftalen i Uniconta
            string assignmentaccount = "YYYYYYYYYYY"; //skal hentes fra opsætning på aftalen i Uniconta
            string creditaccount = setup._CreditAccount; //Bankkonto, skal hentes fra opsætning på aftalen i Uniconta eller muligvis fra firmaoplysninger.

            int transmissionnumberint = 1;
            string transmissionnumber = transmissionnumberint.ToString("D7"); //7 karaktere som unikt fortløbende nummer.

            string filler;

            int numberoftransactions = 0;
            int numberofrecords = 0;
            double totalamount = 0;

            DateTime netsdate = DateTime.Now;
            DateTime earliestpaymentdate = DateTime.MaxValue;
            DateTime lastpaymentdate = DateTime.MinValue;
            DateTime paymentdate;

            using (StreamWriter sw = new StreamWriter(stream))
            {
                //2.1 START RECORD FOR TRANSMISSION. Type=10, Page 4
                numberofrecords++;
                servicecode = "00";
                typeoftransmission = "00";
                recordtype = "10";
                filler = 0.ToString("D49");
                sw.WriteLine(formatcode
                                         + servicecode
                                         + typeoftransmission
                                         + recordtype
                                         + datasender
                                         + transmissionnumber
                                         + datarecipient
                                         + filler
                                        );

                //2.2 START RECORD FOR ASSIGNMENT. Type = 20, Page 5
                numberofrecords++;
                servicecode = "04";
                typeoftransmission = "00";
                recordtype = "20";
                filler = 0.ToString("D45");
                sw.WriteLine(formatcode
                                         + servicecode
                                         + typeoftransmission
                                         + recordtype
                                         + agreementid
                                         + transmissionnumber
                                         + assignmentaccount
                                         + filler
                                        );

                foreach (var rec in Trans)
                {
                    //Console.WriteLine("Account={0}, Amount={1}, Date={2}, Forfald={3}, Tekst={4}", rec.Trans._Account, rec._AmountOpen, rec.Trans._Date, rec._DueDate, rec.Trans._Invoice);

                    paymentdate = rec._DueDate;
                    earliestpaymentdate = earliestpaymentdate > paymentdate ? paymentdate : earliestpaymentdate;
                    lastpaymentdate = lastpaymentdate < paymentdate ? paymentdate : lastpaymentdate;
                    int lineamountint = Convert.ToInt32(-rec._AmountOpen * 100);
                    string kid = "";
                    string abbreviatedname = rec.Trans._Account.Substring(0, Math.Min(10, rec.Trans._Account.Length));
                    string internalreference = Convert.ToString(rec.Trans._Invoice);
                    string externalreference = Convert.ToString(rec.Trans._DocumentRef);

                    //2.5 TRANSACTION RECORDS. 
                    //    AMOUNT POSTING 1. Type = 30, Page 6
                    numberoftransactions++;
                    servicecode = "04";
                    typeoftransmission = "02";
                    recordtype = "30";
                    filler = 0.ToString("D6");
                    numberofrecords++;
                    sw.WriteLine(formatcode
                                 + servicecode
                                 + typeoftransmission
                                 + recordtype
                                 + numberoftransactions.ToString("D7")
                                 + paymentdate.ToString("ddMMyy")
                                 + creditaccount
                                 + lineamountint.ToString("D17")
                                 + kid.PadLeft(25, ' ')
                                 + filler
                                );

                    //    AMOUNT POSTING 2. Type = 31, Page 8
                    servicecode = "04";
                    typeoftransmission = "02";
                    recordtype = "31";
                    filler = 0.ToString("D5");
                    numberofrecords++;
                    sw.WriteLine(formatcode
                                 + servicecode
                                 + typeoftransmission
                                 + recordtype
                                 + numberoftransactions.ToString("D7")
                                 + abbreviatedname.PadRight(10, ' ')
                                 + internalreference.PadRight(25, ' ')
                                 + externalreference.PadRight(25, ' ')
                                 + filler
                                );

                    totalamount += -rec._AmountOpen;
                }

                int totalamountint = Convert.ToInt32(totalamount * 100);

                //2.9 END RECORD FOR ASSIGNMENT. Type = 88, Page 14

                typeoftransmission = "00";
                servicecode = "04";
                recordtype = "88";
                filler = 0.ToString("D27");
                sw.WriteLine(formatcode
                                             + servicecode
                                             + typeoftransmission
                                             + recordtype
                                             + numberoftransactions.ToString("D8")
                                             + numberofrecords.ToString("D8")
                                             + totalamountint.ToString("D17")
                                             + earliestpaymentdate.ToString("ddMMyy")
                                             + lastpaymentdate.ToString("ddMMyy")
                                             + filler
                                            );

                numberofrecords++; //Start Record skal ikke tælles med derfor add efter record er skrevet.

                //2.10 END RECORD FOR TRANSMISSION. Type = 89, Page 24
                numberofrecords++;
                servicecode = "00";
                typeoftransmission = "00";
                recordtype = "89";
                filler = 0.ToString("D33");
                sw.WriteLine(formatcode
                                             + servicecode
                                             + typeoftransmission
                                             + recordtype
                                             + numberoftransactions.ToString("D8")
                                             + numberofrecords.ToString("D8")
                                             + totalamountint.ToString("D17")
                                             + netsdate.ToString("ddMMyy")
                                             + filler
                                            );
            }
        }

        public string GetErrorDescription()
        {
            return exceptionMsg;
        }

        public void Intialize()
        {
           
        }

        public void SetAPI(BaseAPI api)
        {
            
        }

        public void SetMaster(List<UnicontaBaseEntity> masters)
        {
            
        }

        public string[] GetDependentAssembliesName()
        {
            return null;
        }
    }
}
