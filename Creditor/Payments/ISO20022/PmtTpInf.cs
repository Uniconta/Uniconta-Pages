using System;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class PmtTpInf
    {
        private readonly string extServiceCode;
        private readonly string extServicePrtry;
        private readonly string externalLocalInstrument;
        private readonly string extCategoryPurpose;
        private readonly string extProprietaryCode;
        private readonly string instructionPriority;
        
        private const string HPMTTPINF = "PmtTpInf";
        private const string INSTRPRTY = "InstrPrty";
        private const string HSVCLVL = "SvcLvl";
        private const string HLCLINSTRM = "LclInstrm";
        private const string HCTGYPURP = "CtgyPurp";
        private const string CD = "Cd";
        private const string PRTRY = "Prtry";

        /// <summary>
        /// ExternalServiceLevel1Code
        /// </summary>
        public string ExtServiceCode
        {
            get
            {
                return extServiceCode;
            }
        }

        /// <summary>
        /// External Category Purpose
        /// </summary>
        public string ExtCategoryPurpose
        {
            get
            {
                return extCategoryPurpose;
            }
        }

        /// <summary>
        /// ExternalCategory-Purpose1Code
        /// </summary>
        public string InstructionPriority
        {
            get
            {
                return instructionPriority;
            }
        }

        /// <summary>
        /// Payment information
        /// </summary>
        /// <param name="paymentInfoId">Unique identification, as assigned by the initiating party, to unambiguously identify the payment.</param> 
        /// <param name="paymentMethod">Specifies the means of payment that will be used to move the amount of money. CHK Cheque TRF CreditTransfer.</param>
        /// <param name="requestedExecutionDate">Date at which the initiating party requests the clearing agent to process the payment..</param>
        public PmtTpInf(string extServiceCode, string extServicePrtry, string externalLocalInstrument, string extCategoryPurpose, string instructionPriority, string extProprietaryCode)
        {
            this.instructionPriority = instructionPriority;
            this.extServiceCode = extServiceCode;
            this.extServicePrtry = extServicePrtry;
            this.externalLocalInstrument = externalLocalInstrument;
            this.extCategoryPurpose = extCategoryPurpose;
            this.extProprietaryCode = extProprietaryCode;
        }


        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            XmlElement pmtTpInf = baseDoc.AppendElement(doc, parent, HPMTTPINF);

            if (instructionPriority != null)
                baseDoc.AppendElement(doc, pmtTpInf, INSTRPRTY, instructionPriority);

            if (extServiceCode != null || extServicePrtry != null)
            {
                XmlElement svcLvl = baseDoc.AppendElement(doc, pmtTpInf, HSVCLVL);
                if (extServicePrtry != null)
                    baseDoc.AppendElement(doc, svcLvl, PRTRY, extServicePrtry);
                else if (extServiceCode != null)
                    baseDoc.AppendElement(doc, svcLvl, CD, extServiceCode);
            }

            if (externalLocalInstrument != string.Empty || extProprietaryCode != string.Empty)
            {
                XmlElement lclInstrm = baseDoc.AppendElement(doc, pmtTpInf, HLCLINSTRM);

                if (externalLocalInstrument != string.Empty)
                    baseDoc.AppendElement(doc, lclInstrm, CD, externalLocalInstrument);

                if (extProprietaryCode != string.Empty)
                    baseDoc.AppendElement(doc, lclInstrm, PRTRY, extProprietaryCode);
            }

            if (extCategoryPurpose != string.Empty)
            {
                XmlElement ctgyPurp = baseDoc.AppendElement(doc, pmtTpInf, HCTGYPURP);
                baseDoc.AppendElement(doc, ctgyPurp, CD, extCategoryPurpose);
            }
        }
    }
}


