using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Borland.CodeAnalyzer
{
    public static class Extensions
    {
        public static LocalizableString GetLocalizableString(this string nameOfLocalizableResource)
            => new LocalizableResourceString(nameOfLocalizableResource, Resources.ResourceManager, typeof(Resources));
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var element in enumerable)
                action(element);
        }
    }
}
