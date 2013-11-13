/**
 * Copyright (c) 2013 Nokia Corporation.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nokia.Graphics;
using Nokia.Graphics.Imaging;

namespace FilterEffects
{
    public class OriginalImageFilter : AbstractFilter
    {
        public OriginalImageFilter()
            : base()
        {
            Name = "Original";
        }

        protected override void SetFilters(FilterEffect effect)
        {
            // No need to do anything
        }

        public override bool AttachControl(FilterPropertiesControl control)
        {
            return false;
        }
    }
}
