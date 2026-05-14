using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using VinhKhanhAudioGuide.Mobile.Services;

namespace VinhKhanhAudioGuide.Mobile.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Application.Current?.Resources["Primary"] ?? Colors.Purple;
        }

        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToFavoriteColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isFav && isFav)
            return Colors.Red;

        return Application.Current?.Resources["PrimaryDark"] ?? Colors.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToHeartGlyphConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is bool b && b) ? "♥" : "♡";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Colors.White;
        }
        return Application.Current?.Resources["Primary"] ?? Colors.Purple;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !string.IsNullOrWhiteSpace(value?.ToString());
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class MinutesToHoursConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int minutes)
        {
            if (minutes < 60)
                return $"{minutes} phút";

            var hours = minutes / 60;
            var mins = minutes % 60;
            return mins > 0 ? $"{hours}h {mins}p" : $"{hours} giờ";
        }
        return "0 phút";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToRotationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 90d : 0d;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}

/// <summary>Chip background color: Primary if selected, SurfaceContainerHigh if not</summary>
public class BoolToChipBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Application.Current?.Resources["Primary"] as Color ?? Colors.Gray;
        }
        return Application.Current?.Resources["SurfaceContainerHigh"] as Color ?? Colors.LightGray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>Chip text color: OnPrimary (white) if selected, OnSurfaceVariant (dark gray) if not</summary>
public class BoolToChipTextColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Application.Current?.Resources["OnPrimary"] as Color ?? Colors.White;
        }
        return Application.Current?.Resources["OnSurfaceVariant"] as Color ?? Colors.DarkGray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DurationMinutesConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int minutes)
        {
            return "";
        }

        var localization = ResolveLocalizationService();
        if (localization is null)
        {
            return minutes <= 0 ? "0 min" : $"{minutes} min";
        }

        var template = localization.GetString("Common_MinutesFormat");
        return string.Format(template, minutes);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static ILocalizationService? ResolveLocalizationService()
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService(typeof(ILocalizationService)) as ILocalizationService;
    }
}

public class AudioCountConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int count)
        {
            return "";
        }

        var localization = Application.Current?.Handler?.MauiContext?.Services.GetService(typeof(ILocalizationService)) as ILocalizationService;
        if (localization is null)
        {
            return $"{count} audio";
        }

        var template = localization.GetString("Common_AudioCountFormat");
        return string.Format(template, count);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RatingToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int rating && parameter is string starIndexStr && int.TryParse(starIndexStr, out int starIndex))
        {
            if (rating >= starIndex)
            {
                return Color.FromArgb("#FFD700"); // Gold
            }
        }
        return Application.Current?.Resources["Gray300"] as Color ?? Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RatingToStarsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int rating)
        {
            return new string('★', rating).PadRight(5, '☆');
        }
        return "☆☆☆☆☆";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CollectionEmptyToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0;
        }
        if (value is System.Collections.ICollection collection)
        {
            return collection.Count == 0;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
