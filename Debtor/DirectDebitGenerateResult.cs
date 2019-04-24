using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace UnicontaDirectDebitPayment
{
    public class DirectDebitGenerateResult
    {
        private readonly XmlDocument xmlDoc;
        private readonly StreamWriter streamW;

        private readonly bool hasErrors;
        private readonly int numberOfPayments;
        private readonly IEnumerable<PreCheckError> preCheckErrors;
        //private readonly IEnumerable<CheckError> checkErrors;

        //TODO:HUSK OPRYDNING I DENNE

        public XmlDocument XmlDocument
        {
            get
            {
                return xmlDoc;
            }
        }

        public StreamWriter StreamW
        {
            get
            {
                return streamW;
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

        public DirectDebitGenerateResult(StreamWriter sw, int payments, bool hasErrors = false)//, IEnumerable<PreCheckError> preCheckErrors) 
        {
            this.streamW = sw;
            this.hasErrors = hasErrors;
            this.numberOfPayments = payments;
            //this.preCheckErrors = preCheckErrors;
        }

        public DirectDebitGenerateResult(XmlDocument document, int payments, bool hasErrors = false)//, IEnumerable<PreCheckError> preCheckErrors)
        {
            this.xmlDoc = document;
            this.hasErrors = hasErrors;
            this.numberOfPayments = payments;
            //this.preCheckErrors = preCheckErrors;
        }

        public DirectDebitGenerateResult(IEnumerable<PreCheckError> preCheckErrors, int payments, bool hasErrors = false)
        {
            this.hasErrors = hasErrors;
            this.numberOfPayments = payments;
            this.preCheckErrors = preCheckErrors;
        }


    

        //public IEnumerable<CheckError> CheckErrors
        //{
        //    get
        //    {
        //        return checkErrors;
        //    }
        //}
    }


    /// <summary>
    /// Exception thrown during pre-validation of general settings, before generating the payment file.
    /// </summary>
    public class PreCheckError
    {
        private readonly string description;

        public PreCheckError(string description)
        {
            this.description = description;
        }

        public override string ToString()
        {
            return string.Format("{0}", description);
        }

        public string Description
        {
            get
            {
                return description;
            }
        }
    }

    ///// <summary>
    ///// Exception thrown during validation.
    ///// </summary>
    //public class CheckError
    //{
    //    private readonly string description;

    //    public CheckError(string description)
    //    {
    //        this.description = description;
    //    }

    //    public override string ToString()
    //    {
    //        return string.Format("{0}", description);
    //    }

    //    public string Description
    //    {
    //        get
    //        {
    //            return description;
    //        }
    //    }
    //}
}

