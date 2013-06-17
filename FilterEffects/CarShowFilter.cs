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
        // Members
        protected double _brightness = 0.5;
        protected double _saturation = 0.5;
        protected LomoVignetting _lomoVignetting = LomoVignetting.Medium;
        protected LomoStyle _lomoStyle = LomoStyle.Neutral;
        protected String _lomoVignettingGroup = "CarShowLomoVignetting";

        public CarShowFilter()
            : base()
        {
            Name = "Car Show";

            _brightness = 0.5;
            _saturation = 0.8;
            _lomoVignetting = LomoVignetting.High;
            _lomoStyle = LomoStyle.Neutral;
        }

        public override void DefineFilter(EditingSession session)
        {
            session.AddFilter(FilterFactory.CreateLomoFilter(
                _brightness, _saturation, _lomoVignetting, _lomoStyle));
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
            brightnessSlider.Value = _brightness;
            brightnessSlider.ValueChanged += brightnessSlider_ValueChanged;
            Grid.SetRow(brightnessSlider, rowIndex++);

            TextBlock saturationText = new TextBlock();
            saturationText.Text = AppResources.Saturation;
            Grid.SetRow(saturationText, rowIndex++);

            Slider saturationSlider = new Slider();
            saturationSlider.Minimum = 0.0;
            saturationSlider.Maximum = 1.0;
            saturationSlider.Value = _saturation;
            saturationSlider.ValueChanged += saturationSlider_ValueChanged;
            Grid.SetRow(saturationSlider, rowIndex++);

            TextBlock lomoVignettingText = new TextBlock();
            lomoVignettingText.Text = AppResources.LomoVignetting;
            Grid.SetRow(lomoVignettingText, rowIndex++);

            RadioButton lowRadioButton = new RadioButton();
            lowRadioButton.GroupName = _lomoVignettingGroup;
            TextBlock textBlock = new TextBlock();
            textBlock.Text = AppResources.Low;
            lowRadioButton.Content = textBlock;
            lowRadioButton.Checked += lowRadioButton_Checked;
            Grid.SetRow(lowRadioButton, rowIndex++);

            RadioButton medRadioButton = new RadioButton();
            medRadioButton.GroupName = _lomoVignettingGroup;
            textBlock = new TextBlock();
            textBlock.Text = AppResources.Medium;
            medRadioButton.Content = textBlock;
            medRadioButton.Checked += medRadioButton_Checked;
            Grid.SetRow(medRadioButton, rowIndex++);

            RadioButton highRadioButton = new RadioButton();
            highRadioButton.GroupName = _lomoVignettingGroup;
            textBlock = new TextBlock();
            textBlock.Text = AppResources.High;
            highRadioButton.Content = textBlock;
            highRadioButton.Checked += highRadioButton_Checked;
            Grid.SetRow(highRadioButton, rowIndex++);

            switch (_lomoVignetting)
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
            _brightness = 1.0 - e.NewValue;
            Debug.WriteLine("Brightness changed to " + _brightness);
            CreatePreviewImage();
            _control.NotifyManipulated();
        }

        protected void saturationSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            _saturation = e.NewValue;
            CreatePreviewImage();
            _control.NotifyManipulated();
        }

        protected void lowRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            _lomoVignetting = LomoVignetting.Low;
            CreatePreviewImage();
            _control.NotifyManipulated();
        }

        protected void medRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            _lomoVignetting = LomoVignetting.Medium;
            CreatePreviewImage();
            _control.NotifyManipulated();
        }

        protected void highRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            _lomoVignetting = LomoVignetting.High;
            CreatePreviewImage();
            _control.NotifyManipulated();
        }
    }
}
