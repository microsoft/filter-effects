/**
 * Copyright (c) 2013-2014 Nokia Corporation.
 * See the license file delivered with this project for more information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nokia.Graphics;
using Nokia.Graphics.Imaging;

namespace FilterEffects.Filters
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
    }
}
