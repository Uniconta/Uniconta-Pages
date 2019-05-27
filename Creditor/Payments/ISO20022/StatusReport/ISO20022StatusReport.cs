using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.Common;
using Localization = Uniconta.ClientTools.Localization;

namespace ISO20022CreditTransfer
{
    public class ISO20022StatusReport
    {
        private XmlNode idNode { get; set; }
        private XmlNamespaceManager ns { get; set; }
        private XmlDocument xmlDoc { get; set; }
        private PaymentStatus paymentStatus { get; set; }

        protected const string XMLNS_ver00200103 = "urn:iso:std:iso:20022:tech:xsd:pain.002.001.03";

        public const string PAYMSTATUSID_ACSC = "ACSC";
        public const string PAYMSTATUSID_ACSP = "ACSP";
        public const string PAYMSTATUSID_RJCT = "RJCT";
        public const string PAYMSTATUSID_PDNG = "PDNG";
        public const string PAYMSTATUSID_ACWC = "ACWC";


        public void StatusReport(PaymentsGrid dgCreditorTranOpenGrid, string xmlFileName, CrudAPI capi)
        {
            try
            {
                if (!this.IsValidStatusReportFormat(xmlFileName))
                {
                    UnicontaMessageBox.Show(string.Format("Payment Status Report doesn't have a valid format"), Uniconta.ClientTools.Localization.lookup("Error"));
                }
                else
                {
                    //Clear SystemInfo field>>
                    var grid = dgCreditorTranOpenGrid.GetVisibleRows() as IEnumerable<CreditorTransPayment>;
                    foreach (var rec in grid)
                    {
                        rec._ErrorInfo = string.Empty;
                        rec.NotifyErrorSet();
                    }
                    //Clear SystemInfo field<<

                    var changedlst = new List<CrudAPI.UpdatePair>(); 

                    using (var xmlReader = new StreamReader(xmlFileName))
                    {
                        XmlDocument xmldoc = new XmlDocument();
                        xmldoc.Load(xmlReader);

                        var paymStatusList = this.ReadStatusReport(xmldoc);

                        IEnumerable<UnicontaClient.Pages.CreditorTransPayment> queryPaymentTrans = null;

                        foreach (var paymStatusRec in paymStatusList)
                        {
                            if (dgCreditorTranOpenGrid.ItemsSource != null)
                            {
                                var Trans = dgCreditorTranOpenGrid.GetVisibleRows() as IEnumerable<CreditorTransPayment>; 
                                queryPaymentTrans = Trans.Where(paymentTrans =>
                                {
                                var endToEndID = Regex.Replace(paymStatusRec.TransEndToEndId ?? "0", "[^0-9]", "");
                                    return paymentTrans != null && (paymentTrans._PaymentRefId == int.Parse(endToEndID));
                                });

                                foreach (var rec in queryPaymentTrans)
                                {
                                    string statusDescription = paymStatusRec.TransStatusCodeAdd == string.Empty ? paymStatusRec.TransStatusDescription : paymStatusRec.TransStatusCodeAdd;
                                    rec._ErrorInfo = string.Format("{0}\n{1}", paymStatusRec.TransStatusDescriptionShort, statusDescription);
                                    rec.NotifyErrorSet();

                                    if (paymStatusRec.TransStatus == PAYMSTATUSID_ACSC)//The payment has been executed
                                    {
                                        var org = new CreditorTransPayment();
                                        StreamingManager.Copy(rec, org);

                                        rec._Paid = true;
                                        rec.NotifyPropertyChanged("Paid");

                                        changedlst.Add(new CrudAPI.UpdatePair() { loaded = org, modified = rec });
                                    }
                                }
                            }
                        }
                        if (changedlst != null)
                            capi.Update(changedlst);
                    }
                }
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), System.Windows.MessageBoxButton.OK);
                return;
            }
        }

        private string getNodeValue(XmlNode xmlNode, string xmlPath, string attributeName = "")
        {
            string textValue = string.Empty;

            idNode = xmlNode.SelectSingleNode(xmlPath, ns);

            if (idNode == null)
                return string.Empty;

            if (xmlPath == "ns:TxInfAndSts/ns:StsRsnInf/ns:AddtlInf")
            {
                textValue = idNode.InnerText;

                XmlNode nextIdNode = idNode.NextSibling;
                textValue = nextIdNode == null ? textValue : string.Format("{0}, {1}", textValue, nextIdNode.InnerText);
            }
            else
            {
                textValue = idNode.InnerText;
            }

            if (attributeName != string.Empty)
            {
                var xmlAttrib = idNode.Attributes[attributeName];
                if (xmlAttrib == null)
                    return string.Empty;

                return idNode.Attributes[attributeName].Value;
            }

            return textValue;
        }

        private string getNodeValue(string xmlPath, string attributeName = "")
        {
            idNode = xmlDoc.SelectSingleNode(xmlPath, ns);
            if (idNode == null)
                return string.Empty;

            if (attributeName != string.Empty)
                return idNode.Attributes[attributeName].Value;

            return idNode.InnerText;
        }

        
        private bool IsValidStatusReportFormat(string xmlFileName)
        {
            using (var xmlReader = new StreamReader(xmlFileName))
            {
                Regex tagsWithData = new Regex("<\\w+>[^<]+</\\w+>");

                var xmlString = xmlReader.ReadToEnd();

                //Light checking
                if (string.IsNullOrEmpty(xmlString) || tagsWithData.IsMatch(xmlString) == false)
                {
                    return false;
                }

                try
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(xmlString);

                    if (xmlDocument.DocumentElement.NamespaceURI != XMLNS_ver00200103)
                        return false;

                    return true;
                }
                catch (Exception e1)
                {
                    return false;
                }
            }
        }
       

        private List<PaymentStatus> ReadStatusReport(XmlDocument xmlDoc)
        {
            this.xmlDoc = xmlDoc;

            List<PaymentStatus> paymentStatusList = new List<PaymentStatus>();

            ns = new XmlNamespaceManager(xmlDoc.NameTable);
            ns.AddNamespace("ns", XMLNS_ver00200103);

            var HeaderMsgId = getNodeValue("//ns:Document/ns:CstmrPmtStsRpt/ns:GrpHdr/ns:MsgId");
            var HeaderCreatedDate = getNodeValue("//ns:Document/ns:CstmrPmtStsRpt/ns:GrpHdr/ns:CreDtTm");
            var HeaderBankID = getNodeValue("//ns:Document/ns:CstmrPmtStsRpt/ns:GrpHdr/ns:InitgPty/ns:Id/ns:OrgId/ns:BICOrBEI");
            var HeaderCustomerID = getNodeValue("//ns:Document/ns:CstmrPmtStsRpt/ns:GrpHdr/ns:InitgPty/ns:Id/ns:OrgId/ns:Othr/ns:Id");
            var HeaderschmeNmCd = getNodeValue("//ns:Document/ns:CstmrPmtStsRpt/ns:GrpHdr/ns:InitgPty/ns:Id/ns:OrgId/ns:Othr/ns:SchmeNm/ns:Cd");
            var HeaderOrigMsgId = getNodeValue("//ns:Document/ns:CstmrPmtStsRpt/ns:OrgnlGrpInfAndSts/ns:OrgnlMsgId");
            var HeaderPaymFormat = getNodeValue("//ns:Document/ns:CstmrPmtStsRpt/ns:OrgnlGrpInfAndSts/ns:OrgnlMsgNmId");
            var HeaderNumbOfPayments = getNodeValue("//ns:Document/ns:CstmrPmtStsRpt/ns:OrgnlGrpInfAndSts/ns:OrgnlNbOfTxs");
            var HeaderGroupStatus = getNodeValue("//ns:Document/ns:CstmrPmtStsRpt/ns:OrgnlGrpInfAndSts/ns:orgnlGrp_GrpSts");

            foreach (XmlNode orgnlPmtInfAndSts in xmlDoc.SelectNodes("//ns:Document/ns:CstmrPmtStsRpt/ns:OrgnlPmtInfAndSts", ns))
            {
                paymentStatus = new PaymentStatus();

                //Header >>
                paymentStatus.HeaderMsgId = HeaderMsgId;
                paymentStatus.HeaderCreatedDate = HeaderCreatedDate;
                paymentStatus.HeaderBankID = HeaderBankID;
                paymentStatus.HeaderCustomerID = HeaderCustomerID;
                paymentStatus.HeaderschmeNmCd = HeaderschmeNmCd;
                paymentStatus.HeaderOrigMsgId = HeaderOrigMsgId;
                paymentStatus.HeaderPaymFormat = HeaderPaymFormat;
                paymentStatus.HeaderNumbOfPayments = HeaderNumbOfPayments;
                paymentStatus.HeaderGroupStatus = HeaderGroupStatus;
                //Header <<

                //Trans >>
                paymentStatus.TransInstrId = getNodeValue(orgnlPmtInfAndSts, "ns:OrgnlPmtInfId");
                paymentStatus.TransEndToEndId = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:OrgnlEndToEndId");
                paymentStatus.TransStatus = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:TxSts");
                paymentStatus.TransStatusCode = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:StsRsnInf/ns:Rsn/ns:Cd");
                paymentStatus.TransStatusCodeAdd = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:StsRsnInf/ns:AddtlInf");
                var amount = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:OrgnlTxRef/ns:Amt/ns:InstdAmt");
                paymentStatus.TransAmount = Uniconta.Common.Utility.NumberConvert.ToDouble(amount);
                paymentStatus.TransCcy = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:OrgnlTxRef/ns:Amt/ns:InstdAmt", "Ccy");
                paymentStatus.TransReqPaymDate = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:OrgnlTxRef/ns:ReqdExctnDt");
                paymentStatus.TransDbtrName = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:OrgnlTxRef/ns:Dbtr/ns:Nm");
                paymentStatus.TransDbtrIBAN = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:OrgnlTxRef/ns:DbtrAcct/ns:Id/ns:IBAN");
                paymentStatus.TransDbtrBIC = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:OrgnlTxRef/ns:DbtrAgt/ns:FinInstnId/ns:BIC");
                paymentStatus.TransCdtrName = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:OrgnlTxRef/ns:Cdtr/ns:Nm");
                paymentStatus.TransCdtrBBAN = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:OrgnlTxRef/ns:CdtrAcct/ns:Id/ns:Othr/ns:Id");
                paymentStatus.TransCdtrIBAN = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:OrgnlTxRef/ns:CdtrAcct/ns:Id/ns:IBAN");
                paymentStatus.TransCdtrAccType = getNodeValue(orgnlPmtInfAndSts, "ns:TxInfAndSts/ns:OrgnlTxRef/ns:CdtrAcct/ns:Id/ns:Othr/ns:SchmeNm/ns:Cd"); //IBAN, BBAN or OCR
                //Trans <<

                // Status translation >>
                switch (paymentStatus.TransStatus)
                {
                    case PAYMSTATUSID_ACSC:
                        paymentStatus.TransStatusDescriptionShort = Localization.lookup("Paid");
                        paymentStatus.TransStatusDescription = Localization.lookup("PaymentExecuted"); 
                        break;
                    case PAYMSTATUSID_ACSP:
                        paymentStatus.TransStatusDescriptionShort = Localization.lookup("Pending"); 
                        paymentStatus.TransStatusDescription = Localization.lookup("PaymentAcceptedBank");
                        break;
                    case PAYMSTATUSID_RJCT:
                        paymentStatus.TransStatusDescriptionShort = Localization.lookup("Rejected");
                        paymentStatus.TransStatusDescription = Localization.lookup("PaymentRejectedBank");
                        break;
                    case PAYMSTATUSID_PDNG:
                        paymentStatus.TransStatusDescriptionShort = Localization.lookup("Pending");
                        paymentStatus.TransStatusDescription = Localization.lookup("PendingApproval");
                        break;
                    case PAYMSTATUSID_ACWC:
                        paymentStatus.TransStatusDescriptionShort = Localization.lookup("Pending");
                        paymentStatus.TransStatusDescription = string.Format("{0}. {1}", Localization.lookup("PaymentAcceptedBank"), Localization.lookup("NoCharges"));
                        break;
                }
                // Status translation <<

                paymentStatusList.Add(paymentStatus);
            }

            return paymentStatusList;
        }
    }
}
