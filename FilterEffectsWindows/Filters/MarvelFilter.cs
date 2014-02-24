/**
 * Copyright (c) 2013-2014 Nokia Corporation.
 */

using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Nokia.Graphics.Imaging;

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
                Text = Strings.DistinctEdges
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
        }

        void distinctEdgesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Changes.Add(() => { Filter.DistinctEdges = false; });
            Apply();
        }
    }
}
