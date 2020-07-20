using Flurl.Http;
using Newtonsoft.Json.Linq;
using PhoenixAdultNET.Providers.Helpers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class Network1service : IPhoenixAdultNETProviderBase
    {
        public static async Task<IDictionary<string, Cookie>> GetCookies(string url, CancellationToken cancellationToken)
        {
            IDictionary<string, Cookie> cookies;

            using (var http = new FlurlClient(url))
            {
                await http.EnableCookies().AllowAnyHttpStatus().Request().HeadAsync(cancellationToken).ConfigureAwait(false);
                cookies = http.Cookies;
            }

            return cookies;
        }

        public static async Task<JObject> GetDataFromAPI(string url, string instance, CancellationToken cancellationToken)
        {
            JObject json = null;

            var http = await url.AllowAnyHttpStatus().WithHeader("Instance", instance).GetAsync(cancellationToken).ConfigureAwait(false);
            if (http.IsSuccessStatusCode)
                json = JObject.Parse(await http.Content.ReadAsStringAsync().ConfigureAwait(false));

            return json;
        }

        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null)
                return result;

            var searchSceneID = searchTitle.Split()[0];
            var sceneTypes = new List<string> { "scene", "movie", "serie" };
            if (!int.TryParse(searchSceneID, out _))
                searchSceneID = null;

            var cookies = await GetCookies(PhoenixAdultNETHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false);
            if (!cookies.TryGetValue("instance_token", out Cookie cookie))
                return result;

            foreach (var sceneType in sceneTypes)
            {
                string url;
                if (string.IsNullOrEmpty(searchSceneID))
                    url = $"/v2/releases?type={sceneType}&search={searchTitle}";
                else
                    url = $"/v2/releases?type={sceneType}&id={searchSceneID}";

                var searchResults = await GetDataFromAPI(PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + url, cookie.Value, cancellationToken).ConfigureAwait(false);
                if (searchResults == null)
                    break;

                foreach (var searchResult in searchResults["result"])
                {
                    string sceneID = (string)searchResult["id"],
                            curID = $"{siteNum[0]}#{siteNum[1]}#{sceneID}#{sceneType}",
                            sceneName = (string)searchResult["title"];
                    DateTime sceneDateObj = (DateTime)searchResult["dateReleased"];

                    var res = new SceneSearch
                    {
                        CurID = curID,
                        Title = sceneName,
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

            var cookies = await GetCookies(PhoenixAdultNETHelper.GetSearchBaseURL(siteNum), cancellationToken).ConfigureAwait(false);
            if (!cookies.TryGetValue("instance_token", out Cookie cookie))
                return null;

            var url = $"{PhoenixAdultNETHelper.GetSearchSearchURL(siteNum)}/v2/releases?type={sceneID[3]}&id={sceneID[2]}";
            var sceneData = await GetDataFromAPI(url, cookie.Value, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
                return null;

            sceneData = (JObject)sceneData["result"].First;

            result.Title = (string)sceneData["title"];
            result.Description = (string)sceneData["description"];
            result.Studios.Add(PhoenixAdultNETProvider.Lang.TextInfo.ToTitleCase((string)sceneData["brand"]));

            DateTime sceneDateObj = (DateTime)sceneData["dateReleased"];
            result.ReleaseDate = sceneDateObj;

            foreach (var genreLink in sceneData["tags"])
            {
                var genreName = (string)genreLink["name"];

                result.Genres.Add(genreName);
            }

            foreach (var actorLink in sceneData["actors"])
            {
                var actorPageURL = $"{PhoenixAdultNETHelper.GetSearchSearchURL(siteNum)}/v1/actors?id={actorLink["id"]}";
                var actorData = await GetDataFromAPI(actorPageURL, cookie.Value, cancellationToken).ConfigureAwait(false);
                if (actorData != null)
                {
                    actorData = (JObject)actorData["result"].First;
                    var actor = new Actor
                    {
                        Name = (string)actorLink["name"]
                    };

                    if (actorData["images"] != null && actorData["images"].Type == JTokenType.Object)
                        actor.Photo = (string)actorData["images"]["profile"].First["xs"]["url"];

                    result.Actors.Add(actor);

                }
            }

            var imageTypes = new List<string> { "poster", "cover" };
            foreach (var imageType in imageTypes)
                if (sceneData["images"][imageType] != null)
                    foreach (var image in sceneData["images"][imageType])
                    {
                        result.Posters.Add((string)image["xx"]["url"]);
                        result.Backgrounds.Add((string)image["xx"]["url"]);
                    }

            return result;
        }
    }
}
