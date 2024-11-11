using Uniconta.API.System;
using Uniconta.Common.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools;
using UnicontaClient.Pages;
using System.Windows;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWFixedProfiles : ChildWindow
    {
        CompanyAccessAPI api;
        public CompanyUserAccessClient userAccess;
        public FixedProfiles profile;
        [AppEnumAttribute(EnumName = "FixedProfiles")]
        [Display(Name = "FixedProfiles", ResourceType = typeof(CompanyAccessClientText))]
        public string Profile { get { return AppEnums.FixedProfiles.ToString((int)profile); } set { if (value == null) return; profile = (FixedProfiles)AppEnums.FixedProfiles.IndexOf(value); } }

        public CWFixedProfiles(CompanyAccessAPI api, CompanyUserAccessClient user)
        {
            this.DataContext = this;
            Init(api, user);
        }
        void Init(CompanyAccessAPI api, CompanyUserAccessClient user)
        {
            this.api = api;
            this.userAccess = user;
            if (userAccess == null)
                return;
            var Rights = userAccess._Rights;
            this.profile = AccessLevel.GetFixedProfile(Rights);
            InitializeComponent();
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("FixedProfiles");
#endif
#if SILVERLIGHT
            Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
                {
                    if (OKButton.IsFocused)
                        OKButton_Click(null, null);
                    else if (CancelButton.IsFocused)
                        SetDialogResult(false);
                }
        }
        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            userAccess._Rights = AccessLevel.SetFixedProfile(userAccess._Rights, profile);
            var err = await api.GiveCompanyAccess(userAccess);
            if (err != ErrorCodes.Succes)
            {
                UtilDisplay.ShowErrorCode(err);
            }
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }

    
}

