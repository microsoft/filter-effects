/*
 * Copyright (c) 2014 Microsoft Mobile
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Windows;
using System.Windows.Controls;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace FilterEffects.Filters.FilterControls
{
    public sealed partial class HdrControl : UserControl
    {
        public EventHandler<EventArgs> ValueChanged;

        public HdrControl()
        {
            InitializeComponent();
        }

        #region Strength

        /// <summary>
        /// Strength Property name
        /// </summary>
        public const string StrengthPropertyName = "Strength";

        public double Strength
        {
            get
            {
                return (double)GetValue(StrengthProperty);
            }
            set
            {
                SetValue(StrengthProperty, value);
            }
        }

        /// <summary>
        /// Strength Property definition
        /// </summary>
        public static readonly DependencyProperty StrengthProperty =  DependencyProperty.Register(
            StrengthPropertyName,
            typeof(double),
            typeof(HdrControl),
            new PropertyMetadata(default(double), MyPropertyChanged));

        #endregion

        #region NoiseSuppression

        /// <summary>
        /// NoiseSuppression Property name
        /// </summary>
        public const string NoisePropertyName = "NoiseSuppression";

        public double NoiseSuppression
        {
            get
            {
                return (double)GetValue(NoiseSuppressionProperty);
            }
            set
            {
                SetValue(NoiseSuppressionProperty, value);
            }
        }

        /// <summary>
        /// NoiseSuppression Property definition
        /// </summary>
        public static readonly DependencyProperty NoiseSuppressionProperty = DependencyProperty.Register(
            NoisePropertyName,
            typeof(double),
            typeof(HdrControl),
            new PropertyMetadata(default(double), MyPropertyChanged));

        #endregion

        #region Saturation

        /// <summary>
        /// Saturation Property name
        /// </summary>
        public const string SaturationPropertyName = "Saturation";

        public double Saturation
        {
            get
            {
                return (double)GetValue(SaturationProperty);
            }
            set {
                SetValue(SaturationProperty, value);
            }
        }

        /// <summary>
        /// Saturation Property definition
        /// </summary>
        public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(
            SaturationPropertyName,
            typeof(double),
            typeof(HdrControl),
            new PropertyMetadata(default(double), MyPropertyChanged));

        #endregion

        public static void MyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as HdrControl;
            
            if (control != null && control.ValueChanged != null)
            {
                control.ValueChanged(control, new EventArgs());
            }
        }
    }
}
