
namespace ISO20022CreditTransfer
{
    public class PaymentStatus
    {
            #region Properties
            public string HeaderMsgId;
            public string HeaderCreatedDate;
            public string HeaderBankID;
            public string HeaderCustomerID;
            public string HeaderschmeNmCd;
            public string HeaderOrigMsgId;
            public string HeaderPaymFormat;
            public string HeaderNumbOfPayments;
            public string HeaderGroupStatus;

            public string TransInstrId;
            public string TransEndToEndId;
            public string TransStatus;
            public string TransStatusCode;
            public string TransStatusCodeAdd;
            public double TransAmount;
            public string TransCcy;
            public string TransReqPaymDate;
            public string TransDbtrName;
            public string TransDbtrIBAN;
            public string TransDbtrBIC;
            public string TransCdtrName;
            public string TransCdtrBBAN;
            public string TransCdtrIBAN;
            public string TransCdtrAccType;

            public string TransStatusDescription;
            public string TransStatusDescriptionShort;
            #endregion
    }
}
