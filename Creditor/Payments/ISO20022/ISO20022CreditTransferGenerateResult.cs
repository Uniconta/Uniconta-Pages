using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnicontaISO20022CreditTransfer;

namespace ISO20022CreditTransfer
{
    public class XMLDocumentGenerateResult
    {
        private readonly XmlDocument document;
        private readonly bool hasErrors;
        private readonly int numberOfPayments;
        private readonly IEnumerable<PreCheckError> preCheckErrors;
        private readonly IEnumerable<CheckError> checkErrors;
        private readonly Encoding encoding;
        private readonly string fileName;

        public XMLDocumentGenerateResult(XmlDocument document, bool hasErrors, int payments, IEnumerable<PreCheckError> preCheckErrors)
        {
            this.document = document;
            this.hasErrors = hasErrors;
            this.numberOfPayments = payments;
            this.preCheckErrors = preCheckErrors;
        }

        public XMLDocumentGenerateResult(XmlDocument document, bool hasErrors, int payments, IEnumerable<CheckError> checkErrors)
        {
            this.document = document;
            this.hasErrors = hasErrors;
            this.numberOfPayments = payments;
            this.checkErrors = checkErrors;
        }

        public XMLDocumentGenerateResult(XmlDocument document, bool hasErrors, CreditTransferDocument doc, IEnumerable<CheckError> checkErrors, string fileName)
        {
            this.document = document;
            this.hasErrors = hasErrors;
            this.numberOfPayments = doc.HeaderNumberOfTrans;
            this.checkErrors = checkErrors;
            this.encoding = doc.EncodingFormat;
            this.fileName = fileName;
        }

        public XmlDocument Document
        {
            get
            {
                return document;
            }
        }

        public int NumberOfPayments
        {
            get
            {
                return numberOfPayments;
            }
        }

        public bool HasErrors
        {
            get
            {
                return hasErrors;
            }
        }

        public IEnumerable<PreCheckError> PreCheckErrors
        {
            get
            {
                return preCheckErrors;
            }
        }

        public IEnumerable<CheckError> CheckErrors
        {
            get
            {
                return checkErrors;
            }
        }

        public Encoding Encoding
        {
            get
            {
                return encoding;
            }
        }

        public string FileName
        {
            get
            {
                return fileName;
            }
        }
    }
}
