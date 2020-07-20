using PhoenixAdultNET.Providers.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class NetworkR18 : IPhoenixAdultNETProviderBase
    {
        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null)
                return result;

            string searchJAVID = string.Empty;
            var sceneID = searchTitle.Split();
            if (int.TryParse(sceneID[1], out _))
                searchJAVID = $"{sceneID[0]}%20{sceneID[1]}";

            if (!string.IsNullOrEmpty(searchJAVID))
                encodedTitle = searchJAVID;

            var url = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + encodedTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodes("//li[contains(@class, 'item-list')]");
            if (searchResults != null)
                foreach (var searchResult in searchResults)
                {
                    string sceneURL = searchResult.SelectSingleNode(".//a").Attributes["href"].Value,
                            curID,
                            sceneName = searchResult.SelectSingleNode(".//dt").InnerText,
                            scenePoster = searchResult.SelectSingleNode(".//img").Attributes["data-original"].Value,
                            javID = searchResult.SelectSingleNode(".//img").Attributes["alt"].Value;

                    sceneURL = sceneURL.Replace("/" + sceneURL.Split('/').Last(), string.Empty, StringComparison.OrdinalIgnoreCase);
                    curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultNETHelper.Encode(sceneURL)}";

                    var res = new SceneSearch
                    {
                        CurID = curID,
                        Title = sceneName,
                        Poster = scenePoster
                    };

                    if (!string.IsNullOrEmpty(searchJAVID))
                        res.IndexNumber = PhoenixAdultNETHelper.LevenshteinDistance(searchJAVID, javID);

                    result.Add(res);
                }

            return result;
        }

        public async Task<Scene> Update(string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new Scene();
            if (sceneID == null)
                return null;

            var sceneURL = PhoenixAdultNETHelper.Decode(sceneID[2]);
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            result.Title = sceneData.SelectSingleNode("//cite[@itemprop='name']").InnerText;

            var description = sceneData.SelectSingleNode("//div[@class='cmn-box-description01']");
            if (description != null)
                result.Description = description.InnerText.Replace("Product Description", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            result.Studios.Add(sceneData.SelectSingleNode("//dd[@itemprop='productionCompany']").InnerText.Trim());

            var dateNode = sceneData.SelectSingleNode("//dd[@itemprop='dateCreated']");
            if (dateNode != null)
            {
                var date = dateNode.InnerText.Trim().Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(",", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("Sept", "Sep", StringComparison.OrdinalIgnoreCase).Replace("June", "Jun", StringComparison.OrdinalIgnoreCase).Replace("July", "Jul", StringComparison.OrdinalIgnoreCase);
                if (DateTime.TryParseExact(date, "MMM dd yyyy", PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                    result.ReleaseDate = sceneDateObj;
            }

            var genreNode = sceneData.SelectNodes("//a[@itemprop='genre']");
            if (genreNode != null)
                foreach (var genreLink in genreNode)
                {
                    var genreName = genreLink.InnerText.Trim().ToLower(PhoenixAdultNETProvider.Lang);

                    result.Genres.Add(genreName);
                }

            var actorsNode = sceneData.SelectNodes("//div[@itemprop='actors']//span[@itemprop='name']");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText.Trim();

                    if (actorName != "----")
                    {
                        actorName = actorName.Split('(')[0].Trim();

                        var actor = new Actor
                        {
                            Name = actorName
                        };

                        var photoXpath = string.Format(PhoenixAdultNETProvider.Lang, "//div[@id='{0}']//img[contains(@alt, '{1}')]", actorName.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase), actorName);
                        var actorPhoto = sceneData.SelectSingleNode(photoXpath).Attributes["src"].Value;

                        if (!actorPhoto.Contains("nowprinting.gif", StringComparison.OrdinalIgnoreCase))
                            actor.Photo = actorPhoto;

                        result.Actors.Add(actor);
                    }
                }

            var img = sceneData.SelectSingleNode("//img[contains(@alt, 'cover')]").Attributes["src"].Value;
            result.Posters.Add(img);

            foreach (var sceneImages in sceneData.SelectNodes("//section[@id='product-gallery']//img"))
                result.Backgrounds.Add(sceneImages.Attributes["data-src"].Value);

            return result;
        }
    }
}
