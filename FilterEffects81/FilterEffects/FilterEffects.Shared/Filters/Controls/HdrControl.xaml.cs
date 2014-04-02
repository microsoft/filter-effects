using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace FilterEffects.Filters.Controls
{
    public sealed partial class HdrControl : UserControl
    {
        public EventHandler<EventArgs> ValueChanged;

        public HdrControl()
        {
            InitializeComponent();
        }

        #region Strength

        /// <summary>
        /// Strength Property name
        /// </summary>
        public const string StrengthPropertyName = "Strength";

        public double Strength
        {
            get
            {
                return (double)GetValue(StrengthProperty);
            }
            set
            {
                SetValue(StrengthProperty, value);
            }
        }

        /// <summary>
        /// Strength Property definition
        /// </summary>
        public static readonly DependencyProperty StrengthProperty =  DependencyProperty.Register(
            StrengthPropertyName,
            typeof(double),
            typeof(HdrControl),
            new PropertyMetadata(default(double), MyPropertyChanged));

        #endregion

        #region NoiseSuppression

        /// <summary>
        /// NoiseSuppression Property name
        /// </summary>
        public const string NoisePropertyName = "NoiseSuppression";

        public double NoiseSuppression
        {
            get
            {
                return (double)GetValue(NoiseSuppressionProperty);
            }
            set
            {
                SetValue(NoiseSuppressionProperty, value);
            }
        }

        /// <summary>
        /// NoiseSuppression Property definition
        /// </summary>
        public static readonly DependencyProperty NoiseSuppressionProperty = DependencyProperty.Register(
            NoisePropertyName,
            typeof(double),
            typeof(HdrControl),
            new PropertyMetadata(default(double), MyPropertyChanged));

        #endregion

        #region Saturation

        /// <summary>
        /// Saturation Property name
        /// </summary>
        public const string SaturationPropertyName = "Saturation";

        public double Saturation
        {
            get
            {
                return (double)GetValue(SaturationProperty);
            }
            set {
                SetValue(SaturationProperty, value);
            }
        }

        /// <summary>
        /// Saturation Property definition
        /// </summary>
        public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(
            SaturationPropertyName,
            typeof(double),
            typeof(HdrControl),
            new PropertyMetadata(default(double), MyPropertyChanged));

        #endregion

        public static void MyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as HdrControl;
            
            if (control != null && control.ValueChanged != null)
            {
                control.ValueChanged(control, new EventArgs());
            }
        }
    }
}
