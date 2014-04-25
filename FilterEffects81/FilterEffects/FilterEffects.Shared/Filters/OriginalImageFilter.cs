/**
 * Copyright (c) 2013-2014 Microsoft Mobile.
 * See the license file delivered with this project for more information.
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
