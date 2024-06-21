using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;
using Uniconta.Common.Utility;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools.Controls;
using System.Text.RegularExpressions;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWNemhandel : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "ReceiveInvoice", ResourceType = typeof(InputFieldDataText))]
        public bool ReceiveInvoiceGLN { get; set; }

        [InputFieldData]
        [Display(Name = "ReceiveOrder", ResourceType = typeof(InputFieldDataText))]
        public bool ReceiveOrderGLN { get; set; }

        [InputFieldData]
        [Display(Name = "ReceiveStatement", ResourceType = typeof(InputFieldDataText))]
        public bool ReceiveStatementGLN { get; set; }

        [InputFieldData]
        [Display(Name = "ReceiveInvoice", ResourceType = typeof(InputFieldDataText))]
        public bool ReceiveInvoiceCVR { get; set; }

        [InputFieldData]
        [Display(Name = "ReceiveOrder", ResourceType = typeof(InputFieldDataText))]
        public bool ReceiveOrderCVR { get; set; }

        [InputFieldData]
        [Display(Name = "ReceiveStatement", ResourceType = typeof(InputFieldDataText))]
        public bool ReceiveStatementCVR { get; set; }

        [InputFieldData]
        [Display(Name = "Unregister", ResourceType = typeof(InputFieldDataText))]
        public bool UnregisterGLN { get; set; }

        [InputFieldData]
        [Display(Name = "RegisterTime", ResourceType = typeof(InputFieldDataText))]
        public DateTime DateGLN { get; set; }

        [InputFieldData]
        [Display(Name = "Immediately", ResourceType = typeof(InputFieldDataText))]
        public bool ImmediatelyGLN { get; set; }

        [InputFieldData]
        [Display(Name = "Unregister", ResourceType = typeof(InputFieldDataText))]
        public bool UnregisterCVR { get; set; }

        [InputFieldData]
        [Display(Name = "RegistrationTime", ResourceType = typeof(InputFieldDataText))]
        public DateTime DateCVR { get; set; }

        [InputFieldData]
        [Display(Name = "Immediately", ResourceType = typeof(InputFieldDataText))]
        public bool ImmediatelyCVR { get; set; }

        [InputFieldData]
        [Display(Name = "ChangeRegistrationCompany", ResourceType = typeof(InputFieldDataText))]
        public bool ChangeCompany { get; set; }

        readonly Company Comp;
        CrudAPI api;
        public List<eDeliveryNHR> EndPointLst;
        public bool HasChanges;
        bool hasRegistration = false;
        bool hasOtherCompany = false;
        bool hasOtherProvider = false;
        DateTime otherProviderExpirationDate = DateTime.MinValue;
        bool activateOKButton = false;
        bool isLive;

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }

        public CWNemhandel(CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();
            this.api = api;
            Comp = api.CompanyEntity;
            Title = Uniconta.ClientTools.Localization.lookup("NHR");
            OKButton.IsEnabled = false;
            isLive = api.session.Connection.Target == APITarget.Live;

            Loaded += CWNemhandel_Loaded;
        }

        private async void CWNemhandel_Loaded(object sender, RoutedEventArgs e)
        {
            busyIndicator.IsBusy = true;
            EndPointLst = await NHR.Lookup(api);
            busyIndicator.IsBusy = false;

            if (EndPointLst == null)
                return;

            string otherCompanyName = null;
            string otherProviderName = null;
            string otherProviderEmail = null;
            bool hasCompanyAccess = false;
            bool GLNnotRegistered = false;
            bool GLNFuture = false;
            bool CVRFuture = false;

            int otherCompanyId = 0;

            grpFooter.Visibility = grpHeader.Visibility = Visibility.Visible;
            lblUnipedia.Content = UtilDisplay.CreateHyperLinkTextControl(GetLinkForUnipedia(), Uniconta.ClientTools.Localization.lookup("AlwaysCheckUnipedia"));
            var otherProvider = EndPointLst.FirstOrDefault(s => s.OtherProvider);
            hasOtherProvider = otherProvider != null;
            if (hasOtherProvider && otherProvider.ExpirationDate == DateTime.MinValue)
            {
                OKButton.IsEnabled = true;
                otherProviderName = string.Concat(otherProvider.EndPointContactName, " - ", otherProvider.EndPointRegisterName);
                otherProviderEmail = otherProvider.EndPointContactEmail;
                grpGLNHeader.Visibility = Visibility.Collapsed;
                grpCVRHeader.Visibility = Visibility.Visible;
                chkReceiveInvoiceCVR.Visibility = Visibility.Collapsed;
                liReceiveInvoiceCVR.Visibility = Visibility.Collapsed;
                chkReceiveOrderCVR.Visibility = Visibility.Collapsed;
                liReceiveOrderCVR.Visibility = Visibility.Collapsed;
                chkReceiveStatementCVR.Visibility = Visibility.Collapsed;
                liReceiveStatementCVR.Visibility = Visibility.Collapsed;

                var cvrNumber = Regex.Replace(Comp._Id, "[^0-9]", "");

                var cvrUrl = string.Concat(isLive ? NHR.NHR_WEB : NHR.NHR_WEB_DEMO, "DK%3ACVR&key=", cvrNumber);
               
                lblEndPointCVR.Content = UtilDisplay.CreateHyperLinkTextControl(cvrUrl, string.Concat(Uniconta.ClientTools.Localization.lookup("CompanyRegNo"), ": ", cvrNumber));

                txtHeader.Text = StringBuilderReuse.Create().Append("Registreringerne vedrørende CVR-nummer ").Append(cvrNumber).Append(" er tilknyttet en anden udbyder.").AppendLine().AppendLine().
                    Append(Uniconta.ClientTools.Localization.lookup("Name")).Append(": ").AppendLine(otherProviderName).
                    Append(Uniconta.ClientTools.Localization.lookup("Email")).Append(": ").Append(otherProviderEmail).AppendLine().ToStringAndRelease();

                txtFooter.Text = StringBuilderReuse.Create().AppendLine("For at kunne modtage dokumenter via Nemhandel direkte i Uniconta, så skal der skiftes udbyder, så det bliver Uniconta der fremover modtager dokumenterne.").AppendLine().Append("Kontakt den nuværende udbyder og få dem til at deaktivere jeres registrering hos dem. Det anbefales, at aftale dato og tidspunkt for deaktiveringen, så skiftet bliver så smidigt som muligt og ikke får konsekvenser for tilgængeligheden. Det anbefales, at vælge en fredag sent om aftenen, så det påvirker færrest muligt.").ToStringAndRelease();

                CancelButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                foreach (var endPoint in EndPointLst)
                {
                    if (endPoint.RegisteredCompanyId != 0 && endPoint.RegisteredCompanyId != Comp.CompanyId)
                    {
                        hasOtherCompany = true;
                        otherCompanyId = endPoint.RegisteredCompanyId;
                        otherCompanyName = endPoint.RegisteredCompanyName;
                        hasCompanyAccess = !endPoint.ChangeCompanyNoAccess;
                        chkChangeCompany.IsEnabled = !endPoint.ChangeCompanyNoAccess;
                        grpCompany.Visibility = Visibility.Visible;
                        liImmediatelyGLN.Visibility = Visibility.Collapsed;
                        liDateGLN.Visibility = Visibility.Collapsed;
                        liImmediatelyCVR.Visibility = Visibility.Collapsed;
                        liDateCVR.Visibility = Visibility.Collapsed;
                        chkReceiveOrderGLN.IsEnabled = false;
                        chkReceiveStatementGLN.IsEnabled = false;
                        chkUnregisterGLN.IsEnabled = false;
                        chkReceiveOrderCVR.IsEnabled = false;
                        chkReceiveStatementCVR.IsEnabled = false;
                        chkUnregisterCVR.IsEnabled = false;
                    }

                    if (endPoint.KeyType == NHREndPointType.GLN)
                    {
                        grpGLNHeader.Visibility = Visibility.Visible;

                        var glnUrl = string.Concat(isLive ? NHR.NHR_WEB : NHR.NHR_WEB_DEMO, endPoint.KeyType, "&key=", endPoint.Key);

                        lblEndPointGLN.Content = UtilDisplay.CreateHyperLinkTextControl(glnUrl, string.Concat(Uniconta.ClientTools.Localization.lookup("GLNnumber"), ": ", endPoint.Key));

                        if (hasOtherProvider)
                        {
                            otherProviderExpirationDate = endPoint.ExpirationDate;
                            OKButton.IsEnabled = true;
                            chkReceiveInvoiceGLN.IsChecked = true;
                            chkUnregisterGLN.IsChecked = false;
                            DateGLN = otherProviderExpirationDate.AddMinutes(1);
                            NotifyPropertyChanged("DateGLN");
                            dtDateGLN.IsEnabled = false;
                            liDateGLN.Visibility = Visibility.Visible;
                            liImmediatelyGLN.Visibility = Visibility.Collapsed;
                            endPoint.NotRegistered = true;
                        }
                        else if (endPoint.NotRegistered)
                        {
                            OKButton.IsEnabled = true;
                            GLNnotRegistered = true;
                            chkReceiveInvoiceGLN.IsChecked = true;
                            chkImmediatelyGLN.IsChecked = true;
                            liImmediatelyGLN.Label = Uniconta.ClientTools.Localization.lookup("RegisterImmediately");
                            liImmediatelyGLN.Visibility = hasOtherCompany ? Visibility.Collapsed : Visibility.Visible;
                        }
                        else if (endPoint.FutureRegister)
                        {
                            OKButton.IsEnabled = !hasOtherCompany;
                            GLNFuture = true;
                            chkReceiveInvoiceGLN.IsChecked = endPoint.ReceiveInvoice;
                            chkReceiveOrderGLN.IsChecked = endPoint.ReceiveOrder;
                            chkReceiveStatementGLN.IsChecked = endPoint.ReceiveStatement;
                            chkImmediatelyGLN.IsChecked = false;
                            DateGLN = endPoint.ActivationDate;
                            NotifyPropertyChanged("DateGLN");
                            dtDateGLN.IsEnabled = !hasOtherCompany;
                            liDateGLN.Visibility = Visibility.Visible;
                            liImmediatelyGLN.Label = Uniconta.ClientTools.Localization.lookup("RegisterImmediately");
                            liImmediatelyGLN.Visibility = hasOtherCompany ? Visibility.Collapsed : Visibility.Visible;
                        }
                        else
                        {
                            hasRegistration = true;
                            chkReceiveInvoiceGLN.IsChecked = endPoint.ReceiveInvoice;

                            chkReceiveOrderGLN.IsChecked = endPoint.ReceiveOrder;
                            chkReceiveOrderGLN.IsEnabled = !hasOtherCompany;

                            chkReceiveStatementGLN.IsChecked = endPoint.ReceiveStatement;
                            chkReceiveStatementGLN.IsEnabled = !hasOtherCompany;

                            liUnRegisterGLN.Visibility = hasOtherCompany ? Visibility.Collapsed : Visibility.Visible;
                            liDateGLN.Visibility = Visibility.Collapsed;

                            liImmediatelyGLN.Visibility = Visibility.Collapsed;
                            chkImmediatelyGLN.Visibility = Visibility.Collapsed;
                            liImmediatelyGLN.Label = Uniconta.ClientTools.Localization.lookup("UnregisterImmediately");
                            liDateGLN.Label = Uniconta.ClientTools.Localization.lookup("UnregistrationTime");
                            if (endPoint.Unregister)
                            {
                                DateGLN = endPoint.ExpirationDate;
                                chkUnregisterGLN.IsChecked = true;
                                chkImmediatelyGLN.IsChecked = false;
                            }
                        }
                    }
                    else if (endPoint.KeyType == NHREndPointType.CVR)
                    {
                        grpCVRHeader.Visibility = Visibility.Visible;

                        var cvrUrl = string.Concat(isLive ? NHR.NHR_WEB : NHR.NHR_WEB_DEMO, "DK%3ACVR&key=", endPoint.Key);

                        lblEndPointCVR.Content = UtilDisplay.CreateHyperLinkTextControl(cvrUrl, string.Concat(Uniconta.ClientTools.Localization.lookup("CompanyRegNo"), ": ", endPoint.Key));
                        if (hasOtherProvider)
                        {
                            otherProviderExpirationDate = endPoint.ExpirationDate;
                            OKButton.IsEnabled = true;
                            chkReceiveInvoiceCVR.IsChecked = true;
                            chkUnregisterCVR.IsChecked = false;
                            DateCVR = otherProviderExpirationDate.AddMinutes(1);
                            NotifyPropertyChanged("DateCVR");
                            dtDateCVR.IsEnabled = false;
                            liDateCVR.Visibility = Visibility.Visible;
                            liImmediatelyCVR.Visibility = Visibility.Collapsed;
                            endPoint.NotRegistered = true;
                        }
                        else if (endPoint.NotRegistered)
                        {
                            OKButton.IsEnabled = true;

                            if (chkReceiveInvoiceGLN.IsChecked == false)
                                chkReceiveInvoiceCVR.IsChecked = true;

                            chkImmediatelyCVR.IsChecked = true;
                            liImmediatelyCVR.Label = Uniconta.ClientTools.Localization.lookup("RegisterImmediately");
                            liImmediatelyCVR.Visibility = hasOtherCompany ? Visibility.Collapsed : Visibility.Visible;
                        }
                        else if (endPoint.FutureRegister)
                        {
                            OKButton.IsEnabled = !hasOtherCompany;
                            CVRFuture = true;
                            chkReceiveInvoiceCVR.IsChecked = endPoint.ReceiveInvoice;
                            chkReceiveOrderCVR.IsChecked = endPoint.ReceiveOrder;
                            chkReceiveStatementCVR.IsChecked = endPoint.ReceiveStatement;
                            chkUnregisterCVR.IsChecked = false;
                            DateCVR = endPoint.ActivationDate;
                            NotifyPropertyChanged("DateCVR");
                            dtDateCVR.IsEnabled = !hasOtherCompany;
                            liDateCVR.Visibility = Visibility.Visible;
                            liImmediatelyCVR.Label = Uniconta.ClientTools.Localization.lookup("RegisterImmediately");
                            liImmediatelyCVR.Visibility = hasOtherCompany ? Visibility.Collapsed : Visibility.Visible;
                        }
                        else
                        {
                            hasRegistration = true;
                            chkReceiveInvoiceCVR.IsChecked = endPoint.ReceiveInvoice;

                            chkReceiveOrderCVR.IsChecked = endPoint.ReceiveOrder;
                            chkReceiveOrderCVR.IsEnabled = !hasOtherCompany;

                            chkReceiveStatementCVR.IsChecked = endPoint.ReceiveStatement;
                            chkReceiveStatementCVR.IsEnabled = !hasOtherCompany;

                            liUnRegisterCVR.Visibility = hasOtherCompany ? Visibility.Collapsed : Visibility.Visible;
                            liDateCVR.Visibility = Visibility.Collapsed;

                            liImmediatelyCVR.Visibility = Visibility.Collapsed;
                            chkImmediatelyCVR.Visibility = Visibility.Collapsed;
                            liImmediatelyCVR.Label = Uniconta.ClientTools.Localization.lookup("UnregisterImmediately");
                            liDateCVR.Label = Uniconta.ClientTools.Localization.lookup("UnregistrationTime");
                            if (endPoint.Unregister)
                            {
                                DateCVR = endPoint.ExpirationDate;
                                chkUnregisterCVR.IsChecked = true;
                                chkImmediatelyCVR.IsChecked = false;
                            }
                        }
                    }
                }

                var sb = StringBuilderReuse.Create();
                if (hasOtherCompany)
                    txtHeader.Text = sb.Append("Nedenstående registrering er tilknyttet et andet regnskab").Append(" (").Append(otherCompanyId).Append(") ").Append(otherCompanyName).Append(hasCompanyAccess ? null : string.Concat("- ", Uniconta.ClientTools.Localization.lookup("UserNoAccessToCompany"))).ToStringAndRelease();
                else if (hasRegistration)
                {
                    sb.Append(Uniconta.ClientTools.Localization.lookup("RegistrationInNHR")).Append(" ").Append(Comp._Id).Append(" (").Append(Comp.Name).Append(")");
                    if (GLNnotRegistered)
                        sb.AppendLine().AppendLine().Append("Nedenstående GLN-nummer er ikke registreret i Nemhandelsregisteret. Tryk OK for at registrere det.");
                    else if (GLNFuture)
                        sb.AppendLine().AppendLine().Append("GLN-nummer er sat til en fremtidig tilmeldingsdato.");
                    else if (CVRFuture)
                        sb.AppendLine().AppendLine().Append("CVR-nummer er sat til en fremtidig tilmeldingsdato.");
                    txtHeader.Text = sb.ToStringAndRelease();
                }
                else
                {
                    ActivateOKButton();

                    if (hasOtherProvider)
                    {
                        sb.Append("Registreringerne vedrørende CVR-nummer ").Append(Comp._Id).Append(" er tilknyttet en anden udbyder (").Append(otherProviderName).AppendLine(")").AppendLine().
                        Append("Den anden udbyder har sat registreringen til at være afmeldt den ").AppendLine(otherProviderExpirationDate.ToString("dd.MM.yyyy kl. HH:mm")).AppendLine().
                        Append("Tryk OK for at registrere nedenstående GLN-nummer og/eller CVR-nummer. Sæt eventuelt hak i 'Modtag ordre'. Ordre-dokumenter oprettes som salgsordre i Uniconta.");
                    }
                    else
                    {
                        sb.Append(Uniconta.ClientTools.Localization.lookup("NoRegistrationInNHR")).Append(" ").Append(Comp._Id).Append(" (").Append(Comp.Name).Append(")");


                        if (GLNnotRegistered)
                            sb.AppendLine().AppendLine().Append("Tryk OK for at registrere nedenstående GLN-nummer. Sæt eventuelt hak i 'Modtag ordre'. Ordre-dokumenter oprettes som salgsordre i Uniconta.");
                        else
                            sb.AppendLine().AppendLine().Append("Tryk OK for at registrere nedenstående CVR-nummer. Sæt eventuelt hak i 'Modtag ordre'. Ordre-dokumenter oprettes som salgsordre i Uniconta.");

                        if (GLNFuture)
                        {
                            sb.AppendLine().AppendLine().Append("GLN-nummer er sat til en fremtidig tilmeldingsdato.");
                            if (CVRFuture)
                                sb.AppendLine().Append("CVR-nummer er sat til en fremtidig tilmeldingsdato.");
                        }
                        else if (CVRFuture)
                        {
                            sb.AppendLine().AppendLine().Append("CVR-nummer er sat til en fremtidig tilmeldingsdato.");
                        }
                    }
                    txtHeader.Text = sb.ToStringAndRelease();
                }
            }
        }

        private string GetLinkForUnipedia()
        {
            return "https://www.uniconta.com/da/unipedia/tilmeldnemhandel/";
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (hasOtherProvider && otherProviderExpirationDate == DateTime.MinValue)
                SetDialogResult(false);
            else
            {
                foreach (var endPoint in EndPointLst)
                {
                    if (endPoint.NotRegistered)
                        HasChanges = true;

                    if (endPoint.KeyType == NHREndPointType.GLN)
                        UpdateEndPointProperties(endPoint, ReceiveOrderGLN, ReceiveStatementGLN, UnregisterGLN, ImmediatelyGLN, DateGLN);
                    else if (endPoint.KeyType == NHREndPointType.CVR)
                        UpdateEndPointProperties(endPoint, ReceiveOrderCVR, ReceiveStatementCVR, UnregisterCVR, ImmediatelyCVR, DateCVR);

                    if (endPoint.ChangeCompany != ChangeCompany)
                    {
                        endPoint.ChangeCompany = ChangeCompany;
                        HasChanges = true;
                    }
                }
                SetDialogResult(true);
            }
        }

        private void UpdateEndPointProperties(eDeliveryNHR endPoint, bool receiveOrder, bool receiveStatement, bool unregister, bool Immediately, DateTime dateTime)
        {
            if (endPoint.ReceiveOrder != receiveOrder)
            {
                endPoint.ReceiveOrder = receiveOrder;
                endPoint.Update = true;
                HasChanges = true;
            }

            if (endPoint.ReceiveStatement != receiveStatement)
            {
                endPoint.ReceiveStatement = receiveStatement;
                endPoint.Update = true;
                HasChanges = true;
            }

            if (endPoint.NotRegistered)
            {
                HasChanges = true;
                if (Immediately)
                    endPoint.ActivationDate = DateTime.MinValue;
                else
                    endPoint.ActivationDate = dateTime < DateTime.Now.AddMinutes(10) ? DateTime.Now.AddMinutes(15) : dateTime;
            }
            else if (endPoint.FutureRegister && ((dateTime != DateTime.MinValue && endPoint.ActivationDate != dateTime) || Immediately))
            {
                if (Immediately)
                    endPoint.ActivationDate = DateTime.MinValue;
                else
                    endPoint.ActivationDate = dateTime < DateTime.Now.AddMinutes(10) ? DateTime.Now.AddMinutes(15) : dateTime;
                HasChanges = true;
                endPoint.Update = true;
            }
            else if (unregister && endPoint.Unregister && endPoint.ExpirationDate != dateTime || Immediately)
            {
                if (Immediately)
                    endPoint.ExpirationDate = DateTime.MinValue;
                else
                    endPoint.ExpirationDate = dateTime < DateTime.Now.AddMinutes(10) ? DateTime.Now.AddMinutes(15) : dateTime;
                endPoint.Unregister = unregister;
                endPoint.Update = true;
                HasChanges = true;
            }
            else if (endPoint.Unregister != unregister)
            {
                endPoint.Unregister = unregister;
                if (!unregister)
                    endPoint.ExpirationDate = DateTime.MinValue;
                else if (Immediately)
                    endPoint.ExpirationDate = DateTime.MinValue;
                else
                    endPoint.ExpirationDate = dateTime < DateTime.Now.AddMinutes(10) ? DateTime.Now.AddMinutes(15) : dateTime;
                HasChanges = true;
                endPoint.Update = true;
            }
        }

        private void ActivateOKButton()
        {
            if (hasOtherProvider || EndPointLst == null)
                return;

            activateOKButton = false;
            foreach (var endPoint in EndPointLst)
            {
                if (endPoint.NotRegistered)
                    activateOKButton = true;

                if (endPoint.KeyType == NHREndPointType.GLN)
                    CheckValues(endPoint, ReceiveOrderGLN, ReceiveStatementGLN, UnregisterGLN, dtDateGLN.DateTime);
                else if (endPoint.KeyType == NHREndPointType.CVR)
                    CheckValues(endPoint, ReceiveOrderCVR, ReceiveStatementCVR, UnregisterCVR, dtDateCVR.DateTime);
            }

            OKButton.IsEnabled = activateOKButton;
        }

        private void CheckValues(eDeliveryNHR endPoint, bool receiveOrder, bool receiveStatement, bool unregister, DateTime dateTime)
        {
            if (endPoint.ReceiveOrder != receiveOrder || endPoint.ReceiveStatement != receiveStatement || endPoint.NotRegistered ||
                (unregister && endPoint.Unregister && dateTime != DateTime.MinValue && endPoint.ExpirationDate != dateTime) ||
                endPoint.Unregister != unregister || (endPoint.FutureRegister && !hasOtherCompany) || endPoint.ChangeCompany != ChangeCompany ||
                (unregister && (ImmediatelyCVR || ImmediatelyGLN)))
                activateOKButton = true;
        }

        private void chkImmediatelyCVR_Checked(object sender, RoutedEventArgs e)
        {
            liDateCVR.Visibility = Visibility.Collapsed;
            ActivateOKButton();
        }

        private void chkImmediatelyCVR_Unchecked(object sender, RoutedEventArgs e)
        {
            liDateCVR.Visibility = hasOtherCompany ? Visibility.Collapsed : Visibility.Visible;
            DateCVR = DateCVR == DateTime.MinValue || DateCVR < DateTime.Now.AddMinutes(10) ? DateTime.Now.AddMinutes(15) : DateCVR;
            NotifyPropertyChanged("DateCVR");
        }

        private void chkImmediatelyGLN_Checked(object sender, RoutedEventArgs e)
        {
            liDateGLN.Visibility = Visibility.Collapsed;
            ActivateOKButton();
        }

        private void chkImmediatelyGLN_Unchecked(object sender, RoutedEventArgs e)
        {
            liDateGLN.Visibility = hasOtherCompany ? Visibility.Collapsed : Visibility.Visible;
            DateGLN = DateGLN == DateTime.MinValue || DateGLN < DateTime.Now.AddMinutes(10) ? DateTime.Now.AddMinutes(15) : DateGLN;
            NotifyPropertyChanged("DateGLN");
        }

        private void chkUnregisterGLN_Checked(object sender, RoutedEventArgs e)
        {
            liImmediatelyGLN.Visibility = chkImmediatelyGLN.Visibility = hasOtherCompany ? Visibility.Collapsed : Visibility.Visible;
            chkImmediatelyGLN.IsChecked = true;
            chkReceiveOrderGLN.IsEnabled = false;
            chkReceiveStatementGLN.IsEnabled = false;
            ActivateOKButton();
        }

        private void chkUnregisterGLN_Unchecked(object sender, RoutedEventArgs e)
        {
            chkImmediatelyGLN.IsChecked = false;
            liImmediatelyGLN.Visibility = liDateGLN.Visibility = Visibility.Collapsed;
            chkReceiveOrderGLN.IsEnabled = true;
            chkReceiveStatementGLN.IsEnabled = true;
            ActivateOKButton();
        }

        private void chkUnregisterCVR_Checked(object sender, RoutedEventArgs e)
        {
            liImmediatelyCVR.Visibility = chkImmediatelyCVR.Visibility = hasOtherCompany ? Visibility.Collapsed : Visibility.Visible;
            chkImmediatelyCVR.IsChecked = true;
            chkReceiveOrderCVR.IsEnabled = false;
            chkReceiveStatementCVR.IsEnabled = false;
            ActivateOKButton();
        }

        private void chkUnregisterCVR_Unchecked(object sender, RoutedEventArgs e)
        {
            chkImmediatelyCVR.IsChecked = false;
            liImmediatelyCVR.Visibility = liDateCVR.Visibility = Visibility.Collapsed;
            chkReceiveOrderCVR.IsEnabled = true;
            chkReceiveStatementCVR.IsEnabled = true;
            ActivateOKButton();
        }

        private void ActivateOKButton_Reaction(object sender, RoutedEventArgs e) => ActivateOKButton();

        private void DateCVR_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (dtDateCVR.DateTime != DateCVR)
                ActivateOKButton();
        }

        private void DateGLN_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (dtDateGLN.DateTime != DateGLN)
                ActivateOKButton();
        }

    }
}
