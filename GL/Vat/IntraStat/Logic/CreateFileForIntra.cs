using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using UnicontaClient.Creditor.Payments;
using Localization = Uniconta.ClientTools.Localization;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    class CreateFileForIntra
    {
        static String companyRegNr;

#if !SILVERLIGHT
        public static List<IntrastatClient> CreateIntraStatfile(List<IntrastatClient> listOfIntraStat, CrudAPI api)
#else
        public static List<IntrastatClient> CreateIntraStatfile(List<IntrastatClient> listOfIntraStat, CrudAPI api, System.Windows.Controls.SaveFileDialog sfd)
#endif
        {
            List<IntrastatClient> listOfImport = new List<IntrastatClient>();
            List<IntrastatClient> listOfExport = new List<IntrastatClient>();
            companyRegNr = "";

            List<IntrastatClient> listToReturn = new List<IntrastatClient>();
            List<IntrastatClient> listOfNotValidated = new List<IntrastatClient>();


            var tempImpList = listOfIntraStat.Where(a => a.ImportOrExport == CreateIntraStatFilePage.ImportOrExportIntrastat.Import).ToList();
            var tempExppList = listOfIntraStat.Where(a => a.ImportOrExport == CreateIntraStatFilePage.ImportOrExportIntrastat.Export).ToList();

            foreach (var c in api.CompanyEntity._Id.ToCharArray())
            {
                if (Char.IsDigit(c))
                {
                    companyRegNr = companyRegNr + c;
                }
            }

            if (string.IsNullOrWhiteSpace(companyRegNr))
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("MissingAccountVATNr"), Uniconta.ClientTools.Localization.lookup("Error"));
                return null;
            }

            if (tempImpList.Count > 999)
            {
                listToReturn.AddRange(tempImpList.GetRange(998, Int32.MaxValue));
                tempImpList.RemoveRange(998, Int32.MaxValue);
            }
            if (tempExppList.Count > 999)
            {
                listToReturn.AddRange(tempExppList.GetRange(998, Int32.MaxValue));
                tempExppList.RemoveRange(998, Int32.MaxValue);
            }

            var countOfPost = 1;
            var isRec2Created = false;
            var compress = Localization.lookup("Compress");

            if (tempImpList != null || tempImpList.Count > 0)
            {
                foreach (var intrastat in tempImpList)
                {

                    if (!intrastat.systemInfo.Contains(compress) && !intrastat.systemInfo.Contains(Localization.lookup("Ok")) && !string.IsNullOrWhiteSpace(intrastat.systemInfo))
                    {
                        listOfNotValidated.Add(intrastat);
                        continue;
                    }

                    if (!intrastat.systemInfo.Contains(compress) && isRec2Created ==false)
                    {
                        var importItem = new IntrastatClient();
                        importItem.companyRegNr = companyRegNr;

                        while (importItem.companyRegNr.Length <= 7)
                            importItem.companyRegNr = "0" + importItem.companyRegNr;

                        isRec2Created = true;

                        listOfImport = new List<IntrastatClient>();
                        importItem.recNr = "02";
                        importItem.importOrExport = CreateIntraStatFilePage.ImportOrExportIntrastat.Import;

                        importItem.itemAmount = tempImpList.Count.ToString();
                        importItem.itemAmount = importItem.itemAmount.Length != 3
                            ? importItem.itemAmount.PadLeft(3, '0')
                            : importItem.itemAmount;

                        importItem.interntRefNrForAll = new string('0', 10);
                        importItem.monthAndYearOfDate = intrastat.MonthAndYearOfDate;
                        importItem.filler = new string(' ', 52);
                        listOfImport.Insert(0, importItem);
                    }

                    intrastat.itemCount = countOfPost;
                    countOfPost++;

                    intrastat.interntRefNr =
                        NETSNorge.processString(intrastat.itemIntra + intrastat.itemNameIntra, 10, false);
                    listOfImport.Add(intrastat);
                }
            }

            countOfPost = 1;
            isRec2Created = false;

            if (tempExppList != null || tempExppList.Count > 0)
            {
                foreach (var intrastat in tempExppList)
                {
                    if (!intrastat.systemInfo.Contains(compress) && !intrastat.systemInfo.Contains(Localization.lookup("Ok")) && !string.IsNullOrWhiteSpace(intrastat.systemInfo))
                    {
                        listOfNotValidated.Add(intrastat);
                        continue;
                    }

                    if (!intrastat.systemInfo.Contains(compress) && isRec2Created == false)
                    {
                        var exportItem = new IntrastatClient();
                        listOfExport = new List<IntrastatClient>();

                        exportItem.companyRegNr = companyRegNr;

                        while (exportItem.companyRegNr.Length <= 7)
                            exportItem.companyRegNr = "0" + exportItem.companyRegNr;

                        isRec2Created = true;

                        exportItem.recNr = "02";
                        exportItem.importOrExport = CreateIntraStatFilePage.ImportOrExportIntrastat.Export;

                        exportItem.itemAmount = tempExppList.Count.ToString();
                        exportItem.itemAmount = exportItem.itemAmount.Length != 3
                            ? exportItem.itemAmount.PadLeft(3, '0')
                            : exportItem.itemAmount;

                        exportItem.interntRefNrForAll = new string('0', 10);
                       
                        exportItem.monthAndYearOfDate = intrastat.MonthAndYearOfDate;
                        exportItem.filler = new string(' ', 52);
                        listOfExport.Insert(0, exportItem);
                    }
                    intrastat.itemCount = countOfPost;
                    countOfPost++;

                    intrastat.interntRefNr = NETSNorge.processString(intrastat.itemIntra + intrastat.itemNameIntra, 10, false);

                    listOfExport.Add(intrastat);
                }
            }

            if (listOfImport.Count <= 0 && listOfExport.Count <= 0)
            {
                UnicontaMessageBox.Show(Localization.lookup("NoRecordExport"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return null;
            }

            try
            {
#if !SILVERLIGHT
                var sfd = UtilDisplay.LoadSaveFileDialog;
                sfd.Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.TXT);
                var userClickedSave = sfd.ShowDialog();

                if (userClickedSave != true)
                    return null;
#endif

#if !SILVERLIGHT
                using (var stream = File.Create(sfd.FileName))
#else
                using (var stream = sfd.OpenFile())
#endif
                {
#if !SILVERLIGHT
                    using (var sw = new StreamWriter(stream, Encoding.Default))
#else
                    using (var sw = new StreamWriter(stream))
#endif
                    {
                        CreateAndStreamFirstAndLast(listOfImport, sw, true, api);
                        listOfImport.AddRange(listOfExport);
                        StreamToFile(listOfImport, sw);
                        CreateAndStreamFirstAndLast(listOfImport, sw, false, api);
                    }
                    stream.Close();
                }
               
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                return null;
            }

            if (listToReturn != null && listToReturn.Count > 0)
                listToReturn.InsertRange(0, listOfNotValidated);

                return listToReturn;
        }

        public static void StreamToFile(List<IntrastatClient> listOfImportExport, StreamWriter sw)
        {
            var type = new IntrastatClient();

            var outputFields = new[]
            {
                "recNr", "itemAmount", "interntRefNrForAll", "companyRegNr", "importOrExport", "monthAndYearOfDate", "filler",
            };


            var outputFields2 = new[]
            {
                "recNr", "itemCount", "interntRefNr", "euCountry", "transType", "zeroes", "zeroes",
                "itemCode", "zeroes", "netWeight", "additionalAmount", "invoiceAmount", "filler",
            };

            var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();
            var fields2 = outputFields2.Select(fld => type.GetType().GetField(fld)).ToList();


            foreach (var impAndExp in listOfImportExport)
            {
                if (impAndExp.RecNr == "02")
                {
                    foreach (var f in fields)
                    {
                        var val = f.GetValue(impAndExp);
                        string value;

                        if (val is DateTime)
                        {
                            value = ((DateTime)val).ToString("yyMM");
                        }
                        else if (f.Name == "importOrExport")
                        {
                            var enumImpOrExp = (CreateIntraStatFilePage.ImportOrExportIntrastat)val;
                            value = ((int)enumImpOrExp).ToString();
                        }
                        else
                        {
                            value = Convert.ToString(val);
                        }
                        value = Regex.Replace(value, "[\"\';]", " ");
                        sw.Write(value);
                    }
                }
                else
                {
                    foreach (var f in fields2)
                    {
                        var val = f.GetValue(impAndExp);
                        string value;

                        if (val is double)
                        {
                            var doubleNumber = (double)val;

                            if (f.Name == "netWeight")
                            {
                                if (0 < doubleNumber && doubleNumber < 1)
                                {
                                    doubleNumber = 1;
                                }
                            }
                            value = NumberConvert.ToLong(Math.Abs(doubleNumber)).ToString();
                            value = value.PadLeft(f.Name == "additionalAmount" ? 10 : 15, '0');
                        }
                        else switch (f.Name)
                            {
                                case "itemCount":
                                    value = ((int)val).ToString();
                                    while (value.Length < 3)
                                    {
                                        value = "0" + value;
                                    }
                                    break;
                                case "euCountry":
                                    var countryCode = (CountryCode)Enum.Parse(typeof(CountryCode),
                                        ((CreateIntraStatFilePage.EUCountries)val).ToString(), true);

                                    var iso = Enum.GetName(typeof(CountryISOCode), ((int)countryCode));

                                    value = iso + " ";
                                    break;

                                case "interntRefNr":
                                    value = NETSNorge.processString(val.ToString(), 10, false);
                                    break;
                                default:
                                    value = Convert.ToString(val);
                                    break;
                            }

                        value = Regex.Replace(value, "[\"\';]", " ");
                        sw.Write(value);
                    }
                }
            }
        }

        public static void CreateAndStreamFirstAndLast(List<IntrastatClient> listOfImportExport, StreamWriter sw, bool firstOrLast, CrudAPI api)
        {
            var intraStat = new IntrastatClient();
            var streamFileList = new List<IntrastatClient>();
            if (firstOrLast)
            {
                intraStat.recNr = "00";
                intraStat.companyRegNr = companyRegNr;
                intraStat.intraField = "INTRASTAT";
                intraStat.filler = new string(' ', 61);
                streamFileList.Add(intraStat);

                intraStat = new IntrastatClient
                {
                    recNr = "01",
                    transType = "00004",
                    filler = new string(' ', 73)
                };
                streamFileList.Add(intraStat);
            }
            else
            {
                var amountTotal = listOfImportExport.Sum(fakcal => Math.Abs(fakcal.invoiceAmount));

                intraStat.recNr = "10";
                intraStat.suminvoiceAmount = amountTotal;
                intraStat.filler = new string(' ', 62);
                streamFileList.Add(intraStat);
            }


            var outputFields = new[]
            {
                "recNr", "companyRegNr", "intraField", "transType", "suminvoiceAmount", "filler"
            };

            var fields = outputFields.Select(fld => intraStat.GetType().GetField(fld)).ToList();

            foreach (var startEnd in streamFileList)
            {
                foreach (var f in fields)
                {
                    var val = f.GetValue(startEnd);
                    string value;

                    if (val is DateTime)
                    {
                        value = ((DateTime)val).ToString("yyMM");
                    }
                    else if (val is double)
                    {
                        if (streamFileList.Count == 1)
                        {
                            value = NumberConvert.ToLong(Math.Abs((double)val) * 100d).ToString();
                            value = value.PadLeft(16, '0');
                        }
                        else
                        {
                            value = String.Empty;
                        }
                    }
                    else
                    {
                        value = Convert.ToString(val);
                    }
                    value = Regex.Replace(value, "[\"\';]", " ");
                    sw.Write(value);
                }
            }
            
        }
    }
}
