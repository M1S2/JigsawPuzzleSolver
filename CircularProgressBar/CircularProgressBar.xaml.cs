using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CircularProgressBarControl
{
    /// <summary>
    /// Interaktionslogik für CircularProgressBar.xaml
    /// </summary>
    /// see: https://github.com/aalitor/WPF_Circular-Progress-Bar/blob/master/CircularProgressBar/CircularProgressBar.cs
    public partial class CircularProgressBar : ProgressBar
    {
        /// <summary>
        /// Angle of the circular progress bar that is calculated using the Value property
        /// </summary>
        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }
        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register("Angle", typeof(double), typeof(CircularProgressBar), new PropertyMetadata(0.0));

        /// <summary>
        /// Radius of the circular progress bar
        /// </summary>
        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }
        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register("Radius", typeof(double), typeof(CircularProgressBar), new PropertyMetadata(50.0));


        /// <summary>
        /// Thickness of the permanent circle
        /// </summary>
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(CircularProgressBar), new PropertyMetadata(1.0));

        /// <summary>
        /// Brush of the permanent circle
        /// </summary>
        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(CircularProgressBar), new PropertyMetadata(Brushes.LightGray));
        
        /// <summary>
        /// Thickness of the circular progress bar
        /// </summary>
        public double Thickness
        {
            get { return (double)GetValue(ThicknessProperty); }
            set { SetValue(ThicknessProperty, value); }
        }
        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(CircularProgressBar), new PropertyMetadata(10d));

        /// <summary>
        /// Fill Brush of the circular progress bar inner part
        /// </summary>
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }
        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(CircularProgressBar), new PropertyMetadata(Brushes.Transparent));

        /// <summary>
        /// Show percentage value of progress bar
        /// </summary>
        public bool ShowValue
        {
            get { return (bool)GetValue(ShowValueProperty); }
            set { SetValue(ShowValueProperty, value); }
        }
        public static readonly DependencyProperty ShowValueProperty = DependencyProperty.Register(nameof(ShowValue), typeof(bool), typeof(CircularProgressBar), new PropertyMetadata(true));

        //##############################################################################################################################################################################################

        public CircularProgressBar()
        {
            InitializeComponent();
            this.ValueChanged += CircularProgressBar_ValueChanged;
            this.SizeChanged += CircularProgressBar_SizeChanged;
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Change width and height if radius property changes
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == CircularProgressBar.RadiusProperty)
            {
                Width = Height = Radius * 2;
            }
            base.OnPropertyChanged(e);
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Set the radius property using the minimum dimensions (width or height)
        /// </summary>
        private void CircularProgressBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Radius = Math.Min(ActualWidth, ActualHeight) / 2;
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Start the animation on value changes
        /// </summary>
        private void CircularProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CircularProgressBar bar = sender as CircularProgressBar;
            double currentAngle = bar.Angle;
            double targetAngle = e.NewValue / bar.Maximum * 359.999;
            double duration = Math.Abs(currentAngle - targetAngle) / 359.999 * 50; //100;
            DoubleAnimation anim = new DoubleAnimation(currentAngle, targetAngle, TimeSpan.FromMilliseconds(duration > 0 ? duration : 10));
            bar.BeginAnimation(CircularProgressBar.AngleProperty, anim, HandoffBehavior.SnapshotAndReplace);
        }

    }
}
