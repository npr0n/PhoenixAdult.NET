using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using PhoenixAdultNET.Providers.Helpers;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class NetworkFemdomEmpire : IPhoenixAdultNETProviderBase
    {
        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null)
                return result;

            var url = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + encodedTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//div[contains(@class, 'item') and contains(@class, 'hover')]");
            if (searchResults != null)
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = searchResult.SelectSingleNode(".//a").Attributes["href"].Value,
                            curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultNETHelper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode("///div[contains(@class, 'item-info')]//a").InnerText.Trim(),
                            sceneDate = searchResult.SelectSingleNode(".//span[@class='date']").InnerText.Trim(),
                            scenePoster = PhoenixAdultNETHelper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//img").Attributes["src0_1x"].Value;

                    var res = new SceneSearch
                    {
                        CurID = curID,
                        Title = sceneName,
                        Poster = scenePoster
                    };

                    if (DateTime.TryParseExact(sceneDate, "MMMM d, yyyy", PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
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

            result.Title = sceneData.SelectSingleNode("//div[contains(@class, 'videoDetails')]//h3").InnerText.Trim();
            var description = sceneData.SelectSingleNode("//div[contains(@class, 'videoDetails')]//p");
            if (description != null)
                result.Description = description.InnerText.Trim();
            result.Studios.Add("Femdom Empire");

            var dateNode = sceneData.SelectSingleNode("//div[contains(@class, 'videoInfo')]//p");
            if (dateNode != null)
            {
                var date = dateNode.InnerText.Replace("Date Added:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                if (DateTime.TryParseExact(date, "MMMM d, yyyy", PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                    result.ReleaseDate = sceneDateObj;
            }

            var genreNode = sceneData.SelectNodes("//div[contains(@class, 'featuring')][2]//ul//li");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText.Replace("categories:", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("tags:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

                    if (!string.IsNullOrEmpty(genreName))
                        result.Genres.Add(genreName);
                }
            result.Genres.Add("Femdom");

            var actorsNode = sceneData.SelectNodes("//div[contains(@class, 'featuring')][1]/ul/li");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText.Replace("Featuring:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

                    if (!string.IsNullOrEmpty(actorName))
                        result.Actors.Add(new Actor
                        {
                            Name = actorName
                        });
                }

            var img = sceneData.SelectSingleNode("//a[@class='fake_trailer']//img");
            if (img != null)
            {
                result.Posters.Add(PhoenixAdultNETHelper.GetSearchBaseURL(siteNum) + img.Attributes["src0_1x"].Value);
                result.Backgrounds.Add(PhoenixAdultNETHelper.GetSearchBaseURL(siteNum) + img.Attributes["src0_1x"].Value);
            }

            return result;
        }
    }
}
