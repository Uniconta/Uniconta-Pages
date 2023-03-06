using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.Models
{
    class VatReturnModel : BindableBase
    {

        #region Fields
        string _periodKey;
        double _vatDueSales;
        double _vatDueAcquisitions;
        double _vatReclaimedCurrPeriod;
        double _totalValueSalesExVAT;
        double _totalValuePurchasesExVAT;
        double _totalValueGoodsSuppliedExVAT;
        double _totalAcquisitionsExVAT;
        bool _finalised;
        #endregion

        #region Properties
        [JsonProperty("periodKey")]
        public string PeriodKey
        {
            get { return _periodKey; }
            set
            {

                _periodKey = value;
                NotifyPropertyChanged("PeriodKey");
            }
        }

        [BoxNumber(1)]
        [JsonProperty("vatDueSales")]
        public double VatDueSales
        {
            get { return _vatDueSales; }
            set
            {
                _vatDueSales = Math.Round(value, 2);
                NotifyPropertyChanged("VatDueSales");
                NotifyPropertyChanged("TotalVatDue");
                NotifyPropertyChanged("NetVatDue");
            }
        }

        [BoxNumber(2)]
        [JsonProperty("vatDueAcquisitions")]
        public double VatDueAcquisitions
        {
            get { return _vatDueAcquisitions; }
            set
            {

                _vatDueAcquisitions = Math.Round(value, 2);
                NotifyPropertyChanged("VatDueAcquisitions");
                NotifyPropertyChanged("TotalVatDue");
                NotifyPropertyChanged("NetVatDue");
            }
        }

        [JsonProperty("totalVatDue")]
        public double TotalVatDue
        {
            get { return Math.Round(VatDueSales + VatDueAcquisitions, 2); }
        }

        [BoxNumber(4)]
        [JsonProperty("vatReclaimedCurrPeriod")]
        public double VatReclaimedCurrPeriod
        {
            get { return _vatReclaimedCurrPeriod; }
            set
            {
                _vatReclaimedCurrPeriod = Math.Round(value, 2);
                NotifyPropertyChanged("VatReclaimedCurrPeriod");
                NotifyPropertyChanged("NetVatDue");

            }
        }

        [JsonProperty("netVatDue")]
        public double NetVatDue
        {
            get
            {
                if (TotalVatDue > VatReclaimedCurrPeriod)
                {
                    //return TotalVatDue - VatReclaimedCurrPeriod;

                    return Math.Round((VatDueSales + VatDueAcquisitions) - VatReclaimedCurrPeriod, 2);
                }
                else
                {
                    //return VatReclaimedCurrPeriod - TotalVatDue;
                    return Math.Round(VatReclaimedCurrPeriod - (VatDueSales + VatDueAcquisitions), 2);
                }
            }
        }
        [BoxNumber(6)]
        [JsonProperty("totalValueSalesExVAT")]
        public double TotalValueSalesExVAT
        {
            get { return _totalValueSalesExVAT; }
            set
            {

                _totalValueSalesExVAT = value;
                NotifyPropertyChanged("TotalValueSalesExVAT");
            }
        }

        [BoxNumber(7)]
        [JsonProperty("totalValuePurchasesExVAT")]
        public double TotalValuePurchasesExVAT
        {
            get { return _totalValuePurchasesExVAT; }
            set
            {
                _totalValuePurchasesExVAT = Math.Round(value, 0);
                NotifyPropertyChanged("TotalValuePurchasesExVAT");
            }
        }

        [BoxNumber(8)]
        [JsonProperty("totalValueGoodsSuppliedExVAT")]
        public double TotalValueGoodsSuppliedExVAT
        {
            get { return _totalValueGoodsSuppliedExVAT; }
            set
            {
                _totalValueGoodsSuppliedExVAT = Math.Round(value, 0);
                NotifyPropertyChanged("TotalValueGoodsSuppliedExVAT");
            }
        }


        [BoxNumber(9)]
        [JsonProperty("totalAcquisitionsExVAT")]
        public double TotalAcquisitionsExVAT
        {
            get { return _totalAcquisitionsExVAT; }
            set
            {
                _totalAcquisitionsExVAT = Math.Round(value, 0);
                NotifyPropertyChanged("TotalAcquisitionsExVAT");
            }
        }
        [JsonProperty("finalised")]
        public bool Finalised
        {
            get { return _finalised; }
            set
            {
                _finalised = value;
                NotifyPropertyChanged("Finalised");
            }
        }
        #endregion

        public void CalculateTotals()
        {
            TotalValuePurchasesExVAT = TotalValuePurchasesExVAT + TotalAcquisitionsExVAT;
            TotalValueSalesExVAT = TotalValueSalesExVAT + TotalValueGoodsSuppliedExVAT;
        }
    }
}
