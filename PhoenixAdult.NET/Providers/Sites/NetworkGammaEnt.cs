using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using PhoenixAdultNET.Providers.Helpers;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class NetworkGammaEnt : IPhoenixAdultNETProviderBase
    {
        public static async Task<string> GetAPIKey(string url, CancellationToken cancellationToken)
        {
            var http = await url.GetAsync(cancellationToken).ConfigureAwait(false);
            var regEx = Regex.Match(await http.Content.ReadAsStringAsync().ConfigureAwait(false), "\"apiKey\":\"(.*?)\"");
            if (regEx.Groups.Count > 0)
                return regEx.Groups[1].Value;

            return string.Empty;
        }

        public static async Task<JObject> GetDataFromAPI(string url, string indexName, string referer, string searchParams, CancellationToken cancellationToken)
        {
            var param = $"{{'requests':[{{'indexName':'{indexName}','params':'{searchParams}'}}]}}".Replace('\'', '"');
            var headers = new Dictionary<string, string>
            {
                {"Content-Type", "application/json" },
                {"Referer",  referer},
            };

            var http = await url.AllowAnyHttpStatus().WithHeaders(headers).PostStringAsync(param, cancellationToken).ConfigureAwait(false);
            var json = JObject.Parse(await http.Content.ReadAsStringAsync().ConfigureAwait(false));

            return json;
        }

        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var searchSceneID = searchTitle.Split()[0];
            if (!int.TryParse(searchSceneID, out _))
                searchSceneID = null;

            string apiKEY = await GetAPIKey(PhoenixAdultNETHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false),
                   searchParams;

            var sceneTypes = new List<string> { "scenes", "movies" };
            foreach (var sceneType in sceneTypes)
            {
                if (!string.IsNullOrEmpty(searchSceneID))
                    if (sceneType == "scenes")
                        searchParams = $"filters=clip_id={searchSceneID}";
                    else
                        searchParams = $"filters=movie_id={searchSceneID}";
                else
                    searchParams = $"query={searchTitle}";

                var url = $"{PhoenixAdultNETHelper.GetSearchSearchURL(siteNum)}?x-algolia-application-id=TSMKFA364Q&x-algolia-api-key={apiKEY}";
                var searchResults = await GetDataFromAPI(url, $"all_{sceneType}", PhoenixAdultNETHelper.GetSearchBaseURL(siteNum), searchParams, cancellationToken).ConfigureAwait(false);

                foreach (var searchResult in searchResults["results"].First["hits"])
                {
                    string sceneID,
                            curID,
                            sceneName = (string)searchResult["title"];
                    DateTime sceneDateObj;

                    if (sceneType == "scenes")
                    {
                        sceneDateObj = (DateTime)searchResult["release_date"];
                        sceneID = (string)searchResult["clip_id"];
                    }
                    else
                    {
                        var dateField = searchResult["last_modified"] != null ? "last_modified" : "date_created";
                        sceneDateObj = (DateTime)searchResult[dateField];
                        sceneID = (string)searchResult["movie_id"];
                    }
                    var sceneDate = sceneDateObj.ToString("yyyy-MM-dd", PhoenixAdultNETProvider.Lang);

                    var image = (string)searchResult["pictures"].Last(item => !item.ToString().Contains("resized", StringComparison.OrdinalIgnoreCase));

                    curID = $"{siteNum[0]}#{siteNum[1]}#{sceneType}#{sceneID}#{sceneDate}";
                    var res = new SceneSearch
                    {
                        CurID = curID,
                        Title = sceneName,
                        Poster = $"https://images-fame.gammacdn.com/movies/{image}",
                        ReleaseDate = sceneDateObj
                    };

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

            string apiKEY = await GetAPIKey(PhoenixAdultNETHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false),
                   sceneType = sceneID[2] == "scenes" ? "clip_id" : "movie_id",
                   url = $"{PhoenixAdultNETHelper.GetSearchSearchURL(siteNum)}?x-algolia-application-id=TSMKFA364Q&x-algolia-api-key={apiKEY}";
            var sceneData = await GetDataFromAPI(url, $"all_{sceneID[2]}", PhoenixAdultNETHelper.GetSearchBaseURL(siteNum), $"filters={sceneType}={sceneID[3]}", cancellationToken).ConfigureAwait(false);
            sceneData = (JObject)sceneData["results"].First["hits"].First;

            result.Title = (string)sceneData["title"];
            var description = (string)sceneData["description"];
            result.Description = description.Replace("</br>", "\n", StringComparison.OrdinalIgnoreCase);
            result.Studios.Add(PhoenixAdultNETProvider.Lang.TextInfo.ToTitleCase((string)sceneData["network_name"]));

            if (DateTime.TryParseExact(sceneID[4], "yyyy-MM-dd", PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                result.ReleaseDate = sceneDateObj;

            foreach (var genreLink in sceneData["categories"])
            {
                var genreName = (string)genreLink["name"];

                if (!string.IsNullOrEmpty(genreName))
                    result.Genres.Add(genreName);
            }

            foreach (var actorLink in sceneData["actors"])
            {
                string actorName = (string)actorLink["name"],
                       actorPhotoURL = string.Empty;

                var data = await GetDataFromAPI(url, "all_actors", PhoenixAdultNETHelper.GetSearchBaseURL(siteNum), $"filters=actor_id={actorLink["actor_id"]}", cancellationToken).ConfigureAwait(false);
                var actorData = data["results"].First["hits"].First;
                if (actorData["pictures"] != null)
                    actorPhotoURL = (string)actorData["pictures"].Last;

                var actor = new Actor
                {
                    Name = actorName
                };

                if (actorPhotoURL != null)
                    actor.Photo = $"https://images-fame.gammacdn.com/actors{actorPhotoURL}";

                result.Actors.Add(actor);
            }

            var ignore = false;
            var siteList = new List<string>
            {
                "girlsway.com", "puretaboo.com"
            };
            foreach (var site in siteList)
                if (PhoenixAdultNETHelper.GetSearchBaseURL(siteNum).EndsWith(site, StringComparison.OrdinalIgnoreCase))
                {
                    ignore = true;
                    break;
                }

            string image = sceneData["url_title"].ToString().ToLower(PhoenixAdultNETProvider.Lang).Replace('-', '_'),
                   imageURL = $"https://images-fame.gammacdn.com/movies/{sceneData["movie_id"]}/{sceneData["movie_id"]}_{image}_front_400x625.jpg";

            if (!ignore)
                result.Posters.Add(imageURL);

            image = (string)sceneData["pictures"].Last(item => !item.ToString().Contains("resized", StringComparison.OrdinalIgnoreCase));
            imageURL = $"https://images-fame.gammacdn.com/movies/{image}";
            result.Posters.Add(imageURL);
            result.Backgrounds.Add(imageURL);

            return result;
        }
    }
}
