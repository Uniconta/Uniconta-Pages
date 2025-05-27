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
using System.Reflection;

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
    }
}

