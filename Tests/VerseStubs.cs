using System.Collections.Generic;

namespace Verse
{
    public struct TaggedString
    {
        public string Value;
        public static implicit operator string(TaggedString ts) => ts.Value;
        public static implicit operator TaggedString(string s) => new TaggedString { Value = s };
        public override string ToString() => Value ?? "";
    }

    public interface IExposable
    {
        void ExposeData();
    }

    public static class Log
    {
        public static void Warning(string msg) { }
        public static void Message(string msg) { }
        public static void Error(string msg) { }
    }

    public static class Extensions
    {
        public static bool NullOrEmpty(this string? s) => string.IsNullOrEmpty(s);
    }
}
