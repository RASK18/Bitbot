using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
// ReSharper disable UnusedMember.Global

namespace Bitbot
{
    public static class Extensions
    {
        public static decimal Round(this decimal d) => Math.Round(d, 2);

        public static string GetDescription(this Enum e) => e.GetType().GetField(e.ToString()).GetDescription();

        public static string GetDescription(this ICustomAttributeProvider f) =>
            f.GetCustomAttributes(false)
             .OfType<DescriptionAttribute>()
             .SingleOrDefault()?
             .Description;

        public static T? GetEnum<T>(this string s) where T : struct, Enum =>
            (T?)typeof(T)
                .GetFields()
                .SingleOrDefault(f => f.GetDescription() == s)?
                .GetValue(null);
    }
}
