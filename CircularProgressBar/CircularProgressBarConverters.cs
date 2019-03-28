using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Converters;
using System.Windows.Data;
using System.Windows.Media;

namespace CircularProgressBarControl
{
    //see: https://github.com/aalitor/WPF_Circular-Progress-Bar/blob/master/CircularProgressBar/Converters.cs

    public class AngleToPointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double angle = (double)values[0];
            double radius = (double)values[1];
            double stroke = (double)values[2];
            double piang = angle * Math.PI / 180;

            double px = Math.Sin(piang) * (radius - stroke / 2) + radius;
            double py = -Math.Cos(piang) * (radius - stroke / 2) + radius;
            return new Point(px, py);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AngleToIsLargeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double angle = (double)value;

            return angle > 180;
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class StrokeToStartPointConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double radius = (double)values[0];
            double stroke = (double)values[1];
            return new Point(radius, stroke / 2);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class RadiusToSizeConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double radius = (double)values[0];
            double stroke = (double)values[1];
            if(radius < stroke) { return Size.Empty; }
            return new Size(radius - stroke / 2, radius - stroke / 2);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class RadiusToCenterConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double radius = (double)value;
            return new Point(radius, radius);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class RadiusToDiameter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double radius = (double)value;
            return 2 * radius;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class InnerRadiusConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double radius = (double)values[0];
            double stroke = (double)values[1];
            return radius - stroke / 2;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
