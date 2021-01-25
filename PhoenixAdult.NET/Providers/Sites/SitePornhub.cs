using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PhoenixAdultNET.Providers.Helpers;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class SitePornhub : IPhoenixAdultNETProviderBase
    {
        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            searchTitle = searchTitle.Replace(" ", "+", StringComparison.OrdinalIgnoreCase);
            var url = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + searchTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//ul[@id='videoSearchResult']/li[@_vkey]");
            foreach (var searchResult in searchResults)
            {
                var sceneURL = new Uri(PhoenixAdultNETHelper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//a/@href"));
                string curID = PhoenixAdultNETHelper.Encode(sceneURL.PathAndQuery),
                    sceneName = searchResult.SelectSingleNode(".//span[@class='title']").InnerText,
                    scenePoster = searchResult.SelectSingleNode(".//div[@class='phimage']//img/@thumb_url").InnerText;

                var res = new SceneSearch
                {
                    CurID = curID,
                    Title = sceneName,
                    Poster = scenePoster,
                };

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
            var sceneDataJSON = JObject.Parse(sceneData.SelectSingleNode("//script[@type='application/ld+json']").InnerText);

            result.Title = sceneData.SelectSingleNode("//h1[@class='title']").InnerText;
            var studioName = sceneData.SelectSingleNode("//div[@class='userInfo']//a").InnerText;
            result.Studios.Add(studioName);

            var date = (string)sceneDataJSON["uploadDate"];
            if (date != null)
            {
                if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    result.ReleaseDate = sceneDateObj;
                }
            }

            var genreNode = sceneData.SelectNodes("(//div[@class='categoriesWrapper'] | //div[@class='tagsWrapper'])/a");
            foreach (var genreLink in genreNode)
            {
                var genreName = genreLink.InnerText;

                result.Genres.Add(genreName);
            }

            var actorsNode = sceneData.SelectNodes("//div[contains(@class, 'pornstarsWrapper')]/a");
            foreach (var actorLink in actorsNode)
            {
                string actorName = actorLink.Attributes["data-mxptext"].Value,
                        actorPhotoURL = actorLink.SelectSingleNode(".//img[@class='avatar']/@src").InnerText;

                result.Actors.Add(new Actor
                {
                    Name = actorName,
                    Photo = actorPhotoURL,
                });
            }

            return result;
        }

    }
}
