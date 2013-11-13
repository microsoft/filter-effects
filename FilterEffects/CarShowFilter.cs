/**
 * Copyright (c) 2013 Nokia Corporation.
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

using Nokia.Graphics;
using Nokia.Graphics.Imaging;

using FilterEffects.Resources;

namespace FilterEffects
{
    public class CarShowFilter : AbstractFilter
    {
        // Constants
        private const double DefaultBrightness = 0.5;
        private const double DefaultSaturation = 0.5;
        private const LomoVignetting DefaultLomoVignetting = LomoVignetting.High;
        private const LomoStyle DefaultLomoStyle = LomoStyle.Neutral;

        // Members
        protected LomoFilter _lomoFilter;
        protected String _lomoVignettingGroup = "CarShowLomoVignetting";

        public CarShowFilter()
            : base()
        {
            Name = "Car Show";

            _lomoFilter = new LomoFilter();
            _lomoFilter.Brightness = DefaultBrightness;
            _lomoFilter.Saturation = DefaultSaturation;
            _lomoFilter.LomoVignetting = DefaultLomoVignetting;
            _lomoFilter.LomoStyle = DefaultLomoStyle;
        }

        protected override void SetFilters(FilterEffect effect)
        {
            effect.Filters = new List<IFilter>() { _lomoFilter };
        }

        public override bool AttachControl(FilterPropertiesControl control)
        {
            _control = control;
            Grid grid = new Grid();
            int rowIndex = 0;

            TextBlock brightnessText = new TextBlock();
            brightnessText.Text = AppResources.Brightness;
            Grid.SetRow(brightnessText, rowIndex++);

            Slider brightnessSlider = new Slider();
            brightnessSlider.Minimum = 0.0;
            brightnessSlider.Maximum = 1.0;
            brightnessSlider.Value = _lomoFilter.Brightness;
            brightnessSlider.ValueChanged += brightnessSlider_ValueChanged;
            Grid.SetRow(brightnessSlider, rowIndex++);

            TextBlock saturationText = new TextBlock();
            saturationText.Text = AppResources.Saturation;
            Grid.SetRow(saturationText, rowIndex++);

            Slider saturationSlider = new Slider();
            saturationSlider.Minimum = 0.0;
            saturationSlider.Maximum = 1.0;
            saturationSlider.Value = _lomoFilter.Saturation;
            saturationSlider.ValueChanged += saturationSlider_ValueChanged;
            Grid.SetRow(saturationSlider, rowIndex++);

            TextBlock lomoVignettingText = new TextBlock();
            lomoVignettingText.Text = AppResources.LomoVignetting;
            Grid.SetRow(lomoVignettingText, rowIndex++);

            RadioButton highRadioButton = new RadioButton();
            highRadioButton.GroupName = _lomoVignettingGroup;
            TextBlock textBlock = new TextBlock();
            textBlock.Text = AppResources.High;
            highRadioButton.Content = textBlock;
            highRadioButton.Checked += highRadioButton_Checked;
            Grid.SetRow(highRadioButton, rowIndex++);

            RadioButton medRadioButton = new RadioButton();
            medRadioButton.GroupName = _lomoVignettingGroup;
            textBlock = new TextBlock();
            textBlock.Text = AppResources.Medium;
            medRadioButton.Content = textBlock;
            medRadioButton.Checked += medRadioButton_Checked;
            Grid.SetRow(medRadioButton, rowIndex++);

            RadioButton lowRadioButton = new RadioButton();
            lowRadioButton.GroupName = _lomoVignettingGroup;
            textBlock = new TextBlock();
            textBlock.Text = AppResources.Low;
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
            _changes.Add(() => { _lomoFilter.Brightness = 1.0 - e.NewValue; });
            Apply();
            _control.NotifyManipulated();
        }

        protected void saturationSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            Debug.WriteLine("Changing saturation changed to " + e.NewValue);
            _changes.Add(() => { _lomoFilter.Saturation = e.NewValue; });
            Apply();
            _control.NotifyManipulated();
        }

        protected void lowRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            _changes.Add(() => { _lomoFilter.LomoVignetting = LomoVignetting.Low; });
            Apply();
            _control.NotifyManipulated();
        }

        protected void medRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            _changes.Add(() => { _lomoFilter.LomoVignetting = LomoVignetting.Medium; });
            Apply();
            _control.NotifyManipulated();
        }

        protected void highRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            _changes.Add(() => { _lomoFilter.LomoVignetting = LomoVignetting.High; });
            Apply();
            _control.NotifyManipulated();
        }
    }
}
