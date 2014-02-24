/**
 * Copyright (c) 2013-2014 Nokia Corporation.
 */

using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

using Nokia.Graphics.Imaging;

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
        protected LomoFilter Filter;
        protected string LomoVignettingGroup = "CarShowLomoVignetting";

        public SixthGearFilter()
        {
            Name = "Sixth Gear";
            ShortDescription = "Lomo";

            Filter = new LomoFilter
            {
                Brightness = DefaultBrightness,
                Saturation = DefaultSaturation,
                LomoVignetting = DefaultLomoVignetting,
                LomoStyle = DefaultLomoStyle
            };

            CreateControl();
        }

        protected override void SetFilters(FilterEffect effect)
        {
            effect.Filters = new List<IFilter> { Filter };
        }

        protected void CreateControl()
        {
            var grid = new Grid();
            int rowIndex = 0;
            int columnIndex;

            var brightnessText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = FilterControlTitleFontSize,
                Text = Strings.Brightness
            };

            Grid.SetRow(brightnessText, rowIndex++);

            var brightnessSlider = new Slider
            {
                StepFrequency = 0.01,
                Minimum = 0.0,
                Maximum = 1.0,
                Value = Filter.Brightness
            };
            brightnessSlider.ValueChanged += brightnessSlider_ValueChanged;

            Grid.SetRow(brightnessSlider, rowIndex++);

            var saturationText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = FilterControlTitleFontSize,
                Text = Strings.Saturation
            };

            Grid.SetRow(saturationText, rowIndex++);

            var saturationSlider = new Slider
            {
                StepFrequency = 0.01,
                Minimum = 0.0,
                Maximum = 1.0,
                Value = Filter.Saturation
            };
            saturationSlider.ValueChanged += saturationSlider_ValueChanged;

            Grid.SetRow(saturationSlider, rowIndex++);

            var margin = new Thickness { Left = 72 };
            rowIndex = 0;
            columnIndex = 1;

            var lomoVignettingText = new TextBlock
            {
                Margin = margin,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = FilterControlTitleFontSize,
                Text = Strings.LomoVignetting
            };

            Grid.SetRow(lomoVignettingText, rowIndex++);
            Grid.SetColumn(lomoVignettingText, columnIndex);

            var highRadioButton = new RadioButton
            {
                Margin = margin,
                GroupName = LomoVignettingGroup,
                Content = new TextBlock { Text = Strings.High }
            };
            highRadioButton.Checked += highRadioButton_Checked;

            Grid.SetRow(highRadioButton, rowIndex++);
            Grid.SetColumn(highRadioButton, columnIndex);

            var medRadioButton = new RadioButton
            {
                Margin = margin,
                GroupName = LomoVignettingGroup,
                Content = new TextBlock { Text = Strings.Medium }
            };
            medRadioButton.Checked += medRadioButton_Checked;

            Grid.SetRow(medRadioButton, rowIndex++);
            Grid.SetColumn(medRadioButton, columnIndex);

            var lowRadioButton = new RadioButton
            {
                Margin = margin,
                GroupName = LomoVignettingGroup,
                Content = new TextBlock { Text = Strings.Low }
            };
            lowRadioButton.Checked += lowRadioButton_Checked;

            Grid.SetRow(lowRadioButton, rowIndex++);
            Grid.SetColumn(lowRadioButton, columnIndex);

            switch (Filter.LomoVignetting)
            {
                case LomoVignetting.Low: lowRadioButton.IsChecked = true; break;
                case LomoVignetting.Medium: medRadioButton.IsChecked = true; break;
                case LomoVignetting.High: highRadioButton.IsChecked = true; break;
            }

            for (int i = 0; i < rowIndex; ++i)
            {
                var rowDefinition = new RowDefinition();

                if (i < rowIndex - 1)
                {
                    rowDefinition.MinHeight = GridRowMinimumHeight;
                }
                else
                {
                    rowDefinition.Height = GridLength.Auto;
                }

                grid.RowDefinitions.Add(rowDefinition);
            }

            grid.ColumnDefinitions.Add(new ColumnDefinition { MaxWidth = 500 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            grid.Children.Add(brightnessText);
            grid.Children.Add(brightnessSlider);
            grid.Children.Add(saturationText);
            grid.Children.Add(saturationSlider);
            grid.Children.Add(lomoVignettingText);
            grid.Children.Add(lowRadioButton);
            grid.Children.Add(medRadioButton);
            grid.Children.Add(highRadioButton);

            Control = grid;
        }

        protected void brightnessSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Debug.WriteLine("Changing brightness to " + (1.0 - e.NewValue));
            Changes.Add(() => { Filter.Brightness = 1.0 - e.NewValue; });
            Apply();
        }

        protected void saturationSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Debug.WriteLine("Changing saturation changed to " + e.NewValue);
            Changes.Add(() => { Filter.Saturation = e.NewValue; });
            Apply();
        }

        protected void lowRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            Changes.Add(() => { Filter.LomoVignetting = LomoVignetting.Low; });
            Apply();
        }

        protected void medRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            Changes.Add(() => { Filter.LomoVignetting = LomoVignetting.Medium; });
            Apply();
        }

        protected void highRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            Changes.Add(() => { Filter.LomoVignetting = LomoVignetting.High; });
            Apply();
        }
    }
}
