using Flurl.Http;
using PhoenixAdultNET.Providers.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class SiteBrazzers : IPhoenixAdultNETProviderBase
    {
        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var sceneID = searchTitle.Split()[0];
            if (int.TryParse(sceneID, out _))
            {
                string sceneURL = $"{PhoenixAdultNETHelper.GetSearchBaseURL(siteNum)}/scenes/view/id/{sceneID}",
                       curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultNETHelper.Encode(sceneURL)}";

                var sceneData = await Update(curID.Split('#'), cancellationToken).ConfigureAwait(false);

                result.Add(new SceneSearch
                {
                    CurID = curID,
                    Title = sceneData.Title
                });
            }
            else
            {
                var url = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum);
                var http = await url.WithHeader("Cookie", $"textSearch={encodedTitle}").GetAsync(cancellationToken).ConfigureAwait(false);
                var stream = await http.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var data = HTML.ElementFromStream(stream);

                var searchResults = data.SelectNodes("//div[@class='release-card-wrap']");
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = PhoenixAdultNETHelper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//div[@class='scene-card-info']//a[1]").Attributes["href"].Value,
                            curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultNETHelper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//div[@class='scene-card-info']//a[1]").Attributes["title"].Value,
                            scenePoster = $"https:{searchResult.SelectSingleNode(".//img[contains(@class, 'card-main-img')]").Attributes["data-src"].Value}",
                            sceneDate = searchResult.SelectSingleNode(".//time").InnerText.Trim();

                    var res = new SceneSearch
                    {
                        CurID = curID,
                        Title = sceneName,
                        Poster = scenePoster
                    };

                    if (DateTime.TryParse(sceneDate, out DateTime sceneDateObj))
                        res.ReleaseDate = sceneDateObj;

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

            var sceneURL = PhoenixAdultNETHelper.Decode(sceneID[2]);
            var sceneData = (await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false)).SelectSingleNode("//p[@itemprop='description']");

            result.Title = sceneData.SelectSingleNode("//h1").InnerText;
            result.Description = sceneData.SelectSingleNode("//p[@itemprop='description']/text()").InnerText.Trim();
            result.Studios.Add("Brazzers");

            var dateNode = sceneData.SelectSingleNode("//aside[contains(@class, 'scene-date')]");
            if (dateNode != null)
                if (DateTime.TryParse(dateNode.InnerText, out DateTime sceneDateObj))
                    result.ReleaseDate = sceneDateObj;

            var genreNode = sceneData.SelectNodes("//div[contains(@class, 'tag-card-container')]//a");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText;

                    result.Genres.Add(genreName);
                }

            var actorsNode = sceneData.SelectNodes("//div[@class='model-card']");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.SelectSingleNode(".//h2[@class='model-card-title']//a").Attributes["title"].Value,
                           actorPhoto = $"https:{actorLink.SelectSingleNode(".//div[@class='card-image']//img").Attributes["data-src"].Value}";

                    result.Actors.Add(new Actor
                    {
                        Name = actorName,
                        Photo = actorPhoto
                    });
                }

            foreach (var sceneImages in sceneData.SelectNodes("//*[@id='trailer-player']/img"))
                result.Posters.Add($"https:{sceneImages.Attributes["src"].Value}");

            foreach (var sceneImages in sceneData.SelectNodes("//a[@rel='preview']"))
                result.Backgrounds.Add($"https:{sceneImages.Attributes["href"].Value}");

            return result;
        }
    }
}
