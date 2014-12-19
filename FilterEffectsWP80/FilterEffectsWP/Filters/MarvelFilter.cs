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
    public class MarvelFilter : AbstractFilter
    {
        private const bool DefaultDistinctEdges = false;
        protected CartoonFilter _cartoonFilter;

        public MarvelFilter()
            : base()
        {
            Name = "Marvel";
            ShortDescription = "Cartoon";

            _cartoonFilter = new CartoonFilter {DistinctEdges = DefaultDistinctEdges};
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
            TextBlock textBlock = new TextBlock {Text = AppResources.DistinctEdges};
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
