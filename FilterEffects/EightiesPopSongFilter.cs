/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using System;
using System.Collections.Generic;
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
    public class EightiesPopSongFilter : AbstractFilter
    {
        protected const String SketchModeGroup = "SketchModeGroup";
        protected SketchMode _sketchMode = SketchMode.Gray;

        public EightiesPopSongFilter()
            : base()
        {
            Name = "80's Pop Song";
        }

        public override void DefineFilter(EditingSession session)
        {
            session.AddFilter(FilterFactory.CreateSketchFilter(_sketchMode));
        }

        public override bool AttachControl(FilterPropertiesControl control)
        {
            _control = control;
            Grid grid = new Grid();
            int rowIndex = 0;

            TextBlock sketchModeText = new TextBlock();
            sketchModeText.Text = AppResources.SketchMode;
            Grid.SetRow(sketchModeText, rowIndex++);

            RadioButton grayRadioButton = new RadioButton();
            grayRadioButton.GroupName = SketchModeGroup;
            TextBlock textBlock = new TextBlock();
            textBlock.Text = AppResources.Gray;
            grayRadioButton.Content = textBlock;
            grayRadioButton.Checked += grayRadioButton_Checked;
            Grid.SetRow(grayRadioButton, rowIndex++);

            RadioButton colorRadioButton = new RadioButton();
            colorRadioButton.GroupName = SketchModeGroup;
            textBlock = new TextBlock();
            textBlock.Text = AppResources.Color;
            colorRadioButton.Content = textBlock;
            colorRadioButton.Checked += colorRadioButton_Checked;
            Grid.SetRow(colorRadioButton, rowIndex++);

            if (_sketchMode == SketchMode.Gray)
            {
                grayRadioButton.IsChecked = true;
            }
            else
            {
                colorRadioButton.IsChecked = true;
            }

            for (int i = 0; i < rowIndex; ++i)
            {
                RowDefinition rd = new RowDefinition();
                grid.RowDefinitions.Add(rd);
            }

            grid.Children.Add(sketchModeText);
            grid.Children.Add(grayRadioButton);
            grid.Children.Add(colorRadioButton);

            control.ControlsContainer.Children.Add(grid);

            return true;
        }

        void grayRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            _sketchMode = SketchMode.Gray;
            CreatePreviewImage();
            _control.NotifyManipulated();
        }

        void colorRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            _sketchMode = SketchMode.Color;
            CreatePreviewImage();
            _control.NotifyManipulated();
        }
    }
}
