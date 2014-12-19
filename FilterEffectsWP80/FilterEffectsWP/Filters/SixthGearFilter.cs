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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Lumia.Imaging;
using Lumia.Imaging.Artistic;

using FilterEffects.Filters.FilterControls;
using FilterEffects.Resources;

namespace FilterEffects.Filters
{
    public class SixthGearFilter : AbstractFilter
    {
        // Constants
        private const double DefaultBrightness = 0.5;
        private const double DefaultSaturation = 0.5;
        private const LomoVignetting DefaultLomoVignetting = LomoVignetting.High;
        private const LomoStyle DefaultLomoStyle = LomoStyle.Neutral;

        // Members
        protected LomoFilter _lomoFilter;
        protected String _lomoVignettingGroup = "CarShowLomoVignetting";

        public SixthGearFilter()
            : base()
        {
            Name = "Sixth Gear";
            ShortDescription = "Lomo";

            _lomoFilter = new LomoFilter
            {
                Brightness = DefaultBrightness,
                Saturation = DefaultSaturation,
                LomoVignetting = DefaultLomoVignetting,
                LomoStyle = DefaultLomoStyle
            };
        }

        protected override void SetFilters(FilterEffect effect)
        {
            effect.Filters = new List<IFilter>() { _lomoFilter };
        }

        public override bool AttachControl(FilterPropertiesControl control)
        {
            Control = control;

            Grid grid = new Grid();
            int rowIndex = 0;

            TextBlock brightnessText = new TextBlock {Text = AppResources.Brightness};
            Grid.SetRow(brightnessText, rowIndex++);

            Slider brightnessSlider = new Slider {Minimum = 0.0, Maximum = 1.0, Value = _lomoFilter.Brightness};
            brightnessSlider.ValueChanged += brightnessSlider_ValueChanged;
            Grid.SetRow(brightnessSlider, rowIndex++);

            TextBlock saturationText = new TextBlock {Text = AppResources.Saturation};
            Grid.SetRow(saturationText, rowIndex++);

            Slider saturationSlider = new Slider {Minimum = 0.0, Maximum = 1.0, Value = _lomoFilter.Saturation};
            saturationSlider.ValueChanged += saturationSlider_ValueChanged;
            Grid.SetRow(saturationSlider, rowIndex++);

            TextBlock lomoVignettingText = new TextBlock {Text = AppResources.LomoVignetting};
            Grid.SetRow(lomoVignettingText, rowIndex++);

            RadioButton highRadioButton = new RadioButton {GroupName = _lomoVignettingGroup};
            TextBlock textBlock = new TextBlock {Text = AppResources.High};
            highRadioButton.Content = textBlock;
            highRadioButton.Checked += highRadioButton_Checked;
            Grid.SetRow(highRadioButton, rowIndex++);

            RadioButton medRadioButton = new RadioButton {GroupName = _lomoVignettingGroup};
            textBlock = new TextBlock {Text = AppResources.Medium};
            medRadioButton.Content = textBlock;
            medRadioButton.Checked += medRadioButton_Checked;
            Grid.SetRow(medRadioButton, rowIndex++);

            RadioButton lowRadioButton = new RadioButton {GroupName = _lomoVignettingGroup};
            textBlock = new TextBlock {Text = AppResources.Low};
            lowRadioButton.Content = textBlock;
            lowRadioButton.Checked += lowRadioButton_Checked;
            Grid.SetRow(lowRadioButton, rowIndex++);

            switch (_lomoFilter.LomoVignetting)
            {
                case LomoVignetting.Low: lowRadioButton.IsChecked = true; break;
                case LomoVignetting.Medium: medRadioButton.IsChecked = true; break;
                case LomoVignetting.High: highRadioButton.IsChecked = true; break;
            }

            for (int i = 0; i < rowIndex; ++i)
            {
                RowDefinition rd = new RowDefinition();
                grid.RowDefinitions.Add(rd);
            }

            grid.Children.Add(brightnessText);
            grid.Children.Add(brightnessSlider);
            grid.Children.Add(saturationText);
            grid.Children.Add(saturationSlider);
            grid.Children.Add(lomoVignettingText);
            grid.Children.Add(lowRadioButton);
            grid.Children.Add(medRadioButton);
            grid.Children.Add(highRadioButton);

            control.ControlsContainer.Children.Add(grid);
            
            return true;
        }

        protected void brightnessSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            Debug.WriteLine("Changing brightness to " + (1.0 - e.NewValue));
            Changes.Add(() => { _lomoFilter.Brightness = 1.0 - e.NewValue; });
            Apply();
            Control.NotifyManipulated();
        }

        protected void saturationSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            Debug.WriteLine("Changing saturation changed to " + e.NewValue);
            Changes.Add(() => { _lomoFilter.Saturation = e.NewValue; });
            Apply();
            Control.NotifyManipulated();
        }

        protected void lowRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            Changes.Add(() => { _lomoFilter.LomoVignetting = LomoVignetting.Low; });
            Apply();
            Control.NotifyManipulated();
        }

        protected void medRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            Changes.Add(() => { _lomoFilter.LomoVignetting = LomoVignetting.Medium; });
            Apply();
            Control.NotifyManipulated();
        }

        protected void highRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            Changes.Add(() => { _lomoFilter.LomoVignetting = LomoVignetting.High; });
            Apply();
            Control.NotifyManipulated();
        }
    }
}
