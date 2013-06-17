/**
 * Copyright (c) 2013 Nokia Corporation.
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

using Nokia.Graphics;
using Nokia.Graphics.Imaging;

using FilterEffects.Resources;

namespace FilterEffects
{
    public class SadHipsterFilter : CarShowFilter
    {
        public SadHipsterFilter()
            : base()
        {
            Name = "Sad Hipster";

            _brightness = 0.5;
            _saturation = 0.3;
            _lomoVignetting = LomoVignetting.Medium;
            _lomoStyle = LomoStyle.Yellow;
            _lomoVignettingGroup = "SadHipsterLomoVignetting";
        }

        public override void DefineFilter(EditingSession session)
        {
            session.AddFilter(FilterFactory.CreateAntiqueFilter());
            session.AddFilter(FilterFactory.CreateLomoFilter(
                _brightness, _saturation, _lomoVignetting, _lomoStyle));
        }
    }
}
