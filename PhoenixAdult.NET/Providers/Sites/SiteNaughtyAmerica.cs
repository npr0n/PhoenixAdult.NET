using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using PhoenixAdultNET.Providers.Helpers;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class SiteNaughtyAmerica : IPhoenixAdultNETProviderBase
    {
        public static async Task<JObject> GetDataFromAPI(string url, string searchData, CancellationToken cancellationToken)
        {
            var param = $"{{'requests':[{{'indexName':'nacms_scenes_production','params':'{searchData}&hitsPerPage=100'}}]}}".Replace('\'', '"');
            var headers = new Dictionary<string, string>
            {
                {"Content-Type", "application/json" }
            };

            var http = await url.WithHeaders(headers).PostStringAsync(param, cancellationToken).ConfigureAwait(false);
            var json = JObject.Parse(await http.Content.ReadAsStringAsync().ConfigureAwait(false));

            return json;
        }

        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null)
                return result;

            JObject searchResults;
            var searchSceneID = searchTitle.Split()[0];
            string searchParams;
            if (int.TryParse(searchSceneID, out _))
                searchParams = $"filters=id={searchSceneID}";
            else
                searchParams = $"query={searchTitle}";
            var url = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + "?x-algolia-application-id=I6P9Q9R18E&x-algolia-api-key=08396b1791d619478a55687b4deb48b4";
            searchResults = await GetDataFromAPI(url, searchParams, cancellationToken).ConfigureAwait(false);

            foreach (var searchResult in searchResults["results"].First["hits"])
            {
                string sceneID = (string)searchResult["id"],
                        curID = $"{siteNum[0]}#{siteNum[1]}#{sceneID}",
                        sceneName = (string)searchResult["title"];
                long sceneDate = (long)searchResult["published_at"];

                result.Add(new SceneSearch
                {
                    CurID = curID,
                    Title = sceneName,
                    ReleaseDate = DateTimeOffset.FromUnixTimeSeconds(sceneDate).DateTime
                });
            }

            return result;
        }

        public async Task<Scene> Update(string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new Scene();
            if (sceneID == null)
                return null;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultNETProvider.Lang), int.Parse(sceneID[1], PhoenixAdultNETProvider.Lang) };

            var url = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + "?x-algolia-application-id=I6P9Q9R18E&x-algolia-api-key=08396b1791d619478a55687b4deb48b4";
            var sceneData = await GetDataFromAPI(url, $"filters=id={sceneID[2]}", cancellationToken).ConfigureAwait(false);
            sceneData = (JObject)sceneData["results"].First["hits"].First;

            result.Title = (string)sceneData["title"];
            result.Description = (string)sceneData["synopsis"];
            result.Studios.Add("Naughty America");

            DateTimeOffset sceneDateObj = DateTimeOffset.FromUnixTimeSeconds((long)sceneData["published_at"]);
            result.ReleaseDate = sceneDateObj.DateTime;

            foreach (var genreLink in sceneData["fantasies"])
            {
                var genreName = (string)genreLink;

                result.Genres.Add(genreName);
            }

            foreach (var actorLink in sceneData["performers"])
            {
                string actorName = (string)actorLink,
                        actorPhoto = string.Empty,
                        actorsPageURL;

                actorsPageURL = actorName.ToLower(PhoenixAdultNETProvider.Lang).Replace(" ", "-", StringComparison.OrdinalIgnoreCase).Replace("'", string.Empty, StringComparison.OrdinalIgnoreCase);

                var actorURL = $"https://www.naughtyamerica.com/pornstar/{actorsPageURL}";
                var actorData = await HTML.ElementFromURL(actorURL, cancellationToken).ConfigureAwait(false);

                var actorImageNode = actorData.SelectSingleNode("//img[@class='performer-pic']");
                if (actorImageNode != null)
                    actorPhoto = actorImageNode.Attributes["src"]?.Value;

                var actor = new Actor
                {
                    Name = actorName
                };
                if (!string.IsNullOrEmpty(actorPhoto))
                    actor.Photo = $"https:{actorPhoto}";

                result.Actors.Add(actor);
            }

            var sceneURL = $"https://www.naughtyamerica.com/scene/0{sceneID[2]}";
            var sceneDataHTML = await HTML.ElementFromURL(sceneURL, cancellationToken).ConfigureAwait(false);

            foreach (var sceneImages in sceneDataHTML.SelectNodes("//div[contains(@class, 'contain-scene-images') and contains(@class, 'desktop-only')]/a"))
            {
                var image = $"https:{sceneImages.Attributes["href"].Value}";
                result.Posters.Add(image);
                result.Backgrounds.Add(image);
            }

            return result;
        }
    }
}
