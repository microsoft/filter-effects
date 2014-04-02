/**
 * Copyright (c) 2013-2014 Nokia Corporation.
 */


using Nokia.Graphics.Imaging;

namespace FilterEffects.Filters
{
    public class OriginalImageFilter : AbstractFilter
    {
        public OriginalImageFilter()
        {
            Name = "Original";
            ShortDescription = "No filters";
        }

        protected override void SetFilters(FilterEffect effect)
        {
            // No need to do anything
        }
    }
}
