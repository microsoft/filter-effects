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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Lumia.Imaging;
using Lumia.Imaging.Artistic;

namespace FilterEffects.Filters
{
    public class MarvelFilter : AbstractFilter
    {
        private const bool DefaultDistinctEdges = false;
        protected CartoonFilter Filter;

        public MarvelFilter()
        {
            Name = "Marvel";
            ShortDescription = "Cartoon";

            Filter = new CartoonFilter {DistinctEdges = DefaultDistinctEdges};

            CreateControl();
        }

        protected override void SetFilters(FilterEffect effect)
        {
            effect.Filters = new List<IFilter> { Filter };
        }

        private void CreateControl()
        {
            var grid = new Grid();

            var margin = new Thickness {Top = 24};

            grid.Margin = margin;

            var distinctEdgesCheckBox = new CheckBox {Margin = margin, VerticalAlignment = VerticalAlignment.Center};

            var padding = new Thickness {Left = 12, Right = 12};
            distinctEdgesCheckBox.Padding = padding;

            var textBlock = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = FilterControlTitleFontSize,
                Text = _resourceLoader.GetString("DistinctEdges/Text")
            };

            distinctEdgesCheckBox.Content = textBlock;
            distinctEdgesCheckBox.IsChecked = Filter.DistinctEdges;
            distinctEdgesCheckBox.Checked += distinctEdgesCheckBox_Checked;
            distinctEdgesCheckBox.Unchecked += distinctEdgesCheckBox_Unchecked;

            var rowDefinition = new RowDefinition {Height = GridLength.Auto};
            grid.RowDefinitions.Add(rowDefinition);

            var columnDefinition = new ColumnDefinition {Width = GridLength.Auto};
            grid.ColumnDefinitions.Add(columnDefinition);

            grid.Children.Add(distinctEdgesCheckBox);

            Control = grid;
        }

        void distinctEdgesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Changes.Add(() => { Filter.DistinctEdges = true; });
            Apply();
            NotifyManipulated();
        }

        void distinctEdgesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Changes.Add(() => { Filter.DistinctEdges = false; });
            Apply();
            NotifyManipulated();
        }
    }
}
