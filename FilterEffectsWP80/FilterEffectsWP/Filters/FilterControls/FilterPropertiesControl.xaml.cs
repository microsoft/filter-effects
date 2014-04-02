/**
 * Copyright (c) 2013-2014 Nokia Corporation.
 * See the license file delivered with this project for more information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace FilterEffects.Filters.FilterControls
{
    public partial class FilterPropertiesControl : UserControl
    {
        public event EventHandler Manipulated;

        public FilterPropertiesControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Notifies possible listeners that this control was manipulated.
        /// Note that it is up to the users of this class to implement the
        /// logic for calling this method.
        /// </summary>
        public void NotifyManipulated()
        {
            EventHandler handler = Manipulated;

            if (handler != null)
            {
                handler(this, null);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("FilterPropertiesControl.NotifyManipulated(): No handler for Manipulated event!");
            }
        }
    }
}
