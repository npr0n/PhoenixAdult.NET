using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixAdultNET.Providers.Helpers;

namespace PhoenixAdultNET.Providers.Sites
{
    class SiteLegalPorno : IPhoenixAdultNETProviderBase
    {
        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var url = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + encodedTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            if (!data.SelectSingleNode("//title").InnerText.Contains("Search for", StringComparison.OrdinalIgnoreCase))
            {
                string sceneURL = data.SelectSingleNode("//div[@class='user--guest']//a").Attributes["href"].Value,
                       curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultNETHelper.Encode(sceneURL)}";

                var sceneData = await Update(curID.Split('#'), cancellationToken).ConfigureAwait(false);

                result.Add(new SceneSearch
                {
                    CurID = curID,
                    Title = sceneData.Title,
                    Poster = sceneData.Posters.First(),
                    ReleaseDate = sceneData.ReleaseDate
                });
            }
            else
            {
                var searchResults = data.SelectNodes("//div[@class='thumbnails']/div");
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = searchResult.SelectSingleNode(".//a").Attributes["href"].Value,
                            curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultNETHelper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//div[contains(@class, 'thumbnail-title')]//a").InnerText.Trim(),
                            scenePoster = string.Empty,
                            sceneDate = searchResult.SelectSingleNode(".").Attributes["release"].Value;

                    var res = new SceneSearch
                    {
                        CurID = curID,
                        Title = sceneName
                    };

                    if (DateTime.TryParseExact(sceneDate, "yyyy/MM/dd", PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                        res.ReleaseDate = sceneDateObj;

                    var scenePosterNode = searchResult.SelectSingleNode(".//div[@class='thumbnail-image']/a");
                    if (scenePosterNode != null && scenePosterNode.Attributes.Contains("style"))
                        scenePoster = scenePosterNode.Attributes["style"].Value.Split('(')[1].Split(')')[0];

                    if (!string.IsNullOrEmpty(scenePoster))
                        res.Poster = scenePoster;

                    result.Add(res);
                }
            }

            return result;
        }

        public async Task<Scene> Update(string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new Scene();
            if (sceneID == null)
                return null;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultNETProvider.Lang), int.Parse(sceneID[1], PhoenixAdultNETProvider.Lang) };

            string sceneURL = PhoenixAdultNETHelper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Title = sceneData.SelectSingleNode("//h1[@class='watchpage-title']").InnerText.Trim();
            result.Studios.Add("LegalPorno");

            var sceneDate = sceneData.SelectSingleNode("//span[@class='scene-description__detail']//a").InnerText.Trim();
            if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                result.ReleaseDate = sceneDateObj;

            var genreNode = sceneData.SelectNodes("//dd/a[contains(@href, '/niche/')]");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText;

                    result.Genres.Add(genreName);
                }

            var actorsNode = sceneData.SelectNodes("//dd/a[contains(@href, 'model') and not(contains(@href, 'forum'))]");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    var actor = new Actor
                    {
                        Name = actorLink.InnerText.Trim()
                    };

                    var actorPage = await HTML.ElementFromURL(actorLink.Attributes["href"].Value, cancellationToken).ConfigureAwait(false);
                    var actorPhotoNode = actorPage.SelectSingleNode("//div[@class='model--avatar']//img");
                    if (actorPhotoNode != null)
                        actor.Photo = actorPhotoNode.Attributes["src"].Value;

                    result.Actors.Add(actor);
                }

            var scenePoster = sceneData.SelectSingleNode("//div[@id='player']").Attributes["style"].Value.Split('(')[1].Split(')')[0];
            result.Posters.Add(scenePoster);

            var scenePosters = sceneData.SelectNodes("//div[contains(@class, 'thumbs2 gallery')]//img");
            if (scenePosters != null)
                foreach (var poster in scenePosters)
                {
                    scenePoster = poster.Attributes["src"].Value;
                    result.Posters.Add(scenePoster);
                    result.Backgrounds.Add(scenePoster);
                }

            return result;
        }
    }
}
