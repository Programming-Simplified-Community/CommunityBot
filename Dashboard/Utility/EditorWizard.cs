using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Utility;

public record PropInfo(string DisplayName, PropertyInfo Property, bool IsHidden=false, bool IsRequired=false);

public enum InputType
{
    Text, Date, Number, Selection
}

public static class EditorWizard
{
    private static Dictionary<Type, Dictionary<string, PropInfo>> _propertyCache = new();

    public static InputType GetInputTypeForProperty(Type propertyType)
    {
        if (propertyType == typeof(int) || propertyType == typeof(int?) ||
            propertyType == typeof(decimal) || propertyType == typeof(decimal?) ||
            propertyType == typeof(float) || propertyType == typeof(float?) ||
            propertyType == typeof(double) || propertyType == typeof(double?))
            return InputType.Number;

        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?) ||
            propertyType == typeof(DateTimeOffset) || propertyType == typeof(DateTimeOffset?))
            return InputType.Date;

        if (propertyType == typeof(Enum) || propertyType.IsAssignableTo(typeof(IList<>)))
            return InputType.Selection;
        
        return InputType.Text;
    }
    
    public static Dictionary<string, PropInfo> ExtractPropertyInfo(Type type)
    {
        if (_propertyCache.ContainsKey(type))
            return _propertyCache[type];

        var props = type.GetProperties()
            .Where(x => x.SetMethod is not null)
            .ToList();

        Dictionary<string, PropInfo> results = new();
        foreach (var prop in props)
        {
            var requiredAttribute = prop.GetCustomAttribute<RequiredAttribute>();
            var hiddenAttribute = prop.GetCustomAttribute<HiddenInputAttribute>();
            
            var displayName = prop.Name;
                        
            var displayNameAttribute = prop.GetCustomAttribute<DisplayNameAttribute>();
            
            if (displayNameAttribute is not null && !string.IsNullOrEmpty(displayNameAttribute.DisplayName))
                displayName = displayNameAttribute.DisplayName;
            else
            {
                var displayAttribute = prop.GetCustomAttribute<DisplayAttribute>();
                if (displayAttribute is not null && !string.IsNullOrEmpty(displayAttribute.Name))
                    displayName = displayAttribute.Name;
            }

            results.Add(prop.Name, new(displayName, prop, hiddenAttribute is not null, requiredAttribute is not null));
        }

        _propertyCache.Add(type, results);
        return results;
    }
    public static Dictionary<string, PropInfo> ExtractPropertyInfo<T>() => ExtractPropertyInfo(typeof(T));
}