/**
 * Copyright (c) 2013-2014 Nokia Corporation.
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
    public class MarvelFilter : AbstractFilter
    {
        private const bool DefaultDistinctEdges = false;
        protected Nokia.Graphics.Imaging.CartoonFilter _cartoonFilter;

        public MarvelFilter()
            : base()
        {
            Name = "Marvel";
            ShortDescription = "Cartoon";

            _cartoonFilter = new Nokia.Graphics.Imaging.CartoonFilter();
            _cartoonFilter.DistinctEdges = DefaultDistinctEdges;
        }

        protected override void SetFilters(FilterEffect effect)
        {
            effect.Filters = new List<IFilter>() { _cartoonFilter };
        }

        public override bool AttachControl(FilterPropertiesControl control)
        {
            Control = control;

            Grid grid = new Grid();
            int rowIndex = 0;

            CheckBox distinctEdgesCheckBox = new CheckBox();
            TextBlock textBlock = new TextBlock();
            textBlock.Text = AppResources.DistinctEdges;
            distinctEdgesCheckBox.Content = textBlock;
            distinctEdgesCheckBox.IsChecked = _cartoonFilter.DistinctEdges;
            distinctEdgesCheckBox.Checked += distinctEdgesCheckBox_Checked;
            distinctEdgesCheckBox.Unchecked += distinctEdgesCheckBox_Unchecked;
            Grid.SetRow(distinctEdgesCheckBox, rowIndex++);

            for (int i = 0; i < rowIndex; ++i)
            {
                RowDefinition rd = new RowDefinition();
                grid.RowDefinitions.Add(rd);
            }

            grid.Children.Add(distinctEdgesCheckBox);

            control.ControlsContainer.Children.Add(grid);

            return true;
        }

        void distinctEdgesCheckBox_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            Changes.Add(() => { _cartoonFilter.DistinctEdges = true; });
            Apply();
            Control.NotifyManipulated();
        }

        void distinctEdgesCheckBox_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            Changes.Add(() => { _cartoonFilter.DistinctEdges = false; });
            Apply();
            Control.NotifyManipulated();
        }
    }
}
