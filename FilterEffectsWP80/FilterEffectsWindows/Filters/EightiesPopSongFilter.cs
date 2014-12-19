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

using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

using Lumia.Imaging;

namespace FilterEffects.Filters
{
    public class EightiesPopSongFilter : AbstractFilter
    {
        private const string SketchModeGroup = "SketchModeGroup";
        private const SketchMode DefaultSketchMode = SketchMode.Gray;
        private readonly SketchFilter _sketchFilter;

        public EightiesPopSongFilter()
        {
            Name = "80's Pop Song";
            ShortDescription = "Sketch";

            _sketchFilter = new SketchFilter {SketchMode = DefaultSketchMode};

            CreateControl();
        }

        protected override void SetFilters(FilterEffect effect)
        {
            effect.Filters = new List<IFilter> { _sketchFilter };
        }

        private void CreateControl()
        {
            var grid = new Grid();
            int rowIndex = 0;

            var margin = new Thickness {Top = 24};

            grid.Margin = margin;

            var sketchModeText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = FilterControlTitleFontSize,
                Text = Strings.SketchMode
            };

            Grid.SetRow(sketchModeText, rowIndex++);

            var padding = new Thickness {Left = 12, Right = 12};

            var grayRadioButton = new RadioButton {Padding = padding, GroupName = SketchModeGroup};
            
            var textBlock = new TextBlock {Text = Strings.Gray};

            grayRadioButton.Content = textBlock;
            grayRadioButton.Checked += grayRadioButton_Checked;
            Grid.SetRow(grayRadioButton, rowIndex++);

            var colorRadioButton = new RadioButton {Padding = padding, GroupName = SketchModeGroup};
            
            textBlock = new TextBlock {Text = Strings.Color};

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
                var rowDefinition = new RowDefinition();

                if (i == 0)
                {
                    rowDefinition.MinHeight = FilterControlTitleFontSize;
                    rowDefinition.MaxHeight = FilterControlTitleFontSize;
                }
                else if (i < rowIndex - 1)
                {
                    rowDefinition.MinHeight = GridRowMinimumHeight;
                    rowDefinition.MaxHeight = GridRowMaxHeight;
                }
                else
                {
                    rowDefinition.Height = GridLength.Auto;
                }

                grid.RowDefinitions.Add(rowDefinition);
            }

            grid.Children.Add(sketchModeText);
            grid.Children.Add(grayRadioButton);
            grid.Children.Add(colorRadioButton);

            Control = grid;
        }

        void grayRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            Changes.Add(() => { _sketchFilter.SketchMode = SketchMode.Gray; });
            Apply();
        }

        void colorRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            Changes.Add(() => { _sketchFilter.SketchMode = SketchMode.Color; });
            Apply();
        }
    }
}
