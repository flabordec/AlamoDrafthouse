using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace com.magusoft.drafthouse.Converters
{
	public class BooleanConverter<T> : IValueConverter
	{
		public BooleanConverter(T trueValue, T falseValue)
		{
			mTrue = trueValue;
			mFalse = falseValue;
		}

		public readonly T mTrue;
		public readonly T mFalse;

		public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is bool && ((bool)value) ? mTrue : mFalse;
		}

		public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is T && EqualityComparer<T>.Default.Equals((T)value, mTrue);
		}
	}

	[ValueConversion(typeof(bool), typeof(Visibility))]
	public sealed class InverseBooleanToVisibilityConverter : BooleanConverter<Visibility>
	{
		public InverseBooleanToVisibilityConverter() : 
			base(Visibility.Collapsed, Visibility.Visible) { }
	}

}
