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
    public class CartoonFilter : AbstractFilter
    {
        protected bool _distinctEdges = false;

        public CartoonFilter()
            : base()
        {
            Name = "Cartoon";
        }

        public override void DefineFilter(EditingSession session)
        {
            session.AddFilter(FilterFactory.CreateCartoonFilter(_distinctEdges));
        }

        public override bool AttachControl(FilterPropertiesControl control)
        {
            _control = control;
            Grid grid = new Grid();
            int rowIndex = 0;

            CheckBox distinctEdgesCheckBox = new CheckBox();
            TextBlock textBlock = new TextBlock();
            textBlock.Text = AppResources.DistinctEdges;
            distinctEdgesCheckBox.Content = textBlock;
            distinctEdgesCheckBox.IsChecked = _distinctEdges;
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
            _distinctEdges = true;
            CreatePreviewImage();
            _control.NotifyManipulated();
        }

        void distinctEdgesCheckBox_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            _distinctEdges = false;
            CreatePreviewImage();
            _control.NotifyManipulated();
        }
    }
}
