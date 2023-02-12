using Oxide.Core.Libraries.Covalence;
using Oxide.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Oxide
{
    public static class ExtensionMethods
    {
        #region Service Extensions

        public static TService GetService<TService>(this IServiceProvider provider) => (TService)provider.GetService(typeof(TService));
        
        public static IServiceCollection Singleton(this IServiceCollection collection, Type serviceType, Type implementationType) => collection.AddService(ServiceDescriptor.CreateSingleton(serviceType, implementationType));

        public static IServiceCollection Singleton(this IServiceCollection collection, Type serviceType, object implementation) => collection.AddService(ServiceDescriptor.CreateSingleton(serviceType, null, implementation));

        public static IServiceCollection Singleton<TService, TImplementation>(this IServiceCollection collection) where TImplementation : TService => collection.Singleton(typeof(TService), typeof(TImplementation));

        public static IServiceCollection Singleton<TService, TImplementation>(this IServiceCollection collection, TImplementation implementation) where TImplementation : TService => collection.Singleton(typeof(TService), implementation);

        public static IServiceCollection Singleton<TImplementation>(this IServiceCollection collection, TImplementation implementation) => collection.Singleton(implementation.GetType(), implementation);

        public static IServiceCollection Transient(this IServiceCollection collection, Type serviceType, Type implementationType, Delegate factory) => collection.AddService(ServiceDescriptor.CreateTransient(serviceType, implementationType, factory));

        public static IServiceCollection Transient(this IServiceCollection collection, Type serviceType, Type implementationType) => collection.Transient(serviceType, implementationType, null);

        public static IServiceCollection Transient(this IServiceCollection collection, Type implementationType) => collection.Transient(implementationType, implementationType, null);

        public static IServiceCollection Transient(this IServiceCollection collection, Type implementationType, Delegate factory) => collection.Transient(implementationType, implementationType, factory);

        public static IServiceCollection Transient<TService, TImplementation>(this IServiceCollection collection, Func<IServiceProvider, TImplementation> factory) where TImplementation : TService => collection.Transient(typeof(TService), typeof(TImplementation), factory);

        public static IServiceCollection Transient<TService, TImplementation>(this IServiceCollection collection) where TImplementation : TService => collection.Transient(typeof(TService), typeof(TImplementation));

        public static IServiceCollection Transient<TImplementation>(this IServiceCollection collection, Func<IServiceProvider, TImplementation> factory) => collection.Transient(typeof(TImplementation), typeof(TImplementation), factory);

        public static IServiceCollection Transient<TImplementation>(this IServiceCollection collection) => collection.Transient(typeof(TImplementation), typeof(TImplementation));

        #endregion
    }
}

namespace Oxide.Core
{
    /// <summary>
    /// Useful extension methods which are added to base types
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns the last portion of a path separated by slashes
        /// </summary>
        public static string Basename(this string text, string extension = null)
        {
            if (extension != null)
            {
                if (extension.Equals("*.*"))
                {
                    // Return the name excluding any extension
                    Match match = Regex.Match(text, @"([^\\/]+)\.[^\.]+$");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
                else
                {
                    // Return the name excluding the given extension
                    if (extension[0] == '*')
                    {
                        extension = extension.Substring(1);
                    }

                    return Regex.Match(text, @"([^\\/]+)\" + extension + "+$").Groups[1].Value;
                }
            }
            // No extension was given or the path has no extension, return the full file name
            return Regex.Match(text, @"[^\\/]+$").Groups[0].Value;
        }

        /// <summary>
        /// Checks if an array contains a specific item
        /// </summary>
        public static bool Contains<T>(this T[] array, T value)
        {
            foreach (T item in array)
            {
                if (item.Equals(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the directory portion of a path separated by slashes
        /// </summary>
        public static string Dirname(this string text) => Regex.Match(text, "(.+)[\\/][^\\/]+$").Groups[1].Value;

        /// <summary>
        /// Converts PascalCase and camelCase to multiple words
        /// </summary>
        public static string Humanize(this string name) => Regex.Replace(name, @"(\B[A-Z])", " $1");

        /// <summary>
        /// Checks if a string is a valid 64-bit Steam ID
        /// </summary>
        public static bool IsSteamId(this string id)
        {
            return ulong.TryParse(id, out ulong targetId) && targetId > 76561197960265728ul;
        }

        /// <summary>
        /// Checks if a ulong is a valid 64-bit Steam ID
        /// </summary>
        public static bool IsSteamId(this ulong id) => id > 76561197960265728ul;

        /// <summary>
        /// Converts a string to plain text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Plaintext(this string text) => Formatter.ToPlaintext(text);

        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string QuoteSafe(this string text) => "\"" + text.Replace("\"", "\\\"").TrimEnd('\\') + "\"";

        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Quote(this string text) => QuoteSafe(text);

        /// <summary>
        /// Returns a random value from an array
        /// </summary>
        public static T Sample<T>(this T[] array) => array[Core.Random.Range(0, array.Length)];

        /// <summary>
        /// Converts a string into a sanitized string for string.Format
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Sanitize(this string text) => text.Replace("{", "{{").Replace("}", "}}");

        /// <summary>
        /// Converts a string to Sentence case
        /// </summary>
        public static string SentenceCase(this string text)
        {
            Regex regex = new Regex(@"(^[a-z])|\.\s+(.)", RegexOptions.ExplicitCapture);
            return regex.Replace(text.ToLower(), s => s.Value.ToUpper());
        }

        /// <summary>
        /// Converts a string to Title Case
        /// </summary>
        public static string TitleCase(this string text)
        {
            return CultureInfo.InstalledUICulture.TextInfo.ToTitleCase(text.Contains('_') ? text.Replace('_', ' ') : text);
        }

        /// <summary>
        /// Converts a string to Title Case
        /// </summary>
        public static string Titleize(this string text) => TitleCase(text);

        /// <summary>
        /// Turns an array of strings into a sentence
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static string ToSentence<T>(this IEnumerable<T> items)
        {
            IEnumerator<T> enumerator = items.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return string.Empty;
            }

            T firstItem = enumerator.Current;
            if (!enumerator.MoveNext())
            {
                return firstItem?.ToString();
            }

            StringBuilder builder = new StringBuilder(firstItem?.ToString());
            bool moreItems = true;
            while (moreItems)
            {
                T item = enumerator.Current;
                moreItems = enumerator.MoveNext();
                builder.Append(moreItems ? ", " : " and ");
                builder.Append(item);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Shortens a string to the length specified
        /// </summary>
        /// <param name="text"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static string Truncate(this string text, int max) => text.Length <= max ? text : text.Substring(0, max) + " ...";
    }
}

namespace Oxide.Plugins
{
    /// <summary>
    /// Useful extension methods which are added to base types
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns the last portion of a path separated by slashes
        /// </summary>
        public static string Basename(this string text, string extension = null)
        {
            if (extension != null)
            {
                if (extension.Equals("*.*"))
                {
                    // Return the name excluding any extension
                    Match match = Regex.Match(text, @"([^\\/]+)\.[^\.]+$");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
                else
                {
                    // Return the name excluding the given extension
                    if (extension[0] == '*')
                    {
                        extension = extension.Substring(1);
                    }

                    return Regex.Match(text, @"([^\\/]+)\" + extension + "+$").Groups[1].Value;
                }
            }
            // No extension was given or the path has no extension, return the full file name
            return Regex.Match(text, @"[^\\/]+$").Groups[0].Value;
        }

        /// <summary>
        /// Checks if an array contains a specific item
        /// </summary>
        public static bool Contains<T>(this T[] array, T value)
        {
            foreach (T item in array)
            {
                if (item.Equals(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the directory portion of a path separated by slashes
        /// </summary>
        public static string Dirname(this string text) => Regex.Match(text, "(.+)[\\/][^\\/]+$").Groups[1].Value;

        /// <summary>
        /// Converts PascalCase and camelCase to multiple words
        /// </summary>
        public static string Humanize(this string name) => Regex.Replace(name, @"(\B[A-Z])", " $1");

        /// <summary>
        /// Checks if a string is a valid 64-bit Steam ID
        /// </summary>
        public static bool IsSteamId(this string id)
        {
            return ulong.TryParse(id, out ulong targetId) && targetId > 76561197960265728ul;
        }

        /// <summary>
        /// Checks if a ulong is a valid 64-bit Steam ID
        /// </summary>
        public static bool IsSteamId(this ulong id) => id > 76561197960265728ul;

        /// <summary>
        /// Converts a string to plain text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Plaintext(this string text) => Formatter.ToPlaintext(text);

        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string QuoteSafe(this string text) => "\"" + text.Replace("\"", "\\\"").TrimEnd('\\') + "\"";

        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Quote(this string text) => QuoteSafe(text);

        /// <summary>
        /// Returns a random value from an array
        /// </summary>
        public static T Sample<T>(this T[] array) => array[Core.Random.Range(0, array.Length)];

        /// <summary>
        /// Converts a string into a sanitized string for string.Format
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Sanitize(this string text) => text.Replace("{", "{{").Replace("}", "}}");

        /// <summary>
        /// Converts a string to Sentence case
        /// </summary>
        public static string SentenceCase(this string text)
        {
            Regex regex = new Regex(@"(^[a-z])|\.\s+(.)", RegexOptions.ExplicitCapture);
            return regex.Replace(text.ToLower(), s => s.Value.ToUpper());
        }

        /// <summary>
        /// Converts a string to Title Case
        /// </summary>
        public static string TitleCase(this string text)
        {
            return CultureInfo.InstalledUICulture.TextInfo.ToTitleCase(text.Contains('_') ? text.Replace('_', ' ') : text);
        }

        /// <summary>
        /// Converts a string to Title Case
        /// </summary>
        public static string Titleize(this string text) => TitleCase(text);

        /// <summary>
        /// Turns an array of strings into a sentence
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static string ToSentence<T>(this IEnumerable<T> items)
        {
            IEnumerator<T> enumerator = items.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return string.Empty;
            }

            T firstItem = enumerator.Current;
            if (!enumerator.MoveNext())
            {
                return firstItem?.ToString();
            }

            StringBuilder builder = new StringBuilder(firstItem?.ToString());
            bool moreItems = true;
            while (moreItems)
            {
                T item = enumerator.Current;
                moreItems = enumerator.MoveNext();
                builder.Append(moreItems ? ", " : " and ");
                builder.Append(item);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Shortens a string to the length specified
        /// </summary>
        /// <param name="text"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static string Truncate(this string text, int max) => text.Length <= max ? text : text.Substring(0, max) + " ...";
    }
}
