using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

namespace UnicontaISO20022CreditTransfer
{

    public class PaymentReference
    {
        #region Member variables
        private int numberSeqFileId;
        private int numberSeqRefId;
        #endregion

        #region Properties
        public int NumberSeqFileId
        {
            get
            {
                return numberSeqFileId;
            }
            set
            {
                numberSeqFileId = value;
            }
        }

        public int NumberSeqRefId
        {
            get
            {
                return numberSeqRefId;
            }
            set
            {
                numberSeqRefId = value;
            }
        }
        #endregion

        public void InsertPaymentReferenceTask(IEnumerable<CreditorTransPayment> paymentList, IEnumerable<CreditorTransPayment> totalPaymentList, CrudAPI capi, bool glJournalGenerated = false)
        {
            var lstUpdate = new List<CreditorTransPayment>();
            var lstInsert = new List<CreditorPaymentReference>();
            foreach (var rec in totalPaymentList)
            {
                if (rec == null || rec._PaymentRefId == 0)
                    continue;

                lstUpdate.Add(rec);

                if (glJournalGenerated == false)
                {
                    var credPaymRef = new CreditorPaymentReference();
                    credPaymRef._Account = rec.Account;
                    credPaymRef._Created = BasePage.GetSystemDefaultDate();
                    credPaymRef._PaymentFileId = numberSeqFileId;
                    credPaymRef._PaymentRefId = rec._PaymentRefId;
                    credPaymRef._TransDate = rec.Date;
                    credPaymRef._TransRowId = rec.PrimaryKeyId;
                    lstInsert.Add(credPaymRef);
                }

                rec._ErrorInfo = Uniconta.ClientTools.Localization.lookup("Paid");
                rec.Paid = true;
                rec.NotifyErrorSet();
            }

            if (glJournalGenerated == false)
                capi.InsertNoResponse(lstInsert);

            capi.Update(lstUpdate);
        }

        public async Task PaymentRefSequence(CrudAPI capi)
        {
            var seqFileId = 1;
            var seqRefId = 1;

            var queryCreditorPaymRef = await capi.Query<CreditorPaymentReference>();
            if (queryCreditorPaymRef.Length > 0)
            {
                seqFileId = queryCreditorPaymRef.Max(s => s._PaymentFileId) + 1;
                seqRefId = queryCreditorPaymRef.Max(s => s._PaymentRefId);
            }

            numberSeqFileId = seqFileId;
            numberSeqRefId = seqRefId;
        }
    }
}
