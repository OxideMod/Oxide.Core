// A custom markup language for Oxide
//
// Text 		::= {Element}
// Element 		::= String | Bold | Italic | Color | Size
// String		::= {? any character ?}
// Bold			::= "[b]" Text "[/b]"
// Italic		::= "[i]" Text "[/i]"
// Color		::= "[#" ColorValue "]" Text "[/#]"
// ColorValue	::=	RGB | RGBA | Name
// RGB			::= 6 * HexDigit
// RGBA			::= 8 * HexDigit
// HexDigit		::= Digit | "a" | "A" | "b" | "B" | "c" | "C" | "d" | "D" | "e" | "E" | "f" | "F"
// Name			::= "aqua" | "black" | "blue" | "brown" | "cyan" | "darkblue"
//					| "fuchsia" | "green" | "grey" | "lightblue" | "lime"
//					| "magenta" | "maroon" | "navy" | "olive" | "orange"
//					| "purple" | "red" | "silver" | "teal" | "white" | "yellow"
//					? any casing allowed ?
// Size			::= "[+" Integer "]" Text "[/+]"
// Integer		::= Digit {Digit}
// Digit 		::= "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"

using System;
using System.Collections.Generic;
using System.Text;
using Oxide.Pooling;

namespace Oxide.Core.Libraries.Covalence
{
    public class Element : Formatter.Poolable<Element>
    {
        public ElementType Type;
        public object Val;
        public List<Element> Body = new List<Element>();

        public Element() {}

        public Element(ElementType type, object val)
        {
            Type = type;
            Val = val;
        }

        private static Element Get(ElementType type, object val, bool shouldPool)
        {
            if (!shouldPool)
                return new Element(type, val);

            Element e = TakeFromPool();
            e.Type = type;
            e.Val = val;
            return e;
        }

        protected override void Reset()
        {
            Type = default;
            Val = default;
            ReturnToPool(Body);
        }

        public static Element String(object s, bool shouldPool = false) => Element.Get(ElementType.String, s, shouldPool);

        public static Element Tag(ElementType type, bool shouldPool = false) => Element.Get(type, null, shouldPool);

        public static Element ParamTag(ElementType type, object val, bool shouldPool = false) => Element.Get(type, val, shouldPool);
    }

    public enum ElementType { String, Bold, Italic, Color, Size }

    public class Formatter
    {
        private static readonly Dictionary<string, string> colorNames = new Dictionary<string, string>
        {
            ["aqua"] = "00ffff",
            ["black"] = "000000",
            ["blue"] = "0000ff",
            ["brown"] = "a52a2a",
            ["cyan"] = "00ffff",
            ["darkblue"] = "0000a0",
            ["fuchsia"] = "ff00ff",
            ["green"] = "008000",
            ["grey"] = "808080",
            ["lightblue"] = "add8e6",
            ["lime"] = "00ff00",
            ["magenta"] = "ff00ff",
            ["maroon"] = "800000",
            ["navy"] = "000080",
            ["olive"] = "808000",
            ["orange"] = "ffa500",
            ["purple"] = "800080",
            ["red"] = "ff0000",
            ["silver"] = "c0c0c0",
            ["teal"] = "008080",
            ["white"] = "ffffff",
            ["yellow"] = "ffff00"
        };

        private static readonly Tag emptyTag = new Tag(string.Empty, string.Empty);
        private static readonly Tag boldTag = new Tag("<b>", "</b>");
        private static readonly Tag italicTag = new Tag("<i>", "</i>");

        private static readonly Dictionary<ElementType, Func<object, Tag>> plainTextTranslations =
            new Dictionary<ElementType, Func<object, Tag>>();

        private static readonly Dictionary<ElementType, Func<object, Tag>> unityTranslations =
            new Dictionary<ElementType, Func<object, Tag>>
            {
                [ElementType.Bold] = _ => boldTag,
                [ElementType.Italic] = _ => italicTag,
                [ElementType.Color] = c => new Tag($"<color=#{c}>", "</color>"),
                [ElementType.Size] = s => new Tag($"<size={s}>", "</size>")
            };

        private static readonly new Dictionary<ElementType, Func<object, Tag>> rustLegacyTranslations =
            new Dictionary<ElementType, Func<object, Tag>>
            {
                [ElementType.Color] = c => new Tag($"[color #{RGBAtoRGB(c)}]", "[color #ffffff]")
            };

        private static readonly Dictionary<ElementType, Func<object, Tag>> rokAnd7DTDTranslations =
            new Dictionary<ElementType, Func<object, Tag>>
            {
                [ElementType.Color] = c => new Tag($"[{RGBAtoRGB(c)}]", "[e7e7e7]")
            };

        private static readonly Dictionary<ElementType, Func<object, Tag>> terrariaTranslations =
            new Dictionary<ElementType, Func<object, Tag>>
            {
                [ElementType.Color] = c => new Tag($"[c/{RGBAtoRGB(c)}:", "]")
            };

        private static readonly IPoolProvider<StringBuilder> stringPool = Interface.Oxide.PoolFactory.GetProvider<StringBuilder>();

        private class Token : Poolable<Token>
        {
            public TokenType Type;
            public object Val;
            public string Pattern;
            private static readonly Stack<Token> pool = new Stack<Token>();

            public static Token TakeFromPool(TokenType type, object val, string pattern)
            {
                Token t = TakeFromPool();
                t.Type = type;
                t.Val = val;
                t.Pattern = pattern;
                return t;
            }

            protected override void Reset()
            {
                Type = default;
                Val = default;
                Pattern = default;
            }
        }

        private enum TokenType
        { String, Bold, Italic, Color, Size, CloseBold, CloseItalic, CloseColor, CloseSize }

        private static readonly Dictionary<ElementType, TokenType?> closeTags = new Dictionary<ElementType, TokenType?>
        {
            [ElementType.String] = null,
            [ElementType.Bold] = TokenType.CloseBold,
            [ElementType.Italic] = TokenType.CloseItalic,
            [ElementType.Color] = TokenType.CloseColor,
            [ElementType.Size] = TokenType.CloseSize
        };

        private class Lexer : Poolable<Lexer>
        {
            private enum StateType
            {
                Str,
                Tag,
                CloseTag,
                EndTag,
                ParamTag
            }

            private List<Token> tokens = new List<Token>();
            private int patternStart;
            private int tokenStart;
            private int position;
            private string text;
            private StateType state;
            private TokenType currentTokenType;
            private Func<string, object> currentParser;

            private char Current() => text[position];

            private void Next() => position++;

            private void StartNewToken() => tokenStart = position;

            private void StartNewPattern()
            {
                patternStart = position;
                StartNewToken();
            }

            private void ResetTokenStart() => tokenStart = patternStart;

            private string Token() => text.Substring(tokenStart, position - tokenStart);

            private void Add(TokenType type, object val = null)
            {
                Token t = Formatter.Token.TakeFromPool(type, val, text.Substring(patternStart, position - patternStart));
                tokens.Add(t);
            }

            private void WritePatternString()
            {
                if (patternStart >= position)
                {
                    return;
                }

                int ts = tokenStart;
                tokenStart = patternStart;
                Add(TokenType.String, Token());
                tokenStart = ts;
            }

            private static bool IsValidColorCode(string val)
            {
                if (val.Length != 6 && val.Length != 8)
                    return false;

                for (int i = 0; i < val.Length; i++)
                {
                    char c = val[i];
                    if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                        return false;
                }

                return true;
            }

            private static object ParseColor(string val)
            {
                if (!colorNames.TryGetValue(val.ToLower(), out string color) && !IsValidColorCode(val))
                {
                    return null;
                }
                color = color ?? val;
                if (color.Length == 6)
                {
                    color += "ff";
                }
                return color;
            }

            private static object ParseSize(string val)
            {
                if (int.TryParse(val, out int size)) { return size; }
                return null;
            }

            // End of tag (]), transition back to Str
            private void EndTag()
            {
                if (Current() == ']')
                {
                    Next();
                    Add(currentTokenType);
                    StartNewPattern();
                    state = StateType.Str;
                }
                else
                {
                    ResetTokenStart();
                    state = StateType.Str;
                }
            }

            // Start of param tag ([# or [+), read and parse param
            private void ParamTag()
            {
                if (Current() != ']')
                {
                    Next();
                }
                else
                {
                    object parsed = currentParser(Token());
                    if (parsed == null)
                    {
                        ResetTokenStart();
                        state = StateType.Str;
                    }
                    else
                    {
                        Next();
                        Add(currentTokenType, parsed);
                        StartNewPattern();
                        state = StateType.Str;
                    }
                }
            }

            // Start of close tag ([/), trying to identify close tag
            private void CloseTag()
            {
                switch (Current())
                {
                    case 'b':
                        currentTokenType = TokenType.CloseBold;
                        Next();
                        state = StateType.EndTag;
                        break;

                    case 'i':
                        currentTokenType = TokenType.CloseItalic;
                        Next();
                        state = StateType.EndTag;
                        break;

                    case '#':
                        currentTokenType = TokenType.CloseColor;
                        Next();
                        state = StateType.EndTag;
                        break;

                    case '+':
                        currentTokenType = TokenType.CloseSize;
                        Next();
                        state = StateType.EndTag;
                        break;

                    default:
                        ResetTokenStart();
                        state = StateType.Str;
                        break;
                }
            }

            // Start of tag ([), trying to identify tag
            private void Tag()
            {
                switch (Current())
                {
                    case 'b':
                        currentTokenType = TokenType.Bold;
                        Next();
                        state = StateType.EndTag;
                        break;

                    case 'i':
                        currentTokenType = TokenType.Italic;
                        Next();
                        state = StateType.EndTag;
                        break;

                    case '#':
                        currentTokenType = TokenType.Color;
                        currentParser = ParseColor;
                        Next();
                        StartNewToken();
                        state = StateType.ParamTag;
                        break;

                    case '+':
                        currentTokenType = TokenType.Size;
                        currentParser = ParseSize;
                        Next();
                        StartNewToken();
                        state = StateType.ParamTag;
                        break;

                    case '/':
                        Next();
                        state = StateType.CloseTag;
                        break;

                    default:
                        ResetTokenStart();
                        state = StateType.Str;
                        break;
                }
            }

            // Any string, trying to find a tag with ([)
            private void Str()
            {
                if (Current() == '[')
                {
                    WritePatternString();
                    StartNewPattern();
                    Next();
                    state = StateType.Tag;
                }
                else
                {
                    Next();
                }
            }

            public static List<Token> Lex(string text)
            {
                Lexer lexer = new Lexer();
                return lexer.TokenizeText(text);
            }

            public List<Token> TokenizeText(string text)
            {
                // A pattern is the full pattern of a token, e.g. [#foo] instead of just foo as token
                // When we reach eof or an error we want to default to using a string as token
                // To accomplish this, we need the full pattern instead of just the token
                this.text = text;

                while (position < text.Length)
                {
                    // Process the current character based on the current state
                    switch (state)
                    {
                        case StateType.Str:
                            Str();
                            break;

                        case StateType.Tag:
                            Tag();
                            break;

                        case StateType.CloseTag:
                            CloseTag();
                            break;

                        case StateType.EndTag:
                            EndTag();
                            break;

                        case StateType.ParamTag:
                            ParamTag();
                            break;
                    }
                }

                // Flush leftover pattern
                WritePatternString();
                return tokens;
            }

            protected override void Reset()
            {
                text = default;
                patternStart = default;
                tokenStart = default;
                position = default;
                Formatter.Token.ReturnToPool(tokens);
                currentTokenType = default;
                currentParser = default;
                state = StateType.Str;
            }
        }

        private class Entry : Poolable<Entry>
        {
            public string Pattern;
            public Element Element;

            static public Entry TakeFromPool(string pattern, Element element)
            {
                Entry e = TakeFromPool();
                e.Pattern = pattern;
                e.Element = element;
                return e;
            }

            protected override void Reset()
            {
                Pattern = default;
                Element = default;
            }
        }

        private class ElementTreeBuilder : Poolable<ElementTreeBuilder>
        {
            private readonly Stack<Entry> entries = new Stack<Entry>();
            public bool shouldPoolElements;

            public Element ProcessTokens(List<Token> tokens)
            {
                int i = 0;
                entries.Clear();
                entries.Push(Entry.TakeFromPool(null, Element.Tag(ElementType.String, shouldPoolElements)));
                while (i < tokens.Count)
                {
                    Token t = tokens[i++];
                    Element e = entries.Peek().Element;
                    if (t.Type == closeTags[e.Type])
                    {
                        // Last tag was closed, pop tag and add to parent
                        entries.Pop();
                        entries.Peek().Element.Body.Add(e);
                        continue;
                    }

                    // open new tags on bold, italic, color & size.
                    // Add strings and invalid tags as strings to body of current tag
                    switch (t.Type)
                    {
                        case TokenType.String:
                            e.Body.Add(Element.String(t.Val, shouldPoolElements));
                            break;

                        case TokenType.Bold:
                            entries.Push(Entry.TakeFromPool(t.Pattern,
                                Element.Tag(ElementType.Bold, shouldPoolElements)));
                            break;

                        case TokenType.Italic:
                            entries.Push(Entry.TakeFromPool(t.Pattern,
                                Element.Tag(ElementType.Italic, shouldPoolElements)));
                            break;

                        case TokenType.Color:
                            entries.Push(Entry.TakeFromPool(t.Pattern,
                                Element.ParamTag(ElementType.Color, t.Val, shouldPoolElements)));
                            break;

                        case TokenType.Size:
                            entries.Push(Entry.TakeFromPool(t.Pattern,
                                Element.ParamTag(ElementType.Size, t.Val, shouldPoolElements)));
                            break;

                        default:
                            e.Body.Add(Element.String(t.Pattern, shouldPoolElements));
                            break;
                    }
                }

                // Stringify all tags that weren't closed at EOF
                while (entries.Count > 1)
                {
                    Entry e = entries.Pop();
                    List<Element> body = entries.Peek().Element.Body;
                    body.Add(Element.String(e.Pattern));
                    body.AddRange(e.Element.Body);
                    Entry.ReturnToPool(e);
                }

                Entry lastEntry = entries.Pop();
                Element element = lastEntry.Element;
                Entry.ReturnToPool(lastEntry);
                return element;
            }

            protected override void Reset()
            {
                shouldPoolElements = default;
                while (entries.Count > 0)
                    Entry.ReturnToPool(entries.Pop());
            }
        }

        public static List<Element> Parse(string text) => ParseText(text, false).Body;

        private static Element ParseText(string text, bool shouldPoolElements = true)
        {
            Lexer lexer = Lexer.TakeFromPool();
            try
            {
                return ProcessTokens(lexer.TokenizeText(text), shouldPoolElements);
            }
            finally
            {
                Lexer.ReturnToPool(lexer);
            }
        }

        private static Element ProcessTokens(List<Token> tokens, bool shouldPoolElements)
        {
            ElementTreeBuilder elementTreeBuilder = ElementTreeBuilder.TakeFromPool();
            try
            {
                elementTreeBuilder.shouldPoolElements = shouldPoolElements;
                return elementTreeBuilder.ProcessTokens(tokens);
            }
            finally
            {
                ElementTreeBuilder.ReturnToPool(elementTreeBuilder);
            }
        }

        private class Tag
        {
            public string Open;
            public string Close;

            public Tag(string open, string close)
            {
                Open = open;
                Close = close;
            }
        }

        private static Tag Translation(Element e, Dictionary<ElementType, Func<object, Tag>> translations)
        {
            return translations.TryGetValue(e.Type, out Func<object, Tag> parse) ? parse(e.Val) : emptyTag;
        }

        private static string ToTreeFormat(List<Element> tree, Dictionary<ElementType, Func<object, Tag>> translations)
        {
            StringBuilder sb = stringPool.Take();
            try
            {
                AppendTreeFormat(tree, translations, sb);
                return sb.ToString();
            }
            finally
            {
                stringPool.Return(sb);
            }
        }

        private static void AppendTreeFormat(List<Element> tree, Dictionary<ElementType, Func<object, Formatter.Tag>> translations, StringBuilder sb)
        {
            // translation(string) 	= string_value
            // translation(tree) 	= open_tag_translation
            //                      + translation(child_1) + translation(child_2) + ... + translation(child_n)
            //                      + close_tag_translation
            foreach (Element e in tree)
            {
                if (e.Type == ElementType.String)
                {
                    sb.Append(e.Val);
                    continue;
                }
                Tag tag = Translation(e, translations);
                sb.Append(tag.Open);
                AppendTreeFormat(e.Body, translations, sb);
                sb.Append(tag.Close);
            }
        }

        private static string ToTreeFormat(string text, Dictionary<ElementType, Func<object, Tag>> translations)
        {
            Element element = ParseText(text);
            try
            {
                return ToTreeFormat(element.Body, translations);
            }
            finally
            {
                Element.ReturnToPool(element);
            }
        }

        private static string RGBAtoRGB(object rgba) => rgba.ToString().Substring(0, 6);

        public static string ToPlaintext(string text) => ToTreeFormat(text, plainTextTranslations);

        public static string ToUnity(string text) => ToTreeFormat(text, unityTranslations);

        public static string ToRustLegacy(string text) => ToTreeFormat(text, rustLegacyTranslations);

        public static string ToRoKAnd7DTD(string text) => ToTreeFormat(text, rokAnd7DTDTranslations);

        public static string ToTerraria(string text) => ToTreeFormat(text, terrariaTranslations);

        public abstract class Poolable<T> where T : Poolable<T>, new()
        {
            private static readonly Stack<T> _pool = new Stack<T>();
            private static readonly object _poolLock = new object();

            protected bool isFromPool { get; private set; }

            public static T TakeFromPool()
            {
                T item;
                lock (_poolLock)
                {
                    item = _pool.Count > 0 ? _pool.Pop() : new T();
                }
                item.isFromPool = true;
                return item;
            }

            public static void ReturnToPool(T obj)
            {
                if (obj == null || !obj.isFromPool)
                    return;

                obj.Reset();
                lock (_poolLock)
                {
                    _pool.Push(obj);
                }
            }

            public static void ReturnToPool(List<T> objs)
            {
                if (objs == null)
                    return;

                for (int i = 0; i < objs.Count; i++)
                    ReturnToPool(objs[i]);

                objs.Clear();
            }

            protected abstract void Reset();
        }
    }
}
