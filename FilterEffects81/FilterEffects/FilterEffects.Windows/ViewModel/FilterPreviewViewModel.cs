/**
 * Copyright (c) 2014 Nokia Corporation.
 */

using System.Collections.ObjectModel;
using System.ComponentModel;

using FilterEffects.Filters;

namespace FilterEffects.ViewModel
{
    public class FilterPreviewViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<AbstractFilter> FilterPreviewItems
        {
            get;
            private set;
        }

        public FilterPreviewViewModel()
        {
            FilterPreviewItems = new ObservableCollection<AbstractFilter>();
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Add(AbstractFilter filter)
        {
            FilterPreviewItems.Add(filter);
        }
    }
}
