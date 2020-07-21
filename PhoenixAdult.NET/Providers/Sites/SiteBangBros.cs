using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PhoenixAdultNET.Providers.Helpers;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class SiteBangBros : IPhoenixAdultNETProviderBase
    {
        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var url = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + searchTitle.Replace(" ", "-", StringComparison.OrdinalIgnoreCase);
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//div[contains(@class, 'elipsTxt')]//div[@class='echThumb']");
            foreach (var searchResult in searchResults)
            {
                string sceneURL = PhoenixAdultNETHelper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//a[contains(@href, '/video')]").Attributes["href"].Value,
                        curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultNETHelper.Encode(sceneURL)}",
                        sceneName = searchResult.SelectSingleNode(".//span[@class='thmb_ttl']").InnerText.Trim(),
                        scenePoster = $"https:{searchResult.SelectSingleNode(".//img").Attributes["data-src"].Value}",
                        sceneDate = searchResult.SelectSingleNode(".//span[contains(@class, 'thmb_mr_2')]").InnerText.Trim();

                var res = new SceneSearch
                {
                    CurID = curID,
                    Title = sceneName,
                    Poster = scenePoster
                };

                if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                    res.ReleaseDate = sceneDateObj;

                result.Add(res);
            }

            return result;
        }

        public async Task<Scene> Update(string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new Scene();
            if (sceneID == null)
                return null;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultNETProvider.Lang), int.Parse(sceneID[1], PhoenixAdultNETProvider.Lang) };

            var sceneURL = PhoenixAdultNETHelper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Title = sceneData.SelectSingleNode("//h1").InnerText;
            result.Description = sceneData.SelectSingleNode("//div[@class='vdoDesc']").InnerText.Trim();
            result.Studios.Add("Bang Bros");

            var dateNode = sceneData.SelectSingleNode("//span[contains(@class, 'thmb_mr_2')]");
            if (dateNode != null)
                if (DateTime.TryParseExact(dateNode.InnerText.Trim(), "MMM d, yyyy", PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                    result.ReleaseDate = sceneDateObj;

            var genreNode = sceneData.SelectNodes("//div[contains(@class, 'vdoTags')]//a");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText.Trim();

                    result.Genres.Add(genreName);
                }

            var actorsNode = sceneData.SelectNodes("//div[@class='vdoCast']//a[contains(@href, '/model')]");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText.Trim(),
                           actorPageURL = PhoenixAdultNETHelper.GetSearchBaseURL(siteNum) + actorLink.Attributes["href"].Value,
                           actorPhoto;

                    var actorHTML = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);
                    actorPhoto = $"https:{actorHTML.SelectSingleNode("//div[@class='profilePic_in']//img").Attributes["src"].Value}";

                    result.Actors.Add(new Actor
                    {
                        Name = actorName,
                        Photo = actorPhoto
                    });
                }

            var imgNode = sceneData.SelectNodes("//img[contains(@id, 'player-overlay-image')]");
            if (imgNode != null)
                foreach (var sceneImages in imgNode)
                    result.Posters.Add($"https:{sceneImages.Attributes["src"].Value}");

            imgNode = sceneData.SelectNodes("//div[@id='img-slider']//img");
            if (imgNode != null)
                foreach (var sceneImages in imgNode)
                    result.Backgrounds.Add($"https:{sceneImages.Attributes["src"].Value}");

            return result;
        }
    }
}
