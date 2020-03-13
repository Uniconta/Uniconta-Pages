using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.Plugin;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;



namespace UnicontaClient.Creditor.Payments
{
    public class NETSNorge
    {
        public static string processString(string txt, int length, bool padleft)
        {
            if (txt == null)
                txt = string.Empty;
            if (txt.Length > length)
                return txt.Substring(0, length);
            if (padleft)
                return txt.PadLeft(length, ' ');
            else
                return txt.PadRight(length, ' ');
        }

        static Encoding FileEncodig;

        static void WriteRecord(StringBuilder sb, Stream stream)
        {
            var toBytes = FileEncodig.GetBytes(sb.ToString());
            stream.Write(toBytes, 0, toBytes.Length);
            sb.Clear();
        }

        public static ErrorCodes GenerateFile(CrudAPI api, SQLCache Creditors, IEnumerable<CreditorTransPayment> queryPaymentTrans, CreditorPaymentFormat selectedSetup, Stream stream)
        {
            try
            {
                if (queryPaymentTrans == null || selectedSetup == null)
                    return ErrorCodes.NoSucces;

                var setup = new CreditorPaymentFormatClientNets();
                StreamingManager.Copy(selectedSetup, setup);

                if (FileEncodig == null)
                    FileEncodig = Encoding.GetEncoding("ISO-8859-1");

                const string formatcode = "NY";
                const string datarecipient = "00008080";

                string recordtype;
                string servicecode;
                string typeoftransmission;

                string datasender = setup._DataSender;
                string agreementid = setup._AgreementId;
                string assignmentaccount = setup._ContractorAccount;

                setup._ShipmentNumber++;
                string transmissionnumber = setup._ShipmentNumber.ToString("D7"); //7 karaktere som unikt fortløbende nummer.

                int numberoftransactions = 0;
                int numberofrecords = 0;
                long totalamountint = 0;

                DateTime netsdate = BasePage.GetSystemDefaultDate();
                DateTime earliestpaymentdate = DateTime.MaxValue;
                DateTime lastpaymentdate = DateTime.MinValue;


                //2.1 START RECORD FOR TRANSMISSION. Type=10, Page 4
                numberofrecords++;
                servicecode = "00";
                typeoftransmission = "00";
                recordtype = "10";

                StringBuilder sb = new StringBuilder(300);
                sb.Append(formatcode).
                    Append(servicecode).
                    Append(typeoftransmission).
                    Append(recordtype).
                    Append(processString(datasender, 8, false)).
                    Append(processString(transmissionnumber, 7, true)).
                    Append(processString(datarecipient, 8, true)).
                    Append('0', 49).
                    AppendLine();

                WriteRecord(sb, stream);

                //2.2 START RECORD FOR ASSIGNMENT. Type = 20, Page 5
                numberofrecords++;
                servicecode = "04";
                typeoftransmission = "00";
                recordtype = "20";

                sb.Append(formatcode).
                   Append(servicecode).
                   Append(typeoftransmission).
                   Append(recordtype).
                   Append(processString(agreementid, 9, false)).
                   Append(processString(transmissionnumber, 7, true)).
                   Append(processString(assignmentaccount, 11, true)).
                   Append('0', 45).
                   AppendLine();

                WriteRecord(sb, stream);

                var Today = DateTime.Today.Date;
                foreach (var rec in queryPaymentTrans)
                {
                    if (rec._PaymentAmount < 0)
                        continue;

                    var paymentdate = rec._DueDate;
                    if (paymentdate < Today)
                        paymentdate = Today;

                    earliestpaymentdate = earliestpaymentdate > paymentdate ? paymentdate : earliestpaymentdate;
                    lastpaymentdate = lastpaymentdate < paymentdate ? paymentdate : lastpaymentdate;
                    var lineamountint = NumberConvert.ToLong(rec._PaymentAmount * 100d);
                    string kid = rec._PaymentId;

                    var Cred = (Uniconta.DataModel.Creditor)Creditors.Get(rec.Account);
                    var abbreviatedname = Cred._Account;
                    var creditaccount = Cred._PaymentId;
                    string internalreference = NumberConvert.ToString(rec.Voucher);
                    string externalreference = rec.InvoiceAN;

                    //2.5 TRANSACTION RECORDS. 
                    //    AMOUNT POSTING 1. Type = 30, Page 6
                    numberoftransactions++;
                    servicecode = "04";

                    if (string.IsNullOrEmpty(kid))
                        typeoftransmission = "02";
                    else
                        typeoftransmission = "12";

                    recordtype = "30";
                    numberofrecords++;

                    sb.Append(formatcode).
                       Append(servicecode).
                       Append(typeoftransmission).
                       Append(recordtype).
                       AppendFormat("{0:D7}", numberoftransactions).
                       AppendFormat("{0:ddMMyy}", paymentdate).
                       Append(processString(creditaccount, 11, true)).
                       AppendFormat("{0:D17}", lineamountint).
                       Append(processString(kid, 25, true)).
                       Append('0', 6).
                       AppendLine();

                    WriteRecord(sb, stream);


                    //    AMOUNT POSTING 2. Type = 31, Page 8
                    servicecode = "04";
                    //typeoftransmission = "02"; Set in privius record
                    recordtype = "31";
                    numberofrecords++;

                    sb.Append(formatcode).
                       Append(servicecode).
                       Append(typeoftransmission).
                       Append(recordtype).
                       AppendFormat("{0:D7}", numberoftransactions).
                       Append(processString(abbreviatedname, 10, false)).
                       Append(processString(internalreference, 25, false)).
                       Append(processString(externalreference, 25, false)).
                       Append('0', 5).
                       AppendLine();

                    WriteRecord(sb, stream);

                    totalamountint += lineamountint;
                }

                //2.9 END RECORD FOR ASSIGNMENT. Type = 88, Page 14

                typeoftransmission = "00";
                servicecode = "04";
                recordtype = "88";

                sb.Append(formatcode).
                    Append(servicecode).
                    Append(typeoftransmission).
                    Append(recordtype).
                    AppendFormat("{0:D7}", numberoftransactions).
                    AppendFormat("{0:D8}", numberofrecords).
                    AppendFormat("{0:D17}", totalamountint).
                    AppendFormat("{0:ddMMyy}", earliestpaymentdate).
                    AppendFormat("{0:ddMMyy}", lastpaymentdate).
                    Append('0', 27).
                    AppendLine();

                WriteRecord(sb, stream);

                numberofrecords++; //Start Record skal ikke tælles med derfor add efter record er skrevet.

                //2.10 END RECORD FOR TRANSMISSION. Type = 89, Page 24
                numberofrecords++;
                servicecode = "00";
                typeoftransmission = "00";
                recordtype = "89";

                sb.Append(formatcode).
                    Append(servicecode).
                    Append(typeoftransmission).
                    Append(recordtype).
                    AppendFormat("{0:D7}", numberoftransactions).
                    AppendFormat("{0:D8}", numberofrecords).
                    AppendFormat("{0:D17}", totalamountint).
                    AppendFormat("{0:ddMMyy}", netsdate).
                    Append('0', 33).
                    AppendLine();

                WriteRecord(sb, stream);

                api.UpdateNoResponse(setup);
                StreamingManager.Copy(setup, selectedSetup);

                return 0;
            }
            catch (Exception ex)
            {
                api.ReportException(ex, string.Format("NETSForge GenerateFile, CompanyId={0}", api.CompanyId));
                return ErrorCodes.Exception;
            }
        }
#if !SILVERLIGHT
        public static void GenerateJournalLines(CrudAPI api, Uniconta.DataModel.GLDailyJournal jour, string filename)
        {
            try
            {
                DateTime date = DateTime.Today.Date;

                double amount = 0;

                string creditAccountRef = "";
                string kid = "";

                List<Uniconta.DataModel.GLDailyJournalLine> JournalLines = new List<Uniconta.DataModel.GLDailyJournalLine>();

                using (StreamReader sr = new StreamReader(filename, System.Text.Encoding.Default, false)) //Ver 02
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Length != 80)
                        {
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InvalidFileFormat"), Uniconta.ClientTools.Localization.lookup("Error"));
                            break;
                        }

                        var formatCode = line.Substring(0, 2);
                        var serviceCode = line.Substring(2, 2);
                        var transType = line.Substring(4, 2);
                        var recordType = line.Substring(6, 2);
                        if (formatCode == "NY")
                        {
                            if (serviceCode == "04")
                            {
                                if (recordType == "30")
                                {
                                    date = DateTime.ParseExact(line.Substring(15, 6), "ddMMyy", CultureInfo.InvariantCulture);
                                    var amountTxt = line.Substring(32, 17);
                                    amount = Convert.ToDouble(amountTxt) / 100d;
                                    creditAccountRef = line.Substring(21, 10);
                                    kid = line.Substring(49, 25);
                                }

                                if (recordType == "31")
                                {
                                    var rec = new Uniconta.DataModel.GLDailyJournalLine();
                                    rec._DCPostType = DCPostType.Payment;
                                    rec._TransType = jour._TransType;
                                    rec._Dim1 = jour._Dim1;
                                    rec._Dim2 = jour._Dim2;
                                    rec._Dim3 = jour._Dim3;
                                    rec._Dim4 = jour._Dim4;
                                    rec._Dim5 = jour._Dim5;
                                    if (amount >= 0)
                                        rec._Debit = amount;
                                    else
                                        rec._Credit = -amount;

                                    rec._Date = date;
                                    rec._Account = creditAccountRef;
                                    rec._AccountType = (byte)GLJournalAccountType.Creditor;

                                    rec._OffsetAccount = jour._OffsetAccount;
                                    rec._OffsetAccountType = (byte)jour._DefaultOffsetAccountType;

                                    string voucherstring = line.Substring(25, 25).Trim();
                                    rec._Settlements = voucherstring;
                                    rec._SettleValue = SettleValueType.Voucher;

                                    rec._Text = line.Substring(15, 10).Trim();
                                    rec._PaymentId = kid.Trim();

                                    rec.SetMaster(jour);
                                    JournalLines.Add(rec);
                                }
                            }
                        }
                        else
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InvalidFileFormat"), Uniconta.ClientTools.Localization.lookup("Information"));
                    }
                }

                if (JournalLines.Count > 0)
                    api.InsertNoResponse(JournalLines);
            }
            catch (Exception ex)
            {
                api.ReportException(ex, string.Format("NETSForge GenerateJournalLines, CompanyId={0}", api.CompanyId));
            }
        }
#endif
    }
}

