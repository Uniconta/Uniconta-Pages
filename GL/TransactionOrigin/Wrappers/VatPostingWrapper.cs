using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    public class VatPostingWrapper
    {
        public VatPostingWrapper(GLVatClient vat)
        {
            if (vat == null)
                return;

            Vat = vat;

            VatCode = vat.Vat;
            Account = vat.Account;
            AccountRate2 = vat.AccountRate2;
            OffsetAccount = vat.OffsetAccount;
            OffsetAccountRate2 = vat.OffsetAccountRate2;
            UnrealizedAccount = vat.UnrealizedAccount;

            FollowAccount = vat._FollowAccount;
            FollowAccount2 = vat._FollowAccount2;
        }

        public GLVatClient Vat { get; private set; }

        public string VatCode { get; private set; }

        public string Account { get; private set; }

        public string AccountRate2 { get; private set; }

        public string OffsetAccount { get; private set; }

        public string OffsetAccountRate2 { get; private set; }

        public string UnrealizedAccount { get; private set; }

        public bool FollowAccount { get; private set; }

        public bool FollowAccount2 { get; private set; }

        public string SourceOfVAT { get; set; }

        /// <summary>
        /// Gets or sets whether this VAT code was selected on the invoice without
        /// being supplied by a relevant item-group or financial-account setup.
        /// </summary>
        public bool IsManuallySelected { get; set; }

        public Type SourceTable => typeof(GLVatClient);
        public string SourceTableKey => string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceVatSourceKey"), VatCode);

    }
}



