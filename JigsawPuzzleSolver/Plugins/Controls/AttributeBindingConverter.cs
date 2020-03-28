using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace JigsawPuzzleSolver.Plugins.Controls
{
    /// <summary>
    /// Converter to get the value of an custom attribute property.
    /// </summary>
    class AttributeBindingConverter : IValueConverter
    {
        /// <summary>
        /// Converter to get the value of an custom attribute property.
        /// </summary>
        /// <param name="value">Class that is decorated with the attribute</param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Name of the attribute and name of the property to get the value from. Format "%AttributeName%.%PropertyName%" (e.g. "NameAttribute.Name")</param>
        /// <param name="culture"></param>
        /// <returns>Value of the custom attribute property</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) { return GetDefault(targetType); }
            object[] customAttributes = value.GetType().GetCustomAttributes(false);
            if (customAttributes != null && customAttributes.Length > 0)
            {
                string attributeAndPropertyName = parameter as string;
                string[] attributeAndPropertyNameSplitted = attributeAndPropertyName.Split('.');
                if (attributeAndPropertyNameSplitted.Length >= 2)
                {
                    string attributeName = attributeAndPropertyNameSplitted[0];
                    string propertyName = attributeAndPropertyNameSplitted[1];
                    Attribute firstMatchingAttribute = customAttributes.Select(a => a as Attribute).Where(a => ((Type)a.TypeId).Name == attributeName).FirstOrDefault();
                    if (firstMatchingAttribute != null)
                    {
                        PropertyInfo propertyInfo = firstMatchingAttribute.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            return propertyInfo.GetValue(firstMatchingAttribute);
                        }
                    }
                }
            }

            return GetDefault(targetType);
        }

        //see: https://stackoverflow.com/questions/325426/programmatic-equivalent-of-defaulttype
        public object GetDefault(Type t) => this.GetType().GetMethod("GetDefaultGeneric").MakeGenericMethod(t).Invoke(this, null);
        public T GetDefaultGeneric<T>() => default(T);

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
