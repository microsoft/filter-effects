/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Xml.Linq;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

using FilterEffects.Resources;

namespace FilterEffects
{
    public partial class AboutPage : PhoneApplicationPage
    {
        public AboutPage()
        {
            InitializeComponent();
            VersionTextBox.Text = XDocument.Load("WMAppManifest.xml").Root.Element("App").Attribute("Version").Value;
        }
    }
}