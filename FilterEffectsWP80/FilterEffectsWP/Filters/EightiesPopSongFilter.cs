/**
 * Copyright (c) 2013-2014 Microsoft Mobile.
 * See the license file delivered with this project for more information.
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

using FilterEffects.Filters.FilterControls;
using FilterEffects.Resources;

namespace FilterEffects.Filters
{
    public class EightiesPopSongFilter : AbstractFilter
    {
        private const String SketchModeGroup = "SketchModeGroup";
        private const SketchMode DefaultSketchMode = SketchMode.Gray;
        private SketchFilter _sketchFilter;

        public EightiesPopSongFilter()
            : base()
        {
            Name = "80's Pop Song";
            ShortDescription = "Sketch";

            _sketchFilter = new SketchFilter {SketchMode = DefaultSketchMode};
        }

        protected override void SetFilters(FilterEffect effect)
        {
            effect.Filters = new List<IFilter>() { _sketchFilter };
        }

        public override bool AttachControl(FilterPropertiesControl control)
        {
            Control = control;

            Grid grid = new Grid();
            int rowIndex = 0;

            TextBlock sketchModeText = new TextBlock {Text = AppResources.SketchMode};
            Grid.SetRow(sketchModeText, rowIndex++);

            RadioButton grayRadioButton = new RadioButton {GroupName = SketchModeGroup};
            TextBlock textBlock = new TextBlock {Text = AppResources.Gray};
            grayRadioButton.Content = textBlock;
            grayRadioButton.Checked += grayRadioButton_Checked;
            Grid.SetRow(grayRadioButton, rowIndex++);

            RadioButton colorRadioButton = new RadioButton {GroupName = SketchModeGroup};
            textBlock = new TextBlock {Text = AppResources.Color};
            colorRadioButton.Content = textBlock;
            colorRadioButton.Checked += colorRadioButton_Checked;
            Grid.SetRow(colorRadioButton, rowIndex++);

            if (_sketchFilter.SketchMode == SketchMode.Gray)
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
            Changes.Add(() => { _sketchFilter.SketchMode = SketchMode.Gray; });
            Apply();
            Control.NotifyManipulated();
        }

        void colorRadioButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            Changes.Add(() => { _sketchFilter.SketchMode = SketchMode.Color; });
            Apply();
            Control.NotifyManipulated();
        }
    }
}
