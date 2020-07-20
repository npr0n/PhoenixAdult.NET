using Flurl.Http;
using HtmlAgilityPack;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
