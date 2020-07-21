using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;
using PhoenixAdultNET.Providers.Helpers;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class NetworkKink : IPhoenixAdultNETProviderBase
    {
        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null)
                return result;

            var sceneID = searchTitle.Split()[0];
            if (int.TryParse(sceneID, out _))
            {
                string sceneURL = $"{PhoenixAdultNETHelper.GetSearchBaseURL(siteNum)}/shoot/{sceneID}",
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
                var url = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + encodedTitle;
                var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

                var searchResults = data.SelectNodes("//div[@class='shoot-card scene']");
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = PhoenixAdultNETHelper.GetSearchBaseURL(siteNum) + searchResult.SelectSingleNode(".//a[@class='shoot-link']").Attributes["href"].Value,
                            curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultNETHelper.Encode(sceneURL)}",
                            sceneName = searchResult.SelectSingleNode(".//img").Attributes["alt"].Value.Trim(),
                            scenePoster = searchResult.SelectSingleNode(".//img").Attributes["src"].Value,
                            sceneDate = searchResult.SelectSingleNode(".//div[@class='date']").InnerText.Trim();

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
            var http = await sceneURL.WithCookie("viewing-preferences", "straight%2Cgay").GetAsync(cancellationToken).ConfigureAwait(false);
            var stream = await http.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var sceneData = HTML.ElementFromStream(stream);

            result.Title = sceneData.SelectSingleNode("//h1[@class='shoot-title']").GetDirectInnerText().Trim();
            result.Description = sceneData.SelectNodes("//div[@class='description']")[1].InnerText.Replace("Description:", "", StringComparison.OrdinalIgnoreCase).Trim();
            result.Studios.Add("Kink");

            var sceneDate = sceneData.SelectSingleNode("//span[@class='shoot-date']").InnerText.Trim();
            if (DateTime.TryParseExact(sceneDate, "MMMM d, yyyy", PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                result.ReleaseDate = sceneDateObj;

            foreach (var genreLink in sceneData.SelectNodes("//p[@class='tag-list category-tag-list']//a"))
            {
                var genreName = genreLink.InnerText.Replace(",", "", StringComparison.OrdinalIgnoreCase).Trim();

                result.Genres.Add(genreName);
            }

            var actors = sceneData.SelectNodes("//p[@class='starring']//a");
            if (actors != null)
                foreach (var actorLink in actors)
                {
                    string actorName = actorLink.InnerText.Replace(",", "", StringComparison.OrdinalIgnoreCase).Trim(),
                           actorPageURL = PhoenixAdultNETHelper.GetSearchBaseURL(siteNum) + actorLink.Attributes["href"].Value,
                           actorPhoto;

                    http = await actorPageURL.GetAsync(cancellationToken).ConfigureAwait(false);
                    var actorHTML = new HtmlDocument();
                    actorHTML.Load(await http.Content.ReadAsStreamAsync().ConfigureAwait(false));
                    actorPhoto = actorHTML.DocumentNode.SelectSingleNode("//div[contains(@class, 'biography-container')]//img").Attributes["src"].Value;

                    result.Actors.Add(new Actor
                    {
                        Name = actorName,
                        Photo = actorPhoto
                    });
                }

            var sceneImages = sceneData.SelectNodes("//video");
            if (sceneImages != null)
                foreach (var sceneImage in sceneImages)
                    result.Posters.Add(sceneImage.Attributes["poster"].Value);

            sceneImages = sceneData.SelectNodes("//div[@class='player']//img");
            if (sceneImages != null)
                foreach (var sceneImage in sceneImages)
                {
                    result.Posters.Add(sceneImage.Attributes["src"].Value);
                    result.Backgrounds.Add(sceneImage.Attributes["src"].Value);
                }

            sceneImages = sceneData.SelectNodes("//div[@id='previewImages']//img");
            if (sceneImages != null)
                foreach (var sceneImage in sceneImages)
                {
                    result.Posters.Add(sceneImage.Attributes["data-image-file"].Value);
                    result.Backgrounds.Add(sceneImage.Attributes["data-image-file"].Value);
                }

            return result;
        }
    }
}
