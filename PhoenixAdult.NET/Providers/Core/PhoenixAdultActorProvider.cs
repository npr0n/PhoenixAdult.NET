using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Flurl.Http;
using PhoenixAdultNET.Providers.Helpers;

namespace PhoenixAdultNET.Providers
{
    public static class PhoenixAdultNETActorProvider
    {
        public static async Task<Dictionary<string, string>> GetActorPhotos(string name, CancellationToken cancellationToken)
        {
            string image;

            var imageList = new Dictionary<string, string>();

            image = await GetFromAdultDVDEmpire(name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                imageList.Add("AdultDVDEmpire", image);

            image = await GetFromBoobpedia(name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                imageList.Add("Boobpedia", image);

            image = await GetFromBabepedia(name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                imageList.Add("Babepedia", image);

            image = await GetFromIAFD(name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                imageList.Add("IAFD", image);

            return imageList;
        }

        private static async Task<string> GetFromAdultDVDEmpire(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"https://www.adultdvdempire.com/performer/search?q={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorNode = actorData.SelectSingleNode("//div[@id='performerlist']/div//a");
            if (actorNode != null) {
                var actorPageURL = "https://www.adultdvdempire.com" + actorNode.Attributes["href"].Value;
                var actorPage = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);

                var img = actorPage.SelectSingleNode("//div[contains(@class, 'performer-image-container')]/a");
                if (img != null)
                    image = img.Attributes["href"].Value;
            }

            return image;
        }

        private static async Task<string> GetFromBoobpedia(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"http://www.boobpedia.com/wiki/index.php?search={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorImageNode = actorData.SelectSingleNode("//table[@class='infobox']//a[@class='image']//img");
            if (actorImageNode != null) {
                var img = actorImageNode.Attributes["src"].Value;
                if (!img.Contains("NoImage", StringComparison.OrdinalIgnoreCase))
                    image = "http://www.boobpedia.com" + actorImageNode.Attributes["src"].Value;
            }

            return image;
        }

        private static async Task<string> GetFromBabepedia(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string encodedName = name.Replace(" ", "_", StringComparison.OrdinalIgnoreCase),
                   url = $"https://www.babepedia.com/babe/{encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorImageNode = actorData.SelectSingleNode("//div[@id='profimg']/a");
            if (actorImageNode != null)
                image = "https://www.babepedia.com" + actorImageNode.Attributes["href"].Value;

            return image;
        }

        private static async Task<string> GetFromIAFD(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"http://www.iafd.com/results.asp?searchtype=comprehensive&searchstring={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorNode = actorData.SelectSingleNode("//table[@id='tblFem']//tbody//a");
            if (actorNode != null) {
                var actorPageURL = "http://www.iafd.com" + actorNode.Attributes["href"].Value;
                var actorPage = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);

                var actorImage = actorPage.SelectSingleNode("//div[@id='headshot']//img").Attributes["src"].Value;
                if (!actorImage.Contains("nophoto", StringComparison.OrdinalIgnoreCase))
                    image = actorImage;
            }

            return image;
        }
    }
}
