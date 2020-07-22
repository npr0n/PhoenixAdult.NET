using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;

namespace PhoenixAdultNET.Providers.Helpers
{
    public static class HTML
    {
        public static string GetUserAgent() => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";

        public static async Task<HtmlNode> ElementFromURL(string url, CancellationToken cancellationToken)
        {
            var html = new HtmlDocument();
            var http = await url.AllowAnyHttpStatus().WithHeader("User-Agent", GetUserAgent()).GetAsync(cancellationToken).ConfigureAwait(false);
            if (http.IsSuccessStatusCode)
                html.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));

            return html.DocumentNode;
        }

        public static HtmlNode ElementFromString(string data)
        {
            var html = new HtmlDocument();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            html.Load(stream);
            stream.Dispose();

            return html.DocumentNode;
        }

        public static HtmlNode ElementFromStream(Stream data)
        {
            var html = new HtmlDocument();
            html.Load(data);

            return html.DocumentNode;
        }
    }
}

public static class StringExtensions
{
    public static bool Contains(this string source, string toCheck, StringComparison stringComparison) => source?.IndexOf(toCheck, stringComparison) >= 0;

    public static string Replace(this string source, string from, string to, int nums, StringComparison stringComparison)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(from))
            return source;

        for (var i = 0; i < nums; i++)
        {
            int pos = source.IndexOf(from, stringComparison);
            if (pos < 0)
                return source;

            source = source.Substring(0, pos) + to + source.Substring(pos + from.Length);
        }

        return source;
    }

    public static string Replace(this string source, string from, string to, StringComparison stringComparison)
    {
        if (stringComparison == StringComparison.OrdinalIgnoreCase)
            return Regex.Replace(source, Regex.Escape(from), to, RegexOptions.IgnoreCase);
        else
            return source?.Replace(from, to, stringComparison);
    }
}

public static class Base58
{
    private const string DIGITS = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    public static string EncodePlain(byte[] data)
    {
        if (data == null)
            return null;

        var intData = data.Aggregate<byte, BigInteger>(0, (current, t) => current * 256 + t);

        var result = string.Empty;
        while (intData > 0)
        {
            var remainder = (int)(intData % 58);
            intData /= 58;
            result = DIGITS[remainder] + result;
        }

        for (var i = 0; i < data.Length && data[i] == 0; i++)
            result = '1' + result;

        return result;
    }

    public static byte[] DecodePlain(string data)
    {
        if (data == null)
            return null;

        BigInteger intData = 0;
        for (var i = 0; i < data.Length; i++)
        {
            var digit = DIGITS.IndexOf(data[i]);

            if (digit < 0)
                throw new FormatException($"Invalid Base58 character `{data[i]}` at position {i}");

            intData = intData * 58 + digit;
        }

        var leadingZeroCount = data.TakeWhile(c => c == '1').Count();
        var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
        var bytesWithoutLeadingZeros = intData.ToByteArray().Reverse().SkipWhile(b => b == 0);
        var result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();

        return result;
    }
}

public static class LevenshteinDistance
{
    public static int Calculate(string source1, string source2)
    {
        if (source1 == null || source2 == null)
            return -1;

        var source1Length = source1.Length;
        var source2Length = source2.Length;

        var matrix = new int[source1Length + 1, source2Length + 1];

        if (source1Length == 0)
            return source2Length;

        if (source2Length == 0)
            return source1Length;

        for (var i = 0; i <= source1Length; matrix[i, 0] = i++) { }
        for (var j = 0; j <= source2Length; matrix[0, j] = j++) { }

        for (var i = 1; i <= source1Length; i++)
        {
            for (var j = 1; j <= source2Length; j++)
            {
                var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source1Length, source2Length];
    }
}
