using Flurl.Http;
using Newtonsoft.Json.Linq;
using PhoenixAdultNET.Providers.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class NetworkBang : IPhoenixAdultNETProviderBase
    {
        public static async Task<JObject> GetDataFromAPI(string url, string searchTitle, string searchType, CancellationToken cancellationToken)
        {
            var param = $"{{'query':{{'bool':{{'must':[{{'match':{{'{searchType}':'{searchTitle}'}}}},{{'match':{{'type':'movie'}}}}],'must_not':[{{'match':{{'type':'trailer'}}}}]}}}}}}".Replace('\'', '"');
            var headers = new Dictionary<string, string>
            {
                {"Authorization", "Basic YmFuZy1yZWFkOktqVDN0RzJacmQ1TFNRazI=" },
                {"Content-Type", "application/json" }
            };

            var http = await url.WithHeaders(headers).PostStringAsync(param, cancellationToken).ConfigureAwait(false);
            var json = JObject.Parse(await http.Content.ReadAsStringAsync().ConfigureAwait(false));

            return json;
        }

        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            JObject searchResults;
            var searchSceneID = searchTitle.Split()[0];
            if (int.TryParse(searchSceneID, out _))
                searchResults = await GetDataFromAPI(PhoenixAdultNETHelper.GetSearchSearchURL(siteNum), searchSceneID, "identifier", cancellationToken).ConfigureAwait(false);
            else
                searchResults = await GetDataFromAPI(PhoenixAdultNETHelper.GetSearchSearchURL(siteNum), searchTitle, "name", cancellationToken).ConfigureAwait(false);

            foreach (var searchResult in searchResults["hits"]["hits"])
            {
                var sceneData = searchResult["_source"];
                string sceneID = (string)sceneData["identifier"],
                        curID = $"{siteNum[0]}#{siteNum[1]}#{sceneID}",
                        sceneName = (string)sceneData["name"],
                        scenePoster = $"https://i.bang.com/covers/{sceneData["dvd"]["id"]}/front.jpg";
                DateTime sceneDateObj = (DateTime)sceneData["releaseDate"];

                var item = new SceneSearch
                {
                    CurID = curID,
                    Title = sceneName,
                    Poster = scenePoster,
                    ReleaseDate = sceneDateObj
                };

                result.Add(item);
            }

            return result;
        }

        public async Task<Scene> Update(string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new Scene();
            if (sceneID == null)
                return null;

            int[] siteNum = new int[2] { int.Parse(sceneID[0], PhoenixAdultNETProvider.Lang), int.Parse(sceneID[1], PhoenixAdultNETProvider.Lang) };

            var sceneData = await GetDataFromAPI(PhoenixAdultNETHelper.GetSearchSearchURL(siteNum), sceneID[2], "identifier", cancellationToken).ConfigureAwait(false);
            sceneData = (JObject)sceneData["hits"]["hits"].First["_source"];

            result.Title = (string)sceneData["name"];
            result.Description = (string)sceneData["description"];
            result.Studios.Add(PhoenixAdultNETProvider.Lang.TextInfo.ToTitleCase((string)sceneData["studio"]["name"]));

            DateTime sceneDateObj = (DateTime)sceneData["releaseDate"];
            result.ReleaseDate = sceneDateObj;

            foreach (var genreLink in sceneData["genres"])
            {
                var genreName = (string)genreLink["name"];

                result.Genres.Add(genreName);
            }

            foreach (var actorLink in sceneData["actors"])
            {
                string actorName = (string)actorLink["name"],
                       actorPhoto = $"https://i.bang.com/pornstars/{actorLink["id"]}.jpg";

                result.Actors.Add(new Actor
                {
                    Name = actorName,
                    Photo = actorPhoto
                });
            }

            result.Posters.Add($"https://i.bang.com/covers/{sceneData["dvd"]["id"]}/front.jpg");

            foreach (var image in sceneData["screenshots"])
                result.Backgrounds.Add($"https://i.bang.com/screenshots/{sceneData["dvd"]["id"]}/movie/1/{image["screenId"]}.jpg");

            return result;
        }
    }
}
