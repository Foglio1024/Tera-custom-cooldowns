﻿using FoglioUtils;
using System;
using System.Globalization;
using System.Windows.Data;

namespace TCC.Converters
{
    public class FactorToAngleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = System.Convert.ToDouble(value);
            double mult = 1;
            if (parameter != null && parameter.ToString() != "") mult = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);

            return MathUtils.FactorToAngle(val, mult);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    
}
