using PhoenixAdultNET.Providers.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace PhoenixAdultNET.Providers.Sites
{
    internal class NetworkPornPros : IPhoenixAdultNETProviderBase
    {
        public async Task<List<SceneSearch>> Search(int[] siteNum, string searchTitle, string encodedTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
                return result;

            var directURL = searchTitle.Replace(" ", "-", StringComparison.OrdinalIgnoreCase).Replace("'", "-", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(directURL.Substring(directURL.Length - 1, 1), out _) && directURL.Substring(directURL.Length - 2, 1) == "-")
                directURL = directURL[0..^1] + "-" + directURL.Substring(directURL.Length - 1, 1);

            string sceneURL = PhoenixAdultNETHelper.GetSearchSearchURL(siteNum) + directURL,
                    curID = $"{siteNum[0]}#{siteNum[1]}#{PhoenixAdultNETHelper.Encode(sceneURL)}";

            if (searchDate.HasValue)
            {
                var date = searchDate.Value.ToString("yyyy-MM-dd", PhoenixAdultNETProvider.Lang);
                curID += $"#{date}";
            }

            var sceneData = await Update(curID.Split('#'), cancellationToken).ConfigureAwait(false);

            result.Add(new SceneSearch
            {
                CurID = curID,
                Title = sceneData.Title
            });

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

            result.Title = sceneData.SelectSingleNode("//h1").InnerText.Trim();
            var description = sceneData.SelectSingleNode("//div[contains(@id, 'description')]");
            if (description != null)
                result.Description = description.InnerText.Trim();
            result.Studios.Add("Porn Pros");

            var dateNode = sceneData.SelectSingleNode("//div[@class='d-inline d-lg-block mb-1']/span");
            string date = null, dateFormat = null;
            if (dateNode != null)
            {
                date = dateNode.InnerText.Trim();
                dateFormat = "MMMM dd, yyyy";
            }
            else
            {
                if (sceneID.Length > 3)
                {
                    date = sceneID[3];
                    dateFormat = "yyyy-MM-dd";
                }
            }
            if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(dateFormat))
                if (DateTime.TryParseExact(date, dateFormat, PhoenixAdultNETProvider.Lang, DateTimeStyles.None, out DateTime sceneDateObj))
                    result.ReleaseDate = sceneDateObj;

            var genres = new List<string>();
            switch (PhoenixAdultNETHelper.GetSearchSiteName(siteNum))
            {
                case "Lubed":
                    genres = new List<string> {
                        "Lube", "Raw", "Wet"
                    };
                    break;

                case "Holed":
                    genres = new List<string> {
                        "Anal", "Ass"
                    };
                    break;

                case "POVD":
                    genres = new List<string> {
                        "Gonzo", "Pov"
                    };
                    break;

                case "MassageCreep":
                    genres = new List<string> {
                        "Massage", "Oil"
                    };
                    break;

                case "DeepThroatLove":
                    genres = new List<string> {
                        "Blowjob", "Deep Throat"
                    };
                    break;

                case "PureMature":
                    genres = new List<string> {
                        "MILF", "Mature"
                    };
                    break;

                case "Cum4K":
                    genres = new List<string> {
                        "Creampie"
                    };
                    break;

                case "GirlCum":
                    genres = new List<string> {
                        "Orgasms", "Girl Orgasm", "Multiple Orgasms"
                    };
                    break;

                case "PassionHD":
                    genres = new List<string> {
                        "Hardcore"
                    };
                    break;

                case "BBCPie":
                    genres = new List<string> {
                        "Interracial", "BBC", "Creampie"
                    };
                    break;
            }

            foreach (var genreName in genres)
                result.Genres.Add(genreName);

            var actorsNode = sceneData.SelectNodes("//div[contains(@class, 'pt-md')]//a[contains(@href, '/girls/')]");
            if (actorsNode != null)
                foreach (var actorLink in actorsNode)
                {
                    string actorName = actorLink.InnerText.Trim();

                    result.Actors.Add(new Actor
                    {
                        Name = actorName
                    });
                }

            var poster = sceneData.SelectSingleNode("//video[@id='player']");
            if (poster != null)
            {
                var img = poster.Attributes["poster"].Value;
                if (!img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    img = $"https:{img}";
                result.Posters.Add(img);
                result.Backgrounds.Add(img);
            }

            return result;
        }
    }
}
