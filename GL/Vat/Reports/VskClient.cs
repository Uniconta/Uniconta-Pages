using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Uniconta.ClientTools.Controls;
using RSK.Vsk;

namespace RSK
{
      public class VskClient : IDisposable
        {
            private VaskurServices_PortTypeClient Client { get; }

            private const string VersionID = "UnicontaVSK V1.0";
            private const string EndpointUri = "https://thjonusta.rsk.is:443/ws/SkatturWeb.wsd:VaskurServices/SkatturWeb_wsd_VaskurServices_Port";
            
            public VskClient (string username, string password)
            {
                var binding = new BasicHttpsBinding(BasicHttpsSecurityMode.Transport);
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.Certificate;
                binding.Security.Message.AlgorithmSuite = SecurityAlgorithmSuite.Default;
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
                Client = new VaskurServices_PortTypeClient(
                    binding,
                    new EndpointAddress(
                        EndpointUri
                    )
                );
                if (Client.ClientCredentials != null)
                {
                    Client.ClientCredentials.UserName.UserName = username;
                    Client.ClientCredentials.UserName.Password = password;
                }
            }
            
            public async Task<string> NaIVSKNumerAsync(
                string kt)
            {
                var response = await Client.NaIVSKNumerAsync(new docType_ns_NaIVSKNumer { Kennitala = kt, KerfiUtgafa = VersionID });
                return response.NaIVSKNumerSvar.Svar.Tokst ? string.Join(", ", response.NaIVSKNumerSvar.Svar.ListiVSKNumer) : "";
            }

        public async Task<docType_ns_VSKSkyrslaSvar> SkilaVSKSkyrsluAsync(string kennitala, string vskNumer, DateTime toDate, long velta24 = 0, long velta11 = 0, long velta0 = 0, long ut24 = 0, long ut11 = 0, long inn24 = 0, long inn11 = 0)
        {
            var request = new docType_ns_VSKSkyrsla();
            docType_ns_VSKSkyrslaSvar svar = null;
            request.Kennitala = kennitala;
            request.VSKNumer = vskNumer;
            request.Ar = toDate.Year.ToString();
            request.Timabil = $"{toDate.Month * 4:D2}";
            request.KerfiUtgafa = VersionID;
            request.ReiknaAlag = true;
            List<Tuple<TegundFaerslu, long>> tuplesList = new List<Tuple<TegundFaerslu, long>>();
            tuplesList.Add(new Tuple<TegundFaerslu, long>(TegundFaerslu.VeltaAnVSK24, velta24));
            tuplesList.Add(new Tuple<TegundFaerslu, long>(TegundFaerslu.VeltaAnVSK11, velta11));
            tuplesList.Add(new Tuple<TegundFaerslu, long>(TegundFaerslu.UndantheginVelta0, velta0));
            tuplesList.Add(new Tuple<TegundFaerslu, long>(TegundFaerslu.Utskattur24, ut24));
            tuplesList.Add(new Tuple<TegundFaerslu, long>(TegundFaerslu.Utskattur11, ut11));
            tuplesList.Add(new Tuple<TegundFaerslu, long>(TegundFaerslu.Innskattur24, inn24));
            tuplesList.Add(new Tuple<TegundFaerslu, long>(TegundFaerslu.Innskattur11, inn11));
            List<docType_ns_Faersla> faerslaList = tuplesList.Where(o => o.Item2 != 0).Select(tuple => PackDocNsFaersla(tuple.Item1, tuple.Item2)).ToList();
            request.Faerslur = faerslaList.SkipWhile(o => o == null).ToArray();
            SkilaVSKSkyrsluResponse response = await Client.SkilaVSKSkyrsluAsync(request);
            svar = response?.SkilaVSKSkyrsluSvar?.Svar;
            if (svar != null && response.SkilaVSKSkyrsluSvar?.Svar?.NidurstadaSkila != null)
            {
                UnicontaMessageBox.Show("Aðgerð tókst", "Skila til RSK");
            }
            else if (response.SkilaVSKSkyrsluSvar != null)
            {
                MessageBoxResult mbResult = MessageBoxResult.No;
                if (response?.SkilaVSKSkyrsluSvar?.status.code == 999)
                {
                    mbResult = UnicontaMessageBox.Show(
                        response.SkilaVSKSkyrsluSvar.status.message + "\n\nLeiðrétta fyrri skýrslu?",
                        "Villa " + response.SkilaVSKSkyrsluSvar.status.code,
                        MessageBoxButton.YesNo
                    );
                }
                else
                {
                    UnicontaMessageBox.Show(
                        response.SkilaVSKSkyrsluSvar.status.message,
                        "Villa " + response.SkilaVSKSkyrsluSvar.status.code
                    );
                }

                if (mbResult != MessageBoxResult.Yes) return svar;
                var correctionResponse = await Client.LeidrettaVSKSkyrsluAsync(request);
                svar = correctionResponse?.LeidrettaVSKSkyrsluSvar?.Svar;
                if (svar != null && correctionResponse.LeidrettaVSKSkyrsluSvar.Svar?.NidurstadaSkila != null)
                {
                    UnicontaMessageBox.Show("Aðgerð tókst", "Skila til RSK");
                }
                else
                {
                    UnicontaMessageBox.Show(
                        correctionResponse.LeidrettaVSKSkyrsluSvar.status.message,
                        "Villa " + correctionResponse.LeidrettaVSKSkyrsluSvar.status.code
                    );
                }

            }
            return svar;
        }

        private void TestSerialize(
                object request)
            {
                var s = string.Empty;
                var serializer = new XmlSerializer(request.GetType());
                using (StringWriter textWriter = new StringWriter())
                {
                    serializer.Serialize(textWriter, request);
                    s = textWriter.ToString(); 
                }
                MessageBox.Show(s);
            }

            public async Task<EydaSkyrsluIProfunResponse> ResetTestAsync(string kennitala, DateTime toDate, string vskNr)
            {
                var request = new docType_ns_EydaSkyrsluIProfun();
                request.Kennitala = kennitala;
                request.Ar = toDate.Year.ToString();
                request.Timabil = (toDate.Month * 4).ToString("D2");
                request.VSKNumer = vskNr;
                var result = await Client.EydaSkyrsluIProfunAsync(request);
                return result;
            }

            private enum TegundFaerslu
            {
                //Velta
                VeltaAnVSK24,
                VeltaAnVSK11,
                UndantheginVelta0,
                Utskattur24,
                Utskattur11,
                Innskattur24,
                Innskattur11
            }


            private docType_ns_Faersla PackDocNsFaersla(TegundFaerslu type, long? total )
            {
                if (total == null || total == 0) return null;
                docType_ns_Faersla faersla = null;
                string flokkHeiti = "", flokkID = "", threpVal = "", threpID = "", tegundVal = "", tegundID = ""; 
                switch (type)
                {
                    case TegundFaerslu.VeltaAnVSK24:
                        tegundID = "62";
                        tegundVal = "Skattskyld velta án VSK";
                        threpID = "59";
                        threpVal = "24";
                        flokkID = "67";
                        flokkHeiti = "Samtals velta";
                        break;
                    case TegundFaerslu.VeltaAnVSK11:
                        tegundID = "60";
                        tegundVal = "Skattskyld velta án VSK";
                        threpID = "58";
                        threpVal = "11";
                        flokkID = "64";
                        flokkHeiti = "Samtals velta";
                        break;
                    case TegundFaerslu.UndantheginVelta0:
                        tegundID = "18";
                        tegundVal = "Undanþegin velta (12. gr.)";
                        threpID = "4";
                        threpVal = "0";
                        flokkID = "53";
                        flokkHeiti = "Samtals undanþegin";
                        break;
                    case TegundFaerslu.Innskattur24:
                        tegundID = "76";
                        tegundVal = "Innskattur";
                        threpID = "59";
                        threpVal = "24";
                        flokkID = "84";
                        flokkHeiti = "Samtals innskattur";
                        break;
                    case TegundFaerslu.Innskattur11:
                        tegundID = "75";
                        tegundVal = "Innskattur";
                        threpID = "58";
                        threpVal = "11";
                        flokkID = "83";
                        flokkHeiti = "Samtals innskattur";
                        break;
                    case TegundFaerslu.Utskattur24:
                        tegundID = "90";
                        tegundVal = "Útskattur";
                        threpID = "59";
                        threpVal = "24";
                        flokkID = "68";
                        flokkHeiti = "Samtals útskattur";
                        break;
                    case TegundFaerslu.Utskattur11:
                        tegundID = "89";
                        tegundVal = "Útskattur";
                        threpID = "58";
                        threpVal = "11";
                        flokkID = "65";
                        flokkHeiti = "Samtals útskattur";
                        break;
                    default:
                        return null;
                        
                }
                faersla = new docType_ns_Faersla
                {
                    Flokkar = new []{new docType_ns_Flokkur{ id = flokkID, Fjarhaed = total, Heiti = flokkHeiti}}, //We solve lost claims differently
                    Tegund = new Tegund { id = tegundID, Value = tegundVal },
                    Threp = new Threp { id = threpID, Value = threpVal}
                };
                return faersla;
            }



            public void Dispose()
            {
                ((IDisposable) Client)?.Dispose();
            }
        }
}