using System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    public class SIECallbacks
    {
        public Action<string> Line { get; set; }

        public void CallbackLine(string message)
        {
            if (Line != null) Line(message);
        }

        public Action<Exception> SIEException { get; set; }
        public void CallbackException(Exception ex)
        {
            if (SIEException != null) SIEException(ex);
        }

        public Action<SIEPeriodValue> IB { get; set; }
        internal void CallbackIB(SIEPeriodValue pv)
        {
            if (IB != null) IB(pv);
        }

        public Action<SIEPeriodValue> UB { get; set; }
        internal void CallbackUB(SIEPeriodValue pv)
        {
            if (UB != null) UB(pv);
        }

        public Action<SIEPeriodValue> OIB { get; set; }
        internal void CallbackOIB(SIEPeriodValue pv)
        {
            if (OIB != null) OIB(pv);
        }

        public Action<SIEPeriodValue> OUB { get; set; }
        internal void CallbackOUB(SIEPeriodValue pv)
        {
            if (OUB != null) OUB(pv);
        }

        public Action<SIEPeriodValue> PSALDO { get; set; }
        internal void CallbackPSALDO(SIEPeriodValue pv)
        {
            if (PSALDO != null) PSALDO(pv);
        }

        public Action<SIEPeriodValue> PBUDGET { get; set; }
        internal void CallbackPBUDGET(SIEPeriodValue pv)
        {
            if (PBUDGET != null) PBUDGET(pv);
        }

        public Action<SIEPeriodValue> RES { get; set; }
        internal void CallbackRES(SIEPeriodValue pv)
        {
            if (RES != null) RES(pv);
        }

        public Action<SIEVoucher> VER { get; set; }
        internal void CallbackVER(SIEVoucher v)
        {
            if (VER != null) VER(v);
        }
    }
}
