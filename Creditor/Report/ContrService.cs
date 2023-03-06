using System;
using System.Runtime.Serialization;

namespace RSK.GagnaskilWS
{
    [DataContractAttribute(Name="AritunBifreidarSvar", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    public partial class AritunBifreidarSvar : object, System.Runtime.Serialization.IExtensibleDataObject
    {      
        private ExtensionDataObject extensionDataField;        
        private bool SaekjaListaTokstField;        
        private GagnaskilKlasar.AritunBifreidarKlasi[] ForskradarBifreidarField;        
        private DateTime TimastimpillField;        
        private string VillubodField;
        
        public ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [DataMemberAttribute(IsRequired=true)]
        public bool SaekjaListaTokst
        {
            get { return this.SaekjaListaTokstField; }
            set { this.SaekjaListaTokstField = value; }
        }
        
        [DataMemberAttribute(IsRequired=true, Order=1)]
        public GagnaskilKlasar.AritunBifreidarKlasi[] ForskradarBifreidar
        {
            get { return this.ForskradarBifreidarField; }
            set { this.ForskradarBifreidarField = value; }
        }
        
        [DataMemberAttribute(IsRequired=true, Order=2)]
        public System.DateTime Timastimpill
        {
            get { return this.TimastimpillField; }
            set { this.TimastimpillField = value; }
        }
        
        [DataMemberAttribute(Order=3)]
        public string Villubod
        {
            get { return this.VillubodField; }
            set { this.VillubodField = value; }
        }
    }
    
    [DataContractAttribute(Name="HeildarSamtolur", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    public partial class HeildarSamtolur : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;        
        private GagnaskilKlasar.LaunagreidandiKlasi LaunagreidandiKlasiField;        
        private bool TokstField;        
        private System.DateTime TimastimpillField;        
        private string VillubodField;        
        private GagnaskilKlasar.ReiturSummaKlasi[] ReiturSummaKlasiField;        
        private int FjoldiMidaField;        
        private int FjoldiVidtakendaField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [DataMemberAttribute(IsRequired=true)]
        public GagnaskilKlasar.LaunagreidandiKlasi LaunagreidandiKlasi
        {
            get { return this.LaunagreidandiKlasiField; }
            set { this.LaunagreidandiKlasiField = value; }
        }
        
        [DataMemberAttribute(IsRequired=true)]
        public bool Tokst
        {
            get { return this.TokstField; }
            set { this.TokstField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=2)]
        public System.DateTime Timastimpill
        {
            get { return this.TimastimpillField; }
            set { this.TimastimpillField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=3)]
        public string Villubod
        {
            get { return this.VillubodField; }
            set { this.VillubodField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=4)]
        public GagnaskilKlasar.ReiturSummaKlasi[] ReiturSummaKlasi
        {
            get { return this.ReiturSummaKlasiField; }
            set { this.ReiturSummaKlasiField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=5)]
        public int FjoldiMida
        {
            get { return this.FjoldiMidaField; }
            set { this.FjoldiMidaField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=6)]
        public int FjoldiVidtakenda
        {
            get { return this.FjoldiVidtakendaField; }
            set { this.FjoldiVidtakendaField = value; }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="PrentskraKlasi", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    public partial class PrentskraKlasi : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;        
        private byte[] PdfPrentskraField;        
        private bool TokstField;        
        private System.DateTime TimastimpillField;        
        private string VillubodField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public byte[] PdfPrentskra
        {
            get { return this.PdfPrentskraField; }
            set { this.PdfPrentskraField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public bool Tokst
        {
            get { return this.TokstField; }
            set { this.TokstField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=2)]
        public System.DateTime Timastimpill
        {
            get { return this.TimastimpillField; }
            set { this.TimastimpillField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=3)]
        public string Villubod
        {
            get { return this.VillubodField; }
            set { this.VillubodField = value; }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="SamtoluTalningar", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    public partial class SamtoluTalningar : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;        
        private string TegundField;        
        private string StaðaSendingarField;        
        private bool TokstField;        
        private System.DateTime TimastimpillField;        
        private string VillubodField;        
        private GagnaskilWS.SamtolurSendingar[] SamtolurField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string Tegund
        {
            get { return this.TegundField; }
            set { this.TegundField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=1)]
        public string StaðaSendingar
        {
            get { return this.StaðaSendingarField; }
            set { this.StaðaSendingarField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=2)]
        public bool Tokst
        {
            get { return this.TokstField; }
            set { this.TokstField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=3)]
        public System.DateTime Timastimpill
        {
            get { return this.TimastimpillField; }
            set { this.TimastimpillField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=4)]
        public string Villubod
        {
            get { return this.VillubodField; }
            set { this.VillubodField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=5)]
        public GagnaskilWS.SamtolurSendingar[] Samtolur
        {
            get { return this.SamtolurField; }
            set { this.SamtolurField = value; }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="SamtolurSendingar", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    public partial class SamtolurSendingar : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;        
        private string HeitiField;        
        private long FjarhaedField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string Heiti
        {
            get { return this.HeitiField; }
            set { this.HeitiField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=1)]
        public long Fjarhaed
        {
            get { return this.FjarhaedField; }
            set { this.FjarhaedField = value; }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="StadaSkila", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    public partial class StadaSkila : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;        
        private GagnaskilWS.SendingarYfirlit[] IVinnsluField;        
        private GagnaskilWS.SendingarYfirlit[] SkiladField;        
        private GagnaskilWS.SendingarYfirlit[] EyddarField;        
        private bool SaekjaListaTokstField;        
        private System.DateTime TimastimpillField;        
        private string VillubodField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public GagnaskilWS.SendingarYfirlit[] IVinnslu
        {
            get { return this.IVinnsluField; }
            set { this.IVinnsluField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public GagnaskilWS.SendingarYfirlit[] Skilad
        {
            get { return this.SkiladField; }
            set { this.SkiladField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=2)]
        public GagnaskilWS.SendingarYfirlit[] Eyddar
        {
            get { return this.EyddarField; }
            set { this.EyddarField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=3)]
        public bool SaekjaListaTokst
        {
            get { return this.SaekjaListaTokstField; }
            set { this.SaekjaListaTokstField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=4)]
        public System.DateTime Timastimpill
        {
            get { return this.TimastimpillField; }
            set { this.TimastimpillField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=5)]
        public string Villubod
        {
            get { return this.VillubodField; }
            set { this.VillubodField = value; }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="SendingarYfirlit", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    public partial class SendingarYfirlit : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;        
        private string TegundField;        
        private string StofnadAfField;        
        private System.DateTime DagsStofnadField;        
        private long SendingIdField;        
        private int FjoldiMidaField;        
        private string SkiladAfField;        
        private System.DateTime DagsSkiladField;        
        private string EyttAfField;        
        private System.DateTime DagsEyttField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string Tegund
        {
            get { return this.TegundField; }
            set { this.TegundField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=1)]
        public string StofnadAf
        {
            get { return this.StofnadAfField; }
            set { this.StofnadAfField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=2)]
        public System.DateTime DagsStofnad
        {
            get { return this.DagsStofnadField; }
            set { this.DagsStofnadField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=3)]
        public long SendingId
        {
            get { return this.SendingIdField; }
            set { this.SendingIdField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=4)]
        public int FjoldiMida
        {
            get { return this.FjoldiMidaField; }
            set { this.FjoldiMidaField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=5)]
        public string SkiladAf
        {
            get { return this.SkiladAfField; }
            set { this.SkiladAfField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=6)]
        public System.DateTime DagsSkilad
        {
            get { return this.DagsSkiladField; }
            set { this.DagsSkiladField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=7)]
        public string EyttAf
        {
            get { return this.EyttAfField; }
            set { this.EyttAfField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true, Order=8)]
        public System.DateTime DagsEytt
        {
            get { return this.DagsEyttField; }
            set { this.DagsEyttField = value; }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="GagnaskilFaultException", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    public partial class GagnaskilFaultException : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;        
        private int ErrorNumberField;        
        private string ErrorMsgField;        
        private string DescriptionField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public int ErrorNumber
        {
            get { return this.ErrorNumberField; }
            set { this.ErrorNumberField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(Order=1)]
        public string ErrorMsg
        {
            get { return this.ErrorMsgField; }
            set { this.ErrorMsgField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(Order=2)]
        public string Description
        {
            get { return this.DescriptionField; }
            set { this.DescriptionField = value; }
        }
    }
}
namespace GagnaskilKlasar
{
    using System.Runtime.Serialization;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="AritunBifreidarKlasi", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilKlasar")]
    public partial class AritunBifreidarKlasi : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;        
        private string _fastnumerField;        
        private System.DateTime _fyrstiSkraningardagurField;        
        private string _tegundField;        
        private string _undirtegundField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string _fastnumer
        {
            get { return this._fastnumerField; }
            set { this._fastnumerField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public System.DateTime _fyrstiSkraningardagur
        {
            get { return this._fyrstiSkraningardagurField; }
            set { this._fyrstiSkraningardagurField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string _tegund
        {
            get { return this._tegundField; }
            set { this._tegundField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string _undirtegund
        {
            get { return this._undirtegundField; }
            set { this._undirtegundField = value; }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="LaunagreidandiKlasi", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilKlasar")]
    public partial class LaunagreidandiKlasi : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;        
        private int m_iFjoldiLaunamannaField;        
        private int m_iFjoldiMidaField;        
        private GagnaskilKlasar.PersonaKlasi m_oLaunagreidandiField;        
        private GagnaskilKlasar.ReiturSummaKlasi[] m_oReitirSummaField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public int m_iFjoldiLaunamanna
        {
            get { return this.m_iFjoldiLaunamannaField; }
            set { this.m_iFjoldiLaunamannaField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public int m_iFjoldiMida
        {
            get { return this.m_iFjoldiMidaField; }
            set { this.m_iFjoldiMidaField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public GagnaskilKlasar.PersonaKlasi m_oLaunagreidandi
        {
            get { return this.m_oLaunagreidandiField; }
            set { this.m_oLaunagreidandiField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public GagnaskilKlasar.ReiturSummaKlasi[] m_oReitirSumma
        {
            get { return this.m_oReitirSummaField; }
            set { this.m_oReitirSummaField = value; }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="ReiturSummaKlasi", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilKlasar")]
    public partial class ReiturSummaKlasi : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;        
        private int m_fjoldiMidaField;        
        private int m_iFjoldiLaunamannaField;        
        private long m_lUpphaedField;        
        private string m_sMidiTegundHeitiField;        
        private string m_sReitaheitiField;        
        private string m_sReitanumerField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public int m_fjoldiMida
        {
            get { return this.m_fjoldiMidaField; }
            set { this.m_fjoldiMidaField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public int m_iFjoldiLaunamanna
        {
            get { return this.m_iFjoldiLaunamannaField; }
            set { this.m_iFjoldiLaunamannaField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public long m_lUpphaed
        {
            get { return this.m_lUpphaedField; }
            set { this.m_lUpphaedField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string m_sMidiTegundHeiti
        {
            get { return this.m_sMidiTegundHeitiField; }
            set { this.m_sMidiTegundHeitiField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string m_sReitaheiti
        {
            get { return this.m_sReitaheitiField; }
            set { this.m_sReitaheitiField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute(IsRequired=true)]
        public string m_sReitanumer
        {
            get { return this.m_sReitanumerField; }
            set { this.m_sReitanumerField = value; }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="PersonaKlasi", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilKlasar")]
    public partial class PersonaKlasi : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;       
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Runtime.Serialization.DataContractAttribute(Name="StadfestaKlasi", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilKlasar")]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(object[]))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(RSK.GagnaskilWS.AritunBifreidarSvar))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(RSK.GagnaskilWS.GagnaskilFaultException))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(RSK.GagnaskilWS.HeildarSamtolur))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(RSK.GagnaskilWS.PrentskraKlasi))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(RSK.GagnaskilWS.SamtoluTalningar))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(RSK.GagnaskilWS.SamtolurSendingar[]))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(RSK.GagnaskilWS.SamtolurSendingar))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(RSK.GagnaskilWS.StadaSkila))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(RSK.GagnaskilWS.SendingarYfirlit[]))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(RSK.GagnaskilWS.SendingarYfirlit))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(GagnaskilKlasar.AritunBifreidarKlasi[]))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(GagnaskilKlasar.AritunBifreidarKlasi))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(GagnaskilKlasar.LaunagreidandiKlasi))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(GagnaskilKlasar.PersonaKlasi))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(GagnaskilKlasar.ReiturSummaKlasi[]))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(GagnaskilKlasar.ReiturSummaKlasi))]
    public partial class StadfestaKlasi : object, System.Runtime.Serialization.IExtensibleDataObject
    {        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;        
        private string AthugasemdField;        
        private object[] AthugasemdirField;        
        private long BunkanumerField;        
        private object[] HafnadField;        
        private string KtlaunagreidandaField;        
        private byte[] PDFKvittunField;        
        private string TekjuarField;        
        private bool TokstField;        
        private string VillubodField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Athugasemd
        {
            get { return this.AthugasemdField; }
            set { this.AthugasemdField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public object[] Athugasemdir
        {
            get { return this.AthugasemdirField; }
            set { this.AthugasemdirField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public long Bunkanumer
        {
            get { return this.BunkanumerField; }
            set { this.BunkanumerField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public object[] Hafnad
        {
            get { return this.HafnadField; }
            set { this.HafnadField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Ktlaunagreidanda
        {
            get { return this.KtlaunagreidandaField; }
            set { this.KtlaunagreidandaField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public byte[] PDFKvittun
        {
            get { return this.PDFKvittunField; }
            set { this.PDFKvittunField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Tekjuar
        {
            get { return this.TekjuarField; }
            set { this.TekjuarField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public bool Tokst
        {
            get { return this.TokstField; }
            set { this.TokstField = value; }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Villubod
        {
            get { return this.VillubodField; }
            set { this.VillubodField = value; }
        }
    }
}

[System.ServiceModel.ServiceContractAttribute(ConfigurationName="IGagnaskilService")]
public interface IGagnaskilService
{    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/SaekjaAritadarBifreidar", ReplyAction="http://tempuri.org/IGagnaskilService/SaekjaAritadarBifreidarResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(RSK.GagnaskilWS.GagnaskilFaultException), Action="http://tempuri.org/IGagnaskilService/SaekjaAritadarBifreidarGagnaskilFaultExcepti" +
        "onFault", Name="GagnaskilFaultException", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    RSK.GagnaskilWS.AritunBifreidarSvar SaekjaAritadarBifreidar(string KennitalaEiganda, string Tekjuar, string Veflykill);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/SaekjaAritadarBifreidar", ReplyAction="http://tempuri.org/IGagnaskilService/SaekjaAritadarBifreidarResponse")]
    System.Threading.Tasks.Task<RSK.GagnaskilWS.AritunBifreidarSvar> SaekjaAritadarBifreidarAsync(string KennitalaEiganda, string Tekjuar, string Veflykill);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/SaekjaHeildarSamtoluLaunagreidanda", ReplyAction="http://tempuri.org/IGagnaskilService/SaekjaHeildarSamtoluLaunagreidandaResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(RSK.GagnaskilWS.GagnaskilFaultException), Action="http://tempuri.org/IGagnaskilService/SaekjaHeildarSamtoluLaunagreidandaGagnaskilF" +
        "aultExceptionFault", Name="GagnaskilFaultException", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    RSK.GagnaskilWS.HeildarSamtolur SaekjaHeildarSamtoluLaunagreidanda(string KennitalaLaunagreidanda, string Veflykill, string Tekjuar);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/SaekjaHeildarSamtoluLaunagreidanda", ReplyAction="http://tempuri.org/IGagnaskilService/SaekjaHeildarSamtoluLaunagreidandaResponse")]
    System.Threading.Tasks.Task<RSK.GagnaskilWS.HeildarSamtolur> SaekjaHeildarSamtoluLaunagreidandaAsync(string KennitalaLaunagreidanda, string Veflykill, string Tekjuar);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/SaekjaPrentskra", ReplyAction="http://tempuri.org/IGagnaskilService/SaekjaPrentskraResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(RSK.GagnaskilWS.GagnaskilFaultException), Action="http://tempuri.org/IGagnaskilService/SaekjaPrentskraGagnaskilFaultExceptionFault", Name="GagnaskilFaultException", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    RSK.GagnaskilWS.PrentskraKlasi SaekjaPrentskra(string KennitalaLaunagreidanda, string Veflykill, long SendingId);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/SaekjaPrentskra", ReplyAction="http://tempuri.org/IGagnaskilService/SaekjaPrentskraResponse")]
    System.Threading.Tasks.Task<RSK.GagnaskilWS.PrentskraKlasi> SaekjaPrentskraAsync(string KennitalaLaunagreidanda, string Veflykill, long SendingId);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/SaekjaSamtoluSendingar", ReplyAction="http://tempuri.org/IGagnaskilService/SaekjaSamtoluSendingarResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(RSK.GagnaskilWS.GagnaskilFaultException), Action="http://tempuri.org/IGagnaskilService/SaekjaSamtoluSendingarGagnaskilFaultExceptio" +
        "nFault", Name="GagnaskilFaultException", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    RSK.GagnaskilWS.SamtoluTalningar SaekjaSamtoluSendingar(string KennitalaLaunagreidanda, string Veflykill, long SendingId);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/SaekjaSamtoluSendingar", ReplyAction="http://tempuri.org/IGagnaskilService/SaekjaSamtoluSendingarResponse")]
    System.Threading.Tasks.Task<RSK.GagnaskilWS.SamtoluTalningar> SaekjaSamtoluSendingarAsync(string KennitalaLaunagreidanda, string Veflykill, long SendingId);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/SaekjaStoduSkila", ReplyAction="http://tempuri.org/IGagnaskilService/SaekjaStoduSkilaResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(RSK.GagnaskilWS.GagnaskilFaultException), Action="http://tempuri.org/IGagnaskilService/SaekjaStoduSkilaGagnaskilFaultExceptionFault" +
        "", Name="GagnaskilFaultException", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    RSK.GagnaskilWS.StadaSkila SaekjaStoduSkila(string KennitalaLaunagreidanda, string Veflykill, string Tekjuar);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/SaekjaStoduSkila", ReplyAction="http://tempuri.org/IGagnaskilService/SaekjaStoduSkilaResponse")]
    System.Threading.Tasks.Task<RSK.GagnaskilWS.StadaSkila> SaekjaStoduSkilaAsync(string KennitalaLaunagreidanda, string Veflykill, string Tekjuar);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/Senda", ReplyAction="http://tempuri.org/IGagnaskilService/SendaResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(RSK.GagnaskilWS.GagnaskilFaultException), Action="http://tempuri.org/IGagnaskilService/SendaGagnaskilFaultExceptionFault", Name="GagnaskilFaultException", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    GagnaskilKlasar.StadfestaKlasi Senda(System.Xml.XmlElement XmlSkjal);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/Senda", ReplyAction="http://tempuri.org/IGagnaskilService/SendaResponse")]
    System.Threading.Tasks.Task<GagnaskilKlasar.StadfestaKlasi> SendaAsync(System.Xml.XmlElement XmlSkjal);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/Villuprofa", ReplyAction="http://tempuri.org/IGagnaskilService/VilluprofaResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(RSK.GagnaskilWS.GagnaskilFaultException), Action="http://tempuri.org/IGagnaskilService/VilluprofaGagnaskilFaultExceptionFault", Name="GagnaskilFaultException", Namespace="http://schemas.datacontract.org/2004/07/GagnaskilWS")]
    GagnaskilKlasar.StadfestaKlasi Villuprofa(System.Xml.XmlElement XmlSkjal);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IGagnaskilService/Villuprofa", ReplyAction="http://tempuri.org/IGagnaskilService/VilluprofaResponse")]
    System.Threading.Tasks.Task<GagnaskilKlasar.StadfestaKlasi> VilluprofaAsync(System.Xml.XmlElement XmlSkjal);
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
public interface IGagnaskilServiceChannel : IGagnaskilService, System.ServiceModel.IClientChannel
{
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
public partial class GagnaskilServiceClient : System.ServiceModel.ClientBase<IGagnaskilService>, IGagnaskilService
{
    
    public GagnaskilServiceClient()
    {
    }
    
    public GagnaskilServiceClient(string endpointConfigurationName) : 
            base(endpointConfigurationName)
    {
    }
    
    public GagnaskilServiceClient(string endpointConfigurationName, string remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
    {
    }
    
    public GagnaskilServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
    {
    }
    
    public GagnaskilServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(binding, remoteAddress)
    {
    }
    
    public RSK.GagnaskilWS.AritunBifreidarSvar SaekjaAritadarBifreidar(string KennitalaEiganda, string Tekjuar, string Veflykill)
    {
        return base.Channel.SaekjaAritadarBifreidar(KennitalaEiganda, Tekjuar, Veflykill);
    }
    
    public System.Threading.Tasks.Task<RSK.GagnaskilWS.AritunBifreidarSvar> SaekjaAritadarBifreidarAsync(string KennitalaEiganda, string Tekjuar, string Veflykill)
    {
        return base.Channel.SaekjaAritadarBifreidarAsync(KennitalaEiganda, Tekjuar, Veflykill);
    }
    
    public RSK.GagnaskilWS.HeildarSamtolur SaekjaHeildarSamtoluLaunagreidanda(string KennitalaLaunagreidanda, string Veflykill, string Tekjuar)
    {
        return base.Channel.SaekjaHeildarSamtoluLaunagreidanda(KennitalaLaunagreidanda, Veflykill, Tekjuar);
    }
    
    public System.Threading.Tasks.Task<RSK.GagnaskilWS.HeildarSamtolur> SaekjaHeildarSamtoluLaunagreidandaAsync(string KennitalaLaunagreidanda, string Veflykill, string Tekjuar)
    {
        return base.Channel.SaekjaHeildarSamtoluLaunagreidandaAsync(KennitalaLaunagreidanda, Veflykill, Tekjuar);
    }
    
    public RSK.GagnaskilWS.PrentskraKlasi SaekjaPrentskra(string KennitalaLaunagreidanda, string Veflykill, long SendingId)
    {
        return base.Channel.SaekjaPrentskra(KennitalaLaunagreidanda, Veflykill, SendingId);
    }
    
    public System.Threading.Tasks.Task<RSK.GagnaskilWS.PrentskraKlasi> SaekjaPrentskraAsync(string KennitalaLaunagreidanda, string Veflykill, long SendingId)
    {
        return base.Channel.SaekjaPrentskraAsync(KennitalaLaunagreidanda, Veflykill, SendingId);
    }
    
    public RSK.GagnaskilWS.SamtoluTalningar SaekjaSamtoluSendingar(string KennitalaLaunagreidanda, string Veflykill, long SendingId)
    {
        return base.Channel.SaekjaSamtoluSendingar(KennitalaLaunagreidanda, Veflykill, SendingId);
    }
    
    public System.Threading.Tasks.Task<RSK.GagnaskilWS.SamtoluTalningar> SaekjaSamtoluSendingarAsync(string KennitalaLaunagreidanda, string Veflykill, long SendingId)
    {
        return base.Channel.SaekjaSamtoluSendingarAsync(KennitalaLaunagreidanda, Veflykill, SendingId);
    }
    
    public RSK.GagnaskilWS.StadaSkila SaekjaStoduSkila(string KennitalaLaunagreidanda, string Veflykill, string Tekjuar)
    {
        return base.Channel.SaekjaStoduSkila(KennitalaLaunagreidanda, Veflykill, Tekjuar);
    }
    
    public System.Threading.Tasks.Task<RSK.GagnaskilWS.StadaSkila> SaekjaStoduSkilaAsync(string KennitalaLaunagreidanda, string Veflykill, string Tekjuar)
    {
        return base.Channel.SaekjaStoduSkilaAsync(KennitalaLaunagreidanda, Veflykill, Tekjuar);
    }
    
    public GagnaskilKlasar.StadfestaKlasi Senda(System.Xml.XmlElement XmlSkjal)
    {
        return base.Channel.Senda(XmlSkjal);
    }
    
    public System.Threading.Tasks.Task<GagnaskilKlasar.StadfestaKlasi> SendaAsync(System.Xml.XmlElement XmlSkjal)
    {
        return base.Channel.SendaAsync(XmlSkjal);
    }
    
    public GagnaskilKlasar.StadfestaKlasi Villuprofa(System.Xml.XmlElement XmlSkjal)
    {
        return base.Channel.Villuprofa(XmlSkjal);
    }
    
    public System.Threading.Tasks.Task<GagnaskilKlasar.StadfestaKlasi> VilluprofaAsync(System.Xml.XmlElement XmlSkjal)
    {
        return base.Channel.VilluprofaAsync(XmlSkjal);
    }
}