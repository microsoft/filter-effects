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

        public override void DefineFilter(EditingSession session)
        {
            // No effect
        }

        public override bool AttachControl(FilterPropertiesControl control)
        {
            return false;
        }
    }
}
