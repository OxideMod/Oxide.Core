﻿using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Checks if a obj is inherited from type or is a child type
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TQuery"></typeparam>
        /// <param name="source"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool IsRelatedTo<TSource, TQuery>(this TSource source, TQuery query) => typeof(TSource).IsRelatedTo(query.GetType());

        /// <summary>
        /// Checks if a obj is inherited from type or is a child type
        /// </summary>
        /// <typeparam name="TQuery"></typeparam>
        /// <param name="source"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool IsRelatedTo<TQuery>(this Type source, TQuery query) => source.IsRelatedTo(query.GetType());

        /// <summary>
        /// Checks if a obj is inherited from type or is a child type
        /// </summary>
        /// <param name="source"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool IsRelatedTo(this Type source, Type query)
        {
            if (query.IsAssignableFrom(source))
            {
                return true;
            }
            else if (source.DeclaringType != null)
            {
                return source.DeclaringType.IsRelatedTo(query);
            }

            return false;
        }
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

        /// <summary>
        /// Checks if a obj is inherited from type or is a child type
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TQuery"></typeparam>
        /// <param name="source"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool IsRelatedTo<TSource, TQuery>(this TSource source, TQuery query) => typeof(TSource).IsRelatedTo(query.GetType());

        /// <summary>
        /// Checks if a obj is inherited from type or is a child type
        /// </summary>
        /// <typeparam name="TQuery"></typeparam>
        /// <param name="source"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool IsRelatedTo<TQuery>(this Type source, TQuery query) => source.IsRelatedTo(query.GetType());

        /// <summary>
        /// Checks if a obj is inherited from type or is a child type
        /// </summary>
        /// <param name="source"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static bool IsRelatedTo(this Type source, Type query)
        {
            if (query.IsAssignableFrom(source))
            {
                return true;
            }
            else if (source.DeclaringType != null)
            {
                return source.DeclaringType.IsRelatedTo(query);
            }

            return false;
        }
    }
}
