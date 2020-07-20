using PhoenixAdultNET.Providers.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class NetworkDogfart : IPhoenixAdultNETProviderBase
    {
        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var url = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + encodedTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//a[contains(@class, 'thumbnail')]");
            if (searchResults != null)
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = PhoenixAdultNETHelper.GetSearchBaseURL(siteNum) + searchResult.Attributes["href"].Value.Split('?')[0],
                            curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultNETHelper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//div/h3[@class='scene-title']").InnerText,
                            posterURL = $"https:{searchResult.SelectSingleNode(".//img").Attributes["src"].Value}",
                            subSite = searchResult.SelectSingleNode(".//div/p[@class='help-block']").InnerText.Split(".com")[0];

                    var res = new SceneSearch
                    {
                        Title = $"{sceneName} from {subSite}",
                        Poster = posterURL
                    };

                    if (searchDate.HasValue)
                    {
                        res.ReleaseDate = searchDate;
                        curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", PhoenixAdultNETProvider.Lang)}";
                    }

                    res.CurID = curID;

                    if (subSite == PhoenixAdultNETHelper.GetSearchSiteName(siteNum))
                        res.IndexNumber = PhoenixAdultNETHelper.LevenshteinDistance(searchTitle, sceneName) - 100;
                    else
                        res.IndexNumber = PhoenixAdultNETHelper.LevenshteinDistance(searchTitle, sceneName) - 60;

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

            result.Title = sceneData.SelectSingleNode("//div[@class='icon-container']/a").Attributes["title"].Value;
            result.Description = sceneData.SelectSingleNode("//div[contains(@class, 'description')]").InnerText.Replace("...read more", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            result.Studios.Add("Dogfart Network");

            if (sceneID.Length > 3)
                if (DateTime.TryParseExact(sceneID[3], "yyyy-MM-dd", PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                    result.ReleaseDate = sceneDateObj;

            var genreNode = sceneData.SelectNodes("//div[@class='categories']/p/a");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText.Trim();

                    result.Genres.Add(genreName);
                }

            var actorsNode = sceneData.SelectNodes("//h4[@class='more-scenes']/a");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText.Trim();

                    result.Actors.Add(new Actor
                    {
                        Name = actorName
                    });
                }

            var poster = sceneData.SelectSingleNode("//div[@class='icon-container']//img");
            if (poster != null)
                result.Posters.Add($"https:{poster.Attributes["src"].Value}");

            var img = sceneData.SelectNodes("//div[contains(@class, 'preview-image-container')]//a");
            if (img != null)
                foreach (var sceneImages in img)
                {
                    var url = PhoenixAdultNETHelper.GetSearchBaseURL(siteNum) + sceneImages.Attributes["href"].Value;
                    var posterHTML = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                    var posterData = posterHTML.SelectSingleNode("//div[contains(@class, 'remove-bs-padding')]/img").Attributes["src"].Value;
                    result.Backgrounds.Add(posterData);
                }

            return result;
        }
    }
}
