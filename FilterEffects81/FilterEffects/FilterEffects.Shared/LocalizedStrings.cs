/**
 * Copyright (c) 2014 Nokia Corporation.
 * See the license file delivered with this project for more information.
 */

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

namespace FilterEffects
{
    public class LocalizedStrings
    {
        private static ResourceLoader _resourceLoader;
        private static ResourceContext _resourceContext;
        private static ResourceMap _resourceMap;

        public static string GetString(string id, string type)
        {
            string fullId = id + "/" + type;
            System.Diagnostics.Debug.WriteLine("LocalizedStrings.GetText(): " + fullId);

            if (_resourceLoader == null)
            {
                _resourceLoader = new ResourceLoader();
                _resourceContext = new ResourceContext {Languages = new string[] {"en-US"}};
                _resourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("Resources");
            }

            ResourceCandidate candidate = _resourceMap.GetValue(fullId, _resourceContext);
            string text = null;

            if (candidate != null)
            {
                text = candidate.ValueAsString;
            }
            else
            {
                text = "<" + id + ">";
            }

            return text;
        }

        public static string GetText(string id)
        {
            return GetString(id, "Text");
        }
    }
}
