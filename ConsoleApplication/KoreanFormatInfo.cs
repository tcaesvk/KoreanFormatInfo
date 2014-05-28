using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleApplication
{
    public class KoreanFormatInfo : IFormatProvider, ICustomFormatter
    {
        static readonly Lazy<KoreanFormatInfo> invariantInfo = new Lazy<KoreanFormatInfo>(() => new KoreanFormatInfo(CultureInfo.InvariantCulture));
        public static KoreanFormatInfo InvariantInfo { get { return invariantInfo.Value; } }
        static readonly Lazy<KoreanFormatInfo> currentInfo = new Lazy<KoreanFormatInfo>(() => new KoreanFormatInfo(CultureInfo.CurrentCulture));
        public static KoreanFormatInfo CurrentInfo { get { return currentInfo.Value; } }

        public static KoreanFormatInfo GetFormatInfo(string name)
        {
            return new KoreanFormatInfo(CultureInfo.GetCultureInfo(name));
        }

        static readonly Regex formatRegex = new Regex(@"^((?<format>.*)-)?(?<postposition>(\((?<prefix>\p{IsHangulSyllables}+)\)|(?<consonant>\p{IsHangulSyllables}+)/)(?<vowel>\p{IsHangulSyllables}+))$", RegexOptions.Compiled);
        static readonly Regex latinRegex = new Regex(@"(?<latin>[A-Za-z]+)|(?<nonLatin>[^A-Za-z]+)", RegexOptions.Compiled);
        static readonly Regex englishEndsWithVowelRegex = new Regex("([aiuo]r*|[^lmn]e|[^aeioubdg][lmn]e|[ptk]r[aeiou][lmn]e|([aeiou](h+y*|w+|y+)|[aeiu]{2}|[aeiu]o|o[aeiu]|[aeiou]{3,}|[aeiou]([bdglmnv]|th)[aeiou]|[a-z-[aeiou]])[ptk]|[^m][bd]|[^n]g|[hry]|([^c]|[^n]c)s|[zfv]|[ao]w)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly HashSet<char> charactersEndWithVowel = new HashSet<char>(new[] { '2', '4', '5', '9' });
        static readonly HashSet<char> charactersEndWithFinalConsonant = new HashSet<char>(new[] { '0', '1', '3', '6', '7', '8' });
        static readonly HashSet<string> symbolsEndWithVowel = new HashSet<string>(new[]
        {
            // 화폐
            // "¤",
            NumberFormatInfo.InvariantInfo.CurrencySymbol, 
            // 음의 무한대
            // "-Infinity",
            NumberFormatInfo.InvariantInfo.NegativeInfinitySymbol,
            // 퍼센트
            // "%",
            NumberFormatInfo.InvariantInfo.PercentSymbol,
            // 양의 무한대
            // "Infinity",
            NumberFormatInfo.InvariantInfo.PositiveInfinitySymbol,
        });
        static readonly HashSet<string> symbolsEndWithFinalConsonant = new HashSet<string>(new[]
        {
            // 숫자가 아님
            // "NaN",
            NumberFormatInfo.InvariantInfo.NaNSymbol,
            // 퍼밀
            // "‰",
            NumberFormatInfo.InvariantInfo.PerMilleSymbol,
        });

        static KoreanFormatInfo()
        {
            var regionInfoArray = (from ci in CultureInfo.GetCultures(CultureTypes.AllCultures)
                                   where !string.IsNullOrEmpty(ci.Name) && !ci.IsNeutralCulture
                                   let nfi = NumberFormatInfo.GetInstance(ci)
                                   where nfi != null
                                   let ri = new RegionInfo(ci.Name)
                                   orderby ri.ISOCurrencySymbol
                                   select ri).ToArray();
            var currencies = new HashSet<string>(from ri in regionInfoArray select ri.CurrencySymbol);
            foreach (var ri in regionInfoArray)
            {
                var match = englishEndsWithVowelRegex.Match(ri.CurrencyEnglishName);
                (match.Success ? symbolsEndWithVowel : symbolsEndWithFinalConsonant).Add(ri.CurrencySymbol);
            }
            symbolsEndWithVowel.Remove("¥");
            symbolsEndWithFinalConsonant.Remove("Br");
        }

        static bool? CheckEndsWithFinalConsonant(char c)
        {
            if ('가' <= c && c <= '힣') return (c - '가') % 28 != 0;
            if (charactersEndWithVowel.Contains(c)) return false;
            if (charactersEndWithFinalConsonant.Contains(c)) return true;
            return null;
        }

        static bool? CheckEndsWithFinalConsonant(string s)
        {
            if (s == null) throw new ArgumentNullException("s");
            if (symbolsEndWithVowel.Contains(s)) return symbolsEndWithFinalConsonant.Contains(s) ? null as bool? : false;
            if (symbolsEndWithFinalConsonant.Contains(s)) return true;
            foreach (var match in latinRegex.Matches(s).OfType<Match>().Reverse())
            {
                if (match.Groups["latin"].Success) return !englishEndsWithVowelRegex.Match(match.Groups["latin"].Value).Success;
                var nonLatin = match.Groups["nonLatin"].Value;
                for (int i = nonLatin.Length - 1; 0 <= i; --i)
                {
                    var result = CheckEndsWithFinalConsonant(nonLatin[i]);
                    if (result != null) return result;
                }
            }
            return null;
        }

        readonly IFormatProvider formatProvider;

        KoreanFormatInfo(IFormatProvider provider)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            this.formatProvider = provider;
        }

        #region IFormatProvider Members

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter)) return this;
            return formatProvider.GetFormat(formatType);
        }

        #endregion

        #region ICustomFormatter Members

        string ICustomFormatter.Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (formatProvider as KoreanFormatInfo == null)
            {
                var formattable = arg as IFormattable;
                return formattable != null ? formattable.ToString(format, this.formatProvider) : arg != null ? arg.ToString() : string.Empty;
            }
            if (string.IsNullOrEmpty(format))
            {
                var formattable = arg as IFormattable;
                return formattable != null ? formattable.ToString(format, this.formatProvider) : arg != null ? arg.ToString() : string.Empty;
            }
            var match = formatRegex.Match(format);
            if (!match.Success)
            {
                var formattable = arg as IFormattable;
                return formattable != null ? formattable.ToString(format, this.formatProvider) : arg != null ? arg.ToString() : string.Empty;
            }
            string s;
            bool? hasFinalConsonant;
            if (arg == null)
            {
                s = null;
                hasFinalConsonant = null;
            }
            else
            {
                var formattable = arg as IFormattable;
                if (formattable == null)
                {
                    s = arg.ToString();
                    hasFinalConsonant = CheckEndsWithFinalConsonant(s);
                }
                else
                {
                    var fmt = match.Groups["format"].Value;
                    s = formattable.ToString(fmt, formatProvider);
                    var convertible = arg as IConvertible;
                    switch (convertible != null ? convertible.GetTypeCode() : TypeCode.Empty)
                    {
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                            var nfi = (formatProvider.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo) ?? NumberFormatInfo.CurrentInfo;
                            if (s == nfi.NaNSymbol || s == nfi.PositiveInfinitySymbol || s == nfi.NegativeInfinitySymbol) goto default;
                            if (fmt.StartsWith("c", StringComparison.OrdinalIgnoreCase))
                            {
                                hasFinalConsonant = CheckEndsWithFinalConsonant(nfi.CurrencySymbol);
                                break;
                            }
                            if (fmt.StartsWith("p", StringComparison.OrdinalIgnoreCase))
                            {
                                hasFinalConsonant = CheckEndsWithFinalConsonant(nfi.PercentSymbol);
                                break;
                            }
                            goto default;

                        default:
                            hasFinalConsonant = CheckEndsWithFinalConsonant(s);
                            break;
                    }
                }
            }
            if (hasFinalConsonant == null) return (s ?? string.Empty) + match.Groups["postposition"];
            var consonant = match.Groups["consonant"].Value;
            var vowel = match.Groups["vowel"].Value;
            if (string.IsNullOrEmpty(consonant))
            {
                consonant = match.Groups["prefix"].Value + vowel;
            }
            return s + (hasFinalConsonant.Value ? consonant : vowel);
        }

        #endregion
    }
}
