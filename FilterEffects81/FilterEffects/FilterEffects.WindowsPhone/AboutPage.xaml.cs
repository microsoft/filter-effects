/**
 * Copyright (c) 2013-2014 Microsoft Mobile.
 * See the license file delivered with this project for more information.
 */

using System;
using System.Xml.Linq;
using Windows.UI.Xaml.Controls;

namespace FilterEffects
{
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
            
            this.Loaded += (sender, args) =>
            {
                var ver = Windows.ApplicationModel.Package.Current.Id.Version;
                VersionNumber.Text = string.Format("{0}.{1}.{2}", ver.Major, ver.Minor, ver.Revision);
            };
        }
    }
}