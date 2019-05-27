using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages.Creditor.Payments;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// 
    /// </summary>
    public class Altinn
    {
        #region Member Constants
        protected const string XML_PROCESSING_INSTRUCTION = " version='1.0' encoding='UTF-8'";
        protected const string ENVELOPE_TAG_NAME = "Envelope";
        protected const string XMLNS = "xmlns:ns";
        protected const string XMLNSSOAP = "xmlns:soapenv";
        protected const string XMLNS_URI = "http://www.altinn.no/services/Intermediary/Shipment/IntermediaryInbound/2009/10";
        protected const string XMLNSSOAP_URI = "http://schemas.xmlsoap.org/soap/envelope/";
        protected const string NS = "ns";
        protected const string SOAPENV = "soapenv";
        protected const string HEADER = "Header";
        protected const string BODY = "Body";
        protected const string TASKBASIC = "SubmitFormTaskBasic";
        protected const string TASKSHIPMENT = "formTaskShipment";
        protected const string TASKS = "FormTasks";
        protected const string FORMS = "Forms";
        protected const string FORM = "Form";
        protected const string FORMDATA = "FormData";

        protected const string SYSTEMUSERNAME = "systemUserName";
        protected const string SYSTEMPASSWORD = "systemPassword";
        protected const string USERSSN = "userSSN";
        protected const string USERPASSWORD = "userPassword";
        
        protected const string USERPINCODE = "userPinCode";
        protected const string AUTHMETHOD = "authMethod";

        protected const string REPORTEE = "Reportee";
        protected const string EXTERNALSHIPMENTREF = "ExternalShipmentReference";

        protected const string SERVICECODE = "ServiceCode";
        protected const string SERVICEEDITIONCODE = "ServiceEdition";

        protected const string COMPLETED = "Completed";
        protected const string DATAFORMATID = "DataFormatId";
        protected const string DATAFORMATVERSION = "DataFormatVersion";
        protected const string ENDUSERSYSTEMREFERENCE = "EndUserSystemReference";
        protected const string PARENTREFERENCE = "ParentReference";

        protected const string NSRESPONSENSE_HEADER = "http://schemas.xmlsoap.org/soap/envelope/";
        protected const string NSRESPONSENSE_OK = "http://www.altinn.no/services/Intermediary/Shipment/IntermediaryInbound/2009/10";
        protected const string NSRESPONSENSE_ERR = "http://www.altinn.no/services/common/fault/2009/10";

        protected const string NSAUTHENTICATION_01 = "http://www.altinn.no/services/Authentication/SystemAuthentication/2009/10";
        protected const string NSAUTHENTICATION_02 = "http://schemas.altinn.no/services/Authentication/2009/10";

        protected const string SERVICECODE_VALUE = "4222";
        protected const string SERVICEEDITIONCODE_VALUE = "160523";
        protected const string ENDUSERSYSTEMREFERENCE_VALUE = "Uniconta";
        protected const string COMPLETED_VALUE_FALSE = "false";
        protected const string COMPLETED_VALUE_TRUE = "true";

        protected const string Auth_OK = "Ok";
        protected const string OK = "OK";

        protected const string PARENTREFERENCE_VALUE = "0";

        protected const int DATAFORMATID_VALUE = 212;
        protected const int DATAFORMATVERSION_VALUE = 20160523;
        protected const int TERMINTYPE_YEAR = 1;
        protected const int TERMINTYPE_TWOMTH = 4;
        protected const int TERMINTYPE_MTH = 5;
        protected const int TERMINTYPE_HALFMTH = 6;

        protected const int MELDINGSTYPE_HOVED = 1;

        protected const string MELDING_DATAFORMATPROVIDER = "Skatteetaten";
        protected const string MVATJENESTETYPE_RF0002 = "alminneligNaering";
        #endregion

        #region Member variables

        private int year;
        private int terminType;
        private string termin;

        private List<VatSumOperationReport> vatSumOperationLst;

        #endregion

        #region Properties
        private XmlNamespaceManager ns { get; set; }
        private XmlDocument responseDoc { get; set; }
        private string CompanyBBAN { get; set; }
        private string CompanyIBAN { get; set; }
        private string CompanyBIC { get; set; }
        private string CompanyCVR { get; set; }

        private bool AltinnTestEnvironment { get; set; }
        private string SystemUserName { get; set; }
        private string SystemPassword { get; set; }
        private string UserSSN { get; set; }
        private string UserPassword { get; set; }
        private string UserPinCode { get; set; }
        private string AuthMethod { get; set; }
        private DateTime FromDate { get; set; }
        private DateTime ToDate { get; set; }
        private Company Comp { get; set; }
        private bool AutoSigning { get; set; }
        private bool AuthenticationRequest { get; set; }

        private List<VatSumOperationReport> VatSumOperationLst
        {
            get { return vatSumOperationLst; }
            set { vatSumOperationLst = value; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Generates and transfers Mva-oppgaven to Altinn Norway
        /// </summary>
        /// <param name="company">Uniconta company</param> 
        /// <param name="systemUserName">Systemid for sluttbrukersystem definert i portal.</param>
        /// <param name="systemPassword">Passord for sluttbrukersystem definert i portal.</param>
        /// <param name="autoSigning"></param>
        /// <param name="userSSN">Brukers fødselsnummer. Til bruk både til autentisering og evt. signering.</param>
        /// <param name="userPassword">Brukers passord. Til bruk både til autentisering og evt. signering </param>
        /// <param name="userPinCode">Pinkode for valgt engangskodetype (authMethod: SMSPin or AltinnPin) </param>
        /// <param name="authMethod">Angir hvilken engangskodetype bruker (i sluttbrukersystemet) vil autentiseres med. Gyldige typer for denne verdien er:AltinnPin, SMSPin </param>
        /// <param name="fromDate">From date</param>
        /// <param name="toDate">To date</param>
        /// <param name="altinnTest">Transfer to Test environment at Altinn</param>
        /// <returns>Result and response document</returns>
        public Altinn(Company company, string systemUserName, string systemPassword, string userSSN, string userPassword,
                      string userPinCode, string authMethod, DateTime fromDate, DateTime toDate, bool altinnTest)
        {
            Comp = company;
            SystemUserName = systemUserName;  
            SystemPassword = systemPassword; 
            UserSSN = userSSN; 
            UserPassword = userPassword;
            UserPinCode = userPinCode;
            AuthMethod = authMethod;
            FromDate = fromDate;
            ToDate = toDate;
            AutoSigning = true;
            AltinnTestEnvironment = altinnTest;
        }

        /// <summary>
        /// Generates and Send Authentication Request to Altinn Norway
        /// </summary>
        /// <param name="systemUserName">Systemid for sluttbrukersystem definert i portal.</param>
        /// <param name="systemPassword">Passord for sluttbrukersystem definert i portal.</param>
        /// <param name="userSSN">Brukers fødselsnummer. Til bruk både til autentisering og evt. signering.</param>
        /// <param name="userPassword">Brukers passord. Til bruk både til autentisering og evt. signering </param>
        /// <param name="authMethod">Angir hvilken engangskodetype bruker (i sluttbrukersystemet) vil autentiseres med. Gyldige typer for denne verdien er:AltinnPin, SMSPin </param>
        /// <param name="altinnTest">Transfer to Test environment at Altinn</param>
        /// <returns>Result and response document</returns>
        public Altinn(string systemUserName, string systemPassword, string userSSN, string userPassword, string authMethod, bool altinnTest = false)
        {
            SystemUserName = systemUserName;
            SystemPassword = systemPassword;
            UserSSN = userSSN;
            UserPassword = userPassword;
            AuthMethod = authMethod;
            AuthenticationRequest = true;
            AltinnTestEnvironment = altinnTest;
        }

        /// <summary>
        /// Generates and transfers Mva-oppgaven to Altinn Norway (No auto signing)
        /// </summary>
        /// <param name="company">Uniconta company</param> 
        /// <param name="systemUserName">Systemid for sluttbrukersystem definert i portal.</param>
        /// <param name="systemPassword">Passord for sluttbrukersystem definert i portal.</param>
        /// <param name="fromDate">From date</param>
        /// <param name="toDate">To date</param>
        /// <param name="altinnTest">Transfer to Test environment at Altinn</param>
        /// <returns>Result and response document</returns>
        public Altinn(Company company, string systemUserName, string systemPassword, DateTime fromDate, DateTime toDate, bool altinnTest = false)
        {
            Comp = company;
            SystemUserName = systemUserName;
            SystemPassword = systemPassword;
            FromDate = fromDate;
            ToDate = toDate;
            AutoSigning = false;
            AltinnTestEnvironment = altinnTest;
        }
        #endregion

        /// <summary>
        /// Creates an Altinn XML.
        /// </summary>
        public AltinnCreationResult GenerateAltinnXML(List<VatSumOperationReport> vatSumOperationLst)
        {
            VatSumOperationLst = vatSumOperationLst;
          
            var precheckErrors = PrecheckError();

            XmlDocument altinnDoc = null;
            List<AltinnResponse> altinnResponse = null;

            if (precheckErrors.Count == 0)
            {
                altinnResponse = new List<AltinnResponse>();

                var tupleTermin = AltinnCalculateTermin(FromDate, ToDate);
                year = tupleTermin.Item1;
                terminType = tupleTermin.Item2;
                termin = tupleTermin.Item3;

                altinnDoc = CreateXmlDocument();

                AltinnTransfer altinnTransfer = new AltinnTransfer(altinnDoc.OuterXml, AltinnTestEnvironment);

                altinnResponse = ResponseDocument(altinnTransfer.MVAMelding());
            }

            return new AltinnCreationResult(altinnDoc, responseDoc, precheckErrors.Count > 0, altinnResponse, precheckErrors);
        }

        /// <summary>
        /// Send authentication request to Altinn
        /// </summary>
        public AltinnCreationResult SendAuthenticationRequest()
        {
            var precheckErrors = PrecheckError();

            List<AltinnResponse> altinnResponse = null;

            if (precheckErrors.Count == 0)
            {
                altinnResponse = new List<AltinnResponse>();

                AltinnTransfer altinnTransfer = new AltinnTransfer(SystemUserName, UserSSN, UserPassword, AuthMethod, AltinnTestEnvironment);
                altinnResponse = ResponseDocument(altinnTransfer.SendAuthenticationRequest());
            }

            return new AltinnCreationResult(responseDoc, precheckErrors.Count > 0, altinnResponse, precheckErrors);
        }

        private List<AltinnPrecheckError> PrecheckError()
        {
            List<AltinnPrecheckError> precheckErrors = new List<AltinnPrecheckError>();

            if (string.IsNullOrEmpty(SystemUserName))
                precheckErrors.Add(new AltinnPrecheckError("Altinn System-id has not been defined"));
            if (string.IsNullOrEmpty(SystemPassword))
                precheckErrors.Add(new AltinnPrecheckError("Altinn System-Id password has not been defined"));

            if (AuthenticationRequest)
            {
                if (string.IsNullOrEmpty(UserSSN))
                    precheckErrors.Add(new AltinnPrecheckError("Altinn User-Id has not been defined"));
                if (string.IsNullOrEmpty(UserPassword))
                    precheckErrors.Add(new AltinnPrecheckError("Altinn User-Id password has not been defined"));
            }
            else
            {
                if (AutoSigning)
                {
                    if (string.IsNullOrEmpty(UserSSN))
                        precheckErrors.Add(new AltinnPrecheckError("Altinn User-Id has not been defined"));
                    if (string.IsNullOrEmpty(UserPassword))
                        precheckErrors.Add(new AltinnPrecheckError("Altinn User-Id password has not been defined"));
                }

                CompanyBBAN = getCompanyBBAN();
                CompanyIBAN = getCompanyIBAN();
                CompanyBIC = getCompanyBIC();
                CompanyCVR = getCompanyCVR();

                if (CompanyBBAN == string.Empty && CompanyIBAN == string.Empty)
                {
                    precheckErrors.Add(new AltinnPrecheckError("Bank informations are missing"));
                }
                else
                {
                    if (CompanyBBAN == string.Empty)
                    {
                        if (!StandardPaymentFunctions.ValidateIBAN(CompanyIBAN))
                            precheckErrors.Add(new AltinnPrecheckError("The IBAN number has not a valid format"));
                        if (!StandardPaymentFunctions.ValidateBIC(CompanyBIC))
                            precheckErrors.Add(new AltinnPrecheckError("The SWIFT code has not a valid format"));
                    }
                }

                if (Comp._Id == null)
                    precheckErrors.Add(new AltinnPrecheckError("Organisation number has not been defined"));

                year = FromDate.Year;

                if (FromDate == DateTime.MinValue || ToDate == DateTime.MinValue || FromDate > ToDate)
                    precheckErrors.Add(new AltinnPrecheckError("The period is not valid"));

                if (FromDate.Year != ToDate.Year)
                    precheckErrors.Add(new AltinnPrecheckError("From-date and to-date are in two different years"));
            }
            return precheckErrors;
        }

        public static Tuple<int, int, string> AltinnCalculateTermin(DateTime fromDate, DateTime toDate)
        {
            int terminType = 0;
            string termin = string.Empty;

            var year = fromDate.Year;

            int terminId = 0;

            if (toDate.Month - fromDate.Month == 1)
            {
                terminType = TERMINTYPE_TWOMTH;
                terminId = fromDate.Month == 1 ? 1 :
                           fromDate.Month == 3 ? 2 :
                           fromDate.Month == 5 ? 3 :
                           fromDate.Month == 7 ? 4 :
                           fromDate.Month == 9 ? 5 :
                           fromDate.Month == 11 ? 6 : 0;
                termin = string.Format("{0}{1}", terminId, TERMINTYPE_TWOMTH);
            }
            else if (fromDate.Month == 1 && toDate.Month == 12)
            {
                terminType = TERMINTYPE_YEAR;
                termin = string.Format("{0}{1}", TERMINTYPE_YEAR, TERMINTYPE_YEAR);
            }
            else if (fromDate.Month == toDate.Month && toDate.Day - fromDate.Day <= DateTime.DaysInMonth(year, fromDate.Month) / 2)
            {
                if (fromDate.Month == 1)
                    terminId = fromDate.Day == 1 ? 1 : 2;
                if (fromDate.Month == 2)
                    terminId = fromDate.Day == 1 ? 3 : 4;
                if (fromDate.Month == 3)
                    terminId = fromDate.Day == 1 ? 5 : 6;
                if (fromDate.Month == 4)
                    terminId = fromDate.Day == 1 ? 7 : 8;
                if (fromDate.Month == 5)
                    terminId = fromDate.Day == 1 ? 9 : 10;
                if (fromDate.Month == 6)
                    terminId = fromDate.Day == 1 ? 11 : 12;
                if (fromDate.Month == 7)
                    terminId = fromDate.Day == 1 ? 13 : 14;
                if (fromDate.Month == 8)
                    terminId = fromDate.Day == 1 ? 15 : 16;
                if (fromDate.Month == 9)
                    terminId = fromDate.Day == 1 ? 17 : 18;
                if (fromDate.Month == 10)
                    terminId = fromDate.Day == 1 ? 19 : 20;
                if (fromDate.Month == 11)
                    terminId = fromDate.Day == 1 ? 21 : 22;
                if (fromDate.Month == 12)
                    terminId = fromDate.Day == 1 ? 23 : 24;

                termin = string.Format("{0}{1}", terminId, TERMINTYPE_HALFMTH);
            }
            else if (fromDate.Month == toDate.Month)
            {
                terminType = TERMINTYPE_MTH;
                termin = string.Format("{0}{1}", fromDate.Month, TERMINTYPE_MTH);
            }

            termin = termin.PadLeft(3, '0');

            return new Tuple<int, int, string>(year, terminType, termin);
        }
        private string getNodeValue(string xmlPath, string attributeName = "")
        {
            var idNode = responseDoc.SelectSingleNode(xmlPath, ns);
            if (idNode == null)
                return string.Empty;

            if (attributeName != string.Empty)
                return idNode.Attributes[attributeName].Value;

            return idNode.InnerText;
        }

        protected virtual string EnsureString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        protected void CreateDocument(XmlDocument doc, XmlElement root)
        {
            AppendSoapenvElement(doc, root, HEADER);
            XmlElement bodyElement = AppendSoapenvElement(doc, root, BODY);

            AppendTaskBasicElement(doc, bodyElement);
        }

        public virtual XmlDocument CreateXmlDocument()
        {
            XmlDocument doc = new XmlDocument();

            InitDoc(doc);

            return doc;
        }

        protected void InitDoc(XmlDocument doc)
        {
            AppendXmlProcessingInstruction(doc);
            XmlElement root = CreateRootElement(doc);
            CreateDocument(doc, root);
            doc.AppendChild(root);
        }

        protected void AppendXmlProcessingInstruction(XmlDocument doc)
        {
            XmlProcessingInstruction processingInstruction = doc.CreateProcessingInstruction("xml", XML_PROCESSING_INSTRUCTION);
            doc.AppendChild(processingInstruction);
        }

        protected XmlElement CreateRootElement(XmlDocument doc)
        {
            XmlElement root = doc.CreateElement(SOAPENV, ENVELOPE_TAG_NAME, XMLNSSOAP_URI);
            root.SetAttribute(XMLNSSOAP, XMLNSSOAP_URI);
            root.SetAttribute(XMLNS, XMLNS_URI);

            return root;
        }

        internal virtual XmlElement AppendElement(XmlDocument doc, XmlElement parent, string prefix, string name, string uri, string value)
        {
            XmlElement element = doc.CreateElement(prefix, name, uri);

            element.InnerText = value;
            parent.AppendChild(element);

            return element;
        }

        internal virtual XmlElement AppendNsElement(XmlDocument doc, XmlElement parent, string name, string value)
        {
            return AppendElement(doc, parent, NS, name, XMLNS_URI, value);
        }

        internal virtual XmlElement AppendNsElement(XmlDocument doc, XmlElement parent, string name, int value)
        {
            return AppendElement(doc, parent, NS, name, XMLNS_URI, value.ToString());
        }

        internal virtual XmlElement AppendNsElement(XmlDocument doc, XmlElement parent, string name)
        {
            return AppendElement(doc, parent, NS, name, XMLNS_URI, string.Empty);
        }

        internal virtual XmlElement AppendSoapenvElement(XmlDocument doc, XmlElement parent, string name)
        {
            return AppendElement(doc, parent, SOAPENV, name, XMLNSSOAP_URI, string.Empty);
        }

        internal virtual XmlElement AppendNsElementSectioned(XmlDocument doc, XmlElement parent, string name, string value)
        {
            XmlElement retval = AppendElement(doc, parent, NS, name, XMLNS_URI, string.Empty);
            XmlCDataSection section = doc.CreateCDataSection(EnsureString(value));
            retval.AppendChild(section);
            return retval;
        }

        protected virtual void AppendTaskBasicElement(XmlDocument doc, XmlElement root)
        {
            XmlElement TaskBasicElement = AppendNsElement(doc, root, TASKBASIC);

            AppendNsElement(doc, TaskBasicElement, SYSTEMUSERNAME, SystemUserName);
            AppendNsElement(doc, TaskBasicElement, SYSTEMPASSWORD, SystemPassword);
            if (AutoSigning)
            {
                AppendNsElement(doc, TaskBasicElement, USERSSN, UserSSN);
                AppendNsElement(doc, TaskBasicElement, USERPASSWORD, UserPassword);

                AppendNsElement(doc, TaskBasicElement, USERPINCODE, UserPinCode);
                AppendNsElement(doc, TaskBasicElement, AUTHMETHOD, AuthMethod);
            }

            AppendTaskShipmentElement(doc, TaskBasicElement);
        }

        protected virtual void AppendTaskShipmentElement(XmlDocument doc, XmlElement root)
        {
            XmlElement TaskShipmentElement = AppendNsElement(doc, root, TASKSHIPMENT);

            var shipmentRef = DateTime.Now.ToString("yyyyMMddTHHmmss");

            AppendNsElement(doc, TaskShipmentElement, REPORTEE, CompanyCVR);
            AppendNsElement(doc, TaskShipmentElement, EXTERNALSHIPMENTREF, shipmentRef);

            AppendTasksElement(doc, TaskShipmentElement);

            if(AutoSigning)
                AppendSignatureElement(doc, TaskShipmentElement);
        }

        protected virtual void AppendTasksElement(XmlDocument doc, XmlElement root)
        {
            XmlElement TasksElement = AppendNsElement(doc, root, TASKS);


            AppendNsElement(doc, TasksElement, SERVICECODE, SERVICECODE_VALUE);
            AppendNsElement(doc, TasksElement, SERVICEEDITIONCODE, SERVICEEDITIONCODE_VALUE);

            AppendFormsElement(doc, TasksElement);
        }

        protected virtual void AppendFormsElement(XmlDocument doc, XmlElement root)
        {
            XmlElement FormsElement = AppendNsElement(doc, root, FORMS);
            XmlElement FormElement = AppendNsElement(doc, FormsElement, FORM);

            AppendNsElement(doc, FormElement, COMPLETED, AutoSigning ? COMPLETED_VALUE_TRUE : COMPLETED_VALUE_FALSE);
            AppendNsElement(doc, FormElement, DATAFORMATID, DATAFORMATID_VALUE);
            AppendNsElement(doc, FormElement, DATAFORMATVERSION, DATAFORMATVERSION_VALUE);
            AppendNsElement(doc, FormElement, ENDUSERSYSTEMREFERENCE, ENDUSERSYSTEMREFERENCE_VALUE);
            AppendNsElement(doc, FormElement, PARENTREFERENCE, PARENTREFERENCE_VALUE);

            AppendFormDataElement(doc, FormElement);
        }

        protected virtual void AppendSignatureElement(XmlDocument doc, XmlElement root)
        {
            XmlElement SignaturesElement = AppendNsElement(doc, root, "Signatures");

            var loginDateTime = DateTimeOffset.Now.ToString("o");

            AppendNsElement(doc, SignaturesElement, "EndUserSystemReference", "99");
            AppendNsElement(doc, SignaturesElement, "EndUserSystemUserId", "12781");
            AppendNsElement(doc, SignaturesElement, "EndUserSystemLoginDateTime", loginDateTime);
            AppendNsElement(doc, SignaturesElement, "EndUserSystemSignatureDateTime", loginDateTime);
            AppendNsElement(doc, SignaturesElement, "EndUserSystemVersion", "2.0");
            AppendNsElement(doc, SignaturesElement, "EndUserSystemSignatureLogId", "0");
        }


        protected virtual void AppendFormDataElement(XmlDocument doc, XmlElement root)
        {
            var mvaCoreXml = CDATAMvaOppgave(year, terminType, termin);

            byte[] bytes = Encoding.Default.GetBytes(mvaCoreXml);
            mvaCoreXml = Encoding.UTF8.GetString(bytes);

            AppendNsElementSectioned(doc, root, FORMDATA, mvaCoreXml);
        }

        private List<AltinnResponse> ResponseDocument(string response)
        {
            List<AltinnResponse> altinnResponse = new List<AltinnResponse>();

            responseDoc = new XmlDocument();
            using (StringReader s = new StringReader(response))
            {
                responseDoc.Load(s);
            }

            ns = new XmlNamespaceManager(responseDoc.NameTable);
            ns.AddNamespace("ns1", NSRESPONSENSE_HEADER);
            ns.AddNamespace("ns2", NSRESPONSENSE_ERR);
            ns.AddNamespace("ns3", NSRESPONSENSE_OK);
            ns.AddNamespace("ns4", NSAUTHENTICATION_01);
            ns.AddNamespace("ns5", NSAUTHENTICATION_02);

            var idNodeAuth = getNodeValue("//ns4:GetAuthenticationChallengeResult");
            var idNodeResponseFault = getNodeValue("//ns1:Envelope/ns1:Body/ns1:Fault");

            if (idNodeAuth != string.Empty)
            {
                var authenticationMsg = getNodeValue("//ns4:GetAuthenticationChallengeResult/ns5:Message");
                var authenticationStatus = getNodeValue("//ns4:GetAuthenticationChallengeResult/ns5:Status");

                altinnResponse.Add(new AltinnResponse(string.Format("Authentication: {0}", authenticationMsg), authenticationStatus == Auth_OK ? false : true));
            }
            else
            {
                if (idNodeResponseFault != string.Empty)
                {
                    var faultCode = getNodeValue("//ns1:Envelope/ns1:Body/ns1:Fault/faultcode");
                    var faultString = getNodeValue("//ns1:Envelope/ns1:Body/ns1:Fault/faultstring");

                    var altinnErrorMessage = getNodeValue("//ns2:AltinnFault/ns2:AltinnErrorMessage");
                    var altinnExtendedErrorMessage = getNodeValue("//ns2:AltinnFault/ns2:AltinnExtendedErrorMessage");
                    var altinnLocalizedErrorMessage = getNodeValue("//ns2:AltinnFault/ns2:AltinnLocalizedErrorMessage");
                    var errorGuid = getNodeValue("//ns2:AltinnFault/ns2:ErrorGuid");
                    var errorId = getNodeValue("//ns2:AltinnFault/ns2:ErrorId");
                    var userGuid = getNodeValue("//ns2:AltinnFault/ns2:UserGuid");
                    var userId = getNodeValue("//ns2:AltinnFault/ns2:UserId");

                    altinnResponse.Add(new AltinnResponse(string.Format("{0}", altinnErrorMessage), true));
                }
                else
                {
                    var receiptId = getNodeValue("//ns3:SubmitFormTaskBasicResponse/ns3:SubmitFormTaskBasicResult/ns3:ReceiptId");
                    var receiptText = getNodeValue("//ns3:SubmitFormTaskBasicResponse/ns3:SubmitFormTaskBasicResult/ns3:ReceiptText");
                    var receiptStatusCode = getNodeValue("//ns3:SubmitFormTaskBasicResponse/ns3:SubmitFormTaskBasicResult/ns3:ReceiptStatusCode");

                    if (receiptStatusCode == OK)
                        altinnResponse.Add(new AltinnResponse(string.Format("{0}", "Mva-oppgaven has been sent to Altinn")));
                    else
                        altinnResponse.Add(new AltinnResponse(string.Format("{0}\n{1}", receiptStatusCode, receiptText)));
                }
            }

            return altinnResponse;
        }


        private string CDATAMvaOppgave(int year, int terminType, string termin)
        {
            XmlWriterSettings WriterSettings = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, Encoding = new UTF8Encoding(false) };
            XmlSerializerNamespaces Namespaces = new XmlSerializerNamespaces(new[] { new XmlQualifiedName("", "") });

            string returnData = string.Empty;
            melding mvaMelding = new melding();
            meldingMeldingsopplysning meldingsOppl = new meldingMeldingsopplysning();
            meldingMvaAvgift mvaAvgift = new meldingMvaAvgift();
            meldingMvaGrunnlag mvaGrunnlag = new meldingMvaGrunnlag();
            meldingMvaSumAvgift mvaSumAvgift = new meldingMvaSumAvgift();
            meldingSkattepliktig meldSkattepliktig = new meldingSkattepliktig();
            meldingTilleggsopplysning meldTilleggsOppl = new meldingTilleggsopplysning();

            #region ID til meldingen
            mvaMelding.dataFormatId = DATAFORMATID_VALUE;
            mvaMelding.dataFormatProvider = MELDING_DATAFORMATPROVIDER;
            mvaMelding.dataFormatVersion = DATAFORMATVERSION_VALUE;
            mvaMelding.tjenesteType = MVATJENESTETYPE_RF0002;
            #endregion

            #region Termin
            meldingsOppl.aar = Convert.ToUInt16(year);
            meldingsOppl.meldingstype = MELDINGSTYPE_HOVED;
            meldingsOppl.termin = termin;
            meldingsOppl.termintype = terminType;
            #endregion

            #region Info om det som avgir oppgaven
            meldSkattepliktig.endretKontonummer = string.Empty;
            meldSkattepliktig.iban = CompanyIBAN;
            meldSkattepliktig.ibanEndret = string.Empty;
            meldSkattepliktig.KIDnummer = string.Empty;
            meldSkattepliktig.kontonummer = CompanyBBAN;
            meldSkattepliktig.organisasjonsnavn = Comp._Name;
            meldSkattepliktig.organisasjonsnummer = getCompanyCVR();
            meldSkattepliktig.swiftBic = CompanyBIC;
            meldSkattepliktig.swiftBicEndret = string.Empty;
            meldTilleggsOppl.forklaring = string.Empty;
            meldTilleggsOppl.forklaringSendt = false;
            #endregion

            foreach (var rec in vatSumOperationLst)
            {
                switch (rec.Line)
                {
                    case 1:
                        mvaGrunnlag.sumOmsetningUtenforMva = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        break;
                    case 2:
                        mvaGrunnlag.sumOmsetningInnenforMvaUttakOgInnfoersel = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        break;
                    case 3:
                        mvaGrunnlag.innlandOmsetningUttakHoeySats = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        mvaAvgift.innlandOmsetningUttakHoeySats = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                    case 4:
                        mvaGrunnlag.innlandOmsetningUttakMiddelsSats = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        mvaAvgift.innlandOmsetningUttakMiddelsSats = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                    case 5:
                        mvaGrunnlag.innlandOmsetningUttakLavSats = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        mvaAvgift.innlandOmsetningUttakLavSats = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                    case 6:
                        mvaGrunnlag.innlandOmsetningUttakFritattMva = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        break;
                    case 7:
                        mvaGrunnlag.innlandOmsetningOmvendtAvgiftsplikt = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        break;
                    case 8:
                        mvaGrunnlag.utfoerselVareTjenesteFritattMva = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        break;
                    case 9:
                        mvaGrunnlag.innfoerselVareHoeySats = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        mvaAvgift.innfoerselVareHoeySats = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                    case 10:
                        mvaGrunnlag.innfoerselVareMiddelsSats = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        mvaAvgift.innfoerselVareMiddelsSats = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                    case 11:
                        mvaGrunnlag.innfoerselVareFritattMva = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        break;
                    case 12:
                        mvaGrunnlag.kjoepUtlandTjenesteHoeySats = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        mvaAvgift.kjoepUtlandTjenesteHoeySats = Math.Abs(Convert.ToInt32(rec.Amount));

                        if (mvaGrunnlag.kjoepUtlandTjenesteHoeySats != 0 || mvaAvgift.kjoepUtlandTjenesteHoeySats != 0)
                        {
                            meldTilleggsOppl.forklaring = "Avgift er beregnet med 10 % av grunnlaget";
                            meldTilleggsOppl.forklaringSendt = true;
                        }
                        else
                        {
                            meldTilleggsOppl.forklaring = string.Empty;
                            meldTilleggsOppl.forklaringSendt = false;
                        }
                        break;
                    case 13:
                        mvaGrunnlag.kjoepInnlandVareTjenesteHoeySats = Math.Abs(Convert.ToInt32(rec.AmountBase));
                        mvaAvgift.kjoepInnlandVareTjenesteHoeySats = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                    case 14:
                        mvaAvgift.fradragInnlandInngaaendeHoeySats = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                    case 15:
                        mvaAvgift.fradragInnlandInngaaendeMiddelsSats = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                    case 16:
                        mvaAvgift.fradragInnlandInngaaendeLavSats = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                    case 17:
                        mvaAvgift.fradragInnfoerselMvaHoeySats = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                    case 18:
                        mvaAvgift.fradragInnfoerselMvaMiddelsSats = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                    case 19:
                        if (rec.AmountBase < 0)
                            mvaSumAvgift.aaBetale = Math.Abs(Convert.ToInt32(rec.Amount));
                        else
                            mvaSumAvgift.tilGode = Math.Abs(Convert.ToInt32(rec.Amount));
                        break;
                }
            }

            mvaMelding.mvaAvgift = mvaAvgift;
            mvaMelding.mvaGrunnlag = mvaGrunnlag;
            mvaMelding.mvaSumAvgift = mvaSumAvgift;
            mvaMelding.meldingsopplysning = meldingsOppl;
            mvaMelding.tilleggsopplysning = meldTilleggsOppl;
            mvaMelding.skattepliktig = meldSkattepliktig;
            var ms = UnistreamReuse.Create();
            var writer = XmlWriter.Create(ms, WriterSettings);
            var serializer = new XmlSerializer(mvaMelding.GetType());
            serializer.Serialize(writer, mvaMelding, Namespaces);
            return Encoding.UTF8.GetString(ms.ToArrayAndRelease());
        }

        private string getCompanyCVR()
        {
            var cvr = Comp._Id ?? string.Empty; 
            cvr = Regex.Replace(cvr, "[^0-9]", "");

            return cvr;
        }

        private string getCompanyBBAN()
        {
            var bban = Comp._NationalBank ?? string.Empty;
            bban = Regex.Replace(bban, "[^0-9]", "");

            return bban;
        }

        private string getCompanyIBAN()
        {
            var iban = Comp._IBAN ?? string.Empty;
            iban = Regex.Replace(iban, "[^\\w\\d]", "");

            return iban;
        }

        private string getCompanyBIC()
        {
            var bic = Comp._SWIFT ?? string.Empty;

            if (bic != string.Empty)
            {
                bic = Regex.Replace(bic, "[^\\w\\d]", "");
                bic = bic.ToUpper();
            }

            return bic;
        }
    }
}

