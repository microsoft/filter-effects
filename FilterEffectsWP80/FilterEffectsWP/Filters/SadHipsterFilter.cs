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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


using Lumia.Imaging;
using Lumia.Imaging.Artistic;
using FilterEffects.Resources;

namespace FilterEffects.Filters
{
    public class SadHipsterFilter : SixthGearFilter
    {
        // Constants
        private const double DefaultBrightness = 0.5;
        private const double DefaultSaturation = 0.3;
        private const LomoVignetting DefaultLomoVignetting = LomoVignetting.Medium;
        private const LomoStyle DefaultLomoStyle = LomoStyle.Yellow;

        public SadHipsterFilter()
            : base()
        {
            Name = "Sad Hipster";
            ShortDescription = "Antique & Lomo";

            _lomoFilter.Brightness = DefaultBrightness;
            _lomoFilter.Saturation = DefaultSaturation;
            _lomoFilter.LomoVignetting = DefaultLomoVignetting;
            _lomoFilter.LomoStyle = DefaultLomoStyle;

            _lomoVignettingGroup = "SadHipsterLomoVignetting";
        }

        protected override void SetFilters(FilterEffect effect)
        {
            AntiqueFilter antiqueFilter = new AntiqueFilter();
            effect.Filters = new List<IFilter>() { antiqueFilter, _lomoFilter };
        }
    }
}
