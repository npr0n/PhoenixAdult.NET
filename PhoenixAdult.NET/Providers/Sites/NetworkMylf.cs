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
    internal class NetworkMylf : IPhoenixAdultNETProviderBase
    {
        public static async Task<JObject> GetJSONfromPage(string url, CancellationToken cancellationToken)
        {
            JObject json = null;

            var http = await url.AllowAnyHttpStatus().GetAsync(cancellationToken).ConfigureAwait(false);
            if (http.IsSuccessStatusCode)
            {
                var data = await http.Content.ReadAsStringAsync().ConfigureAwait(false);
                var regEx = new Regex(@"window\.__INITIAL_STATE__ = (.*);").Match(data);
                if (regEx.Groups.Count > 0)
                    json = (JObject)JObject.Parse(regEx.Groups[1].Value)["content"];
            }

            return json;
        }

        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null)
                return result;

            var directURL = searchTitle.Replace(" ", "-", StringComparison.OrdinalIgnoreCase).ToLower(PhoenixAdultNETProvider.Lang);
            if (!directURL.Contains("/", StringComparison.OrdinalIgnoreCase))
                directURL = directURL.Replace("-", "/", 1, StringComparison.OrdinalIgnoreCase);

            if (!int.TryParse(directURL.Split('/')[0], out _))
                directURL = directURL.Replace("/", "-", 1, StringComparison.OrdinalIgnoreCase);
            else
                directURL = directURL.Split('/')[1];

            directURL = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + directURL;
            var searchResultsURLs = new List<string>
            {
                directURL
            };

            var searchResults = await PhoenixAdultNETHelper.GetGoogleSearchResults(searchTitle, siteNum, cancellationToken).ConfigureAwait(false);
            foreach (var searchResult in searchResults)
            {
                var url = searchResult.Split('?').First();
                if (url.Contains("/movies/", StringComparison.OrdinalIgnoreCase) && !searchResultsURLs.Contains(url))
                    searchResultsURLs.Add(url);
            }

            foreach (var sceneURL in searchResultsURLs)
            {
                string curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultNETHelper.Encode(sceneURL)}";

                var sceneData = await Update(curID.Split('#'), cancellationToken).ConfigureAwait(false);
                if (sceneData != null)
                {
                    var res = new SceneSearch
                    {
                        Title = sceneData.Title
                    };

                    if (searchDate.HasValue)
                    {
                        res.ReleaseDate = searchDate;
                        curID += $"#{searchDate.Value.ToString("yyyy-MM-dd", PhoenixAdultNETProvider.Lang)}";
                    }

                    res.CurID = curID;
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
            var sceneData = await GetJSONfromPage(sceneURL, cancellationToken).ConfigureAwait(false);
            if (sceneData == null)
                return null;

            string contentName = string.Empty;
            foreach (var name in new List<string>() { "moviesContent", "videosContent" })
                if (sceneData.ContainsKey(name) && (sceneData[name] != null))
                {
                    contentName = name;
                    break;
                }

            if (string.IsNullOrEmpty(contentName))
                return null;

            sceneData = (JObject)sceneData[contentName];
            var sceneName = sceneData.Properties().First().Name;
            sceneData = (JObject)sceneData[sceneName];

            result.Title = (string)sceneData["title"];
            result.Description = (string)sceneData["description"];
            switch (siteNum[0])
            {
                case 23:
                    result.Studios.Add("Mylf");
                    break;

                case 24:
                    result.Studios.Add("TeamSkeet");
                    break;
            }

            DateTime? releaseDate = null;
            if (sceneData.ContainsKey("publishedDate"))
                releaseDate = (DateTime)sceneData["publishedDate"];
            else
            {
                if (sceneID.Length > 3)
                    if (DateTime.TryParseExact(sceneID[3], "yyyy-MM-dd", PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                        releaseDate = sceneDateObj;
            }

            if (releaseDate.HasValue)
                result.ReleaseDate = releaseDate.Value;

            string subSite;
            if (sceneData.ContainsKey("site"))
                subSite = (string)sceneData["site"]["name"];
            else
                subSite = PhoenixAdultNETHelper.GetSearchSiteName(siteNum);

            var genres = new List<string>();
            switch (subSite)
            {
                case "MylfBoss":
                    genres = new List<string> {
                        "Office", "Boss"
                    };
                    break;

                case "MylfBlows":
                    genres = new List<string> {
                        "Blowjob"
                    };
                    break;

                case "Milfty":
                    genres = new List<string> {
                        "Cheating"
                    };
                    break;

                case "Mom Drips":
                    genres = new List<string> {
                        "Creampie"
                    };
                    break;

                case "Milf Body":
                    genres = new List<string> {
                        "Gym", "Fitness"
                    };
                    break;

                case "Lone Milf":
                    genres = new List<string> {
                        "Solo"
                    };
                    break;

                case "Full Of JOI":
                    genres = new List<string> {
                        "JOI"
                    };
                    break;

                case "Mylfed":
                    genres = new List<string> {
                        "Lesbian"
                    };
                    break;

                case "MylfDom":
                    genres = new List<string> {
                        "BDSM"
                    };
                    break;

                case "Sis Loves Me":
                    genres = new List<string> {
                        "Step Sister"
                    };
                    break;

                case "DadCrush":
                case "DaughterSwap":
                    genres = new List<string> {
                        "Step Dad", "Step Daughter"
                    };
                    break;

                case "PervMom":
                    genres = new List<string> {
                        "Step Mom"
                    };
                    break;

                case "Family Strokes":
                    genres = new List<string> {
                        "Taboo Family"
                    };
                    break;

                case "Foster Tapes":
                    genres = new List<string> {
                        "Taboo Sex"
                    };
                    break;

                case "BFFs":
                    genres = new List<string> {
                        "Teen", "Group Sex"
                    };
                    break;

                case "Shoplyfter":
                    genres = new List<string> {
                        "Strip"
                    };
                    break;

                case "ShoplyfterMylf":
                    genres = new List<string> {
                        "Strip", "MILF"
                    };
                    break;

                case "Exxxtra Small":
                    genres = new List<string> {
                        "Teen", "Small Tits"
                    };
                    break;

                case "Little Asians":
                    genres = new List<string> {
                        "Teen", "Asian"
                    };
                    break;

                case "TeenJoi":
                    genres = new List<string> {
                        "Teen", "JOI"
                    };
                    break;

                case "Black Valley Girls":
                    genres = new List<string> {
                        "Teen", "Ebony"
                    };
                    break;

                case "Thickumz":
                    genres = new List<string> {
                        "Thick"
                    };
                    break;

                case "Dyked":
                    genres = new List<string> {
                        "Hardcore", "Teen", "Lesbian"
                    };
                    break;

                case "Teens Love Black Cocks":
                    genres = new List<string> {
                        "Teen", "BBC"
                    };
                    break;
            }

            foreach (var genreName in genres)
                result.Genres.Add(genreName);

            foreach (var genreName in new List<string>() { "MILF", "Mature" })
                result.Genres.Add(genreName);

            foreach (var actorLink in sceneData["models"])
            {
                string actorName = (string)actorLink["modelName"],
                       actorID = (string)actorLink["modelId"],
                       actorPhotoURL;

                var actorData = await GetJSONfromPage($"{PhoenixAdultNETHelper.GetSearchBaseURL(siteNum)}/models/{actorID}", cancellationToken).ConfigureAwait(false);
                if (actorData != null)
                {
                    actorPhotoURL = (string)actorData["modelsContent"][actorID]["img"];
                    result.Actors.Add(new Actor
                    {
                        Name = actorName,
                        Photo = actorPhotoURL,
                    });
                }
            }

            var img = (string)sceneData["img"];
            result.Posters.Add(img);
            result.Backgrounds.Add(img);

            return result;
        }
    }
}
