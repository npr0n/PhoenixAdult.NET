using Flurl.Http;
using PhoenixAdultNET.Providers.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PhoenixAdultNET.Providers
{
    public static class PhoenixAdultNETProvider
    {
        public static readonly CultureInfo Lang = new CultureInfo("en-US", false);
        public static async Task<List<SceneSearch>> Search(string fileName, CancellationToken cancellationToken)
        {
            var result = new List<SceneSearch>();

            if (string.IsNullOrEmpty(fileName))
                return result;

            var title = ReplaceAbbrieviation(fileName);
            var site = GetSiteFromTitle(title);
            if (site.Key != null)
            {
                string searchTitle = GetClearTitle(title, site.Value),
                       searchDate = string.Empty,
                       encodedTitle;
                DateTime? searchDateObj;
                var titleAfterDate = GetDateFromTitle(searchTitle);

                var siteNum = new int[2] {
                    site.Key[0],
                    site.Key[1]
                };
                searchTitle = titleAfterDate.Item1;
                searchDateObj = titleAfterDate.Item2;
                if (searchDateObj.HasValue)
                    searchDate = searchDateObj.Value.ToString("yyyy-MM-dd", Lang);
                encodedTitle = Uri.EscapeDataString(searchTitle);

                var provider = PhoenixAdultNETList.GetProviderBySiteID(siteNum[0]);
                if (provider != null)
                {
                    result = await provider.Search(siteNum, searchTitle, encodedTitle, searchDateObj, cancellationToken).ConfigureAwait(false);
                    if (result.Count > 0)
                        if (result.Any(scene => scene.IndexNumber.HasValue))
                            result = result.OrderByDescending(scene => scene.IndexNumber.HasValue).ThenBy(scene => scene.IndexNumber).ToList();
                        else if (!string.IsNullOrEmpty(searchDate) && result.All(scene => scene.ReleaseDate.HasValue) && result.Any(scene => scene.ReleaseDate.Value != searchDateObj))
                            result = result.OrderBy(scene => Math.Abs((searchDateObj - scene.ReleaseDate).Value.TotalDays)).ToList();
                        else
                            result = result.OrderByDescending(scene => 100 - PhoenixAdultNETHelper.LevenshteinDistance(searchTitle, scene.Title)).ToList();
                }
            }

            return result;
        }

        public static async Task<Scene> Update(string externalID, CancellationToken cancellationToken)
        {
            var result = new Scene();

            if (string.IsNullOrEmpty(externalID))
                return null;

            var curID = externalID.Split('#');
            var provider = PhoenixAdultNETList.GetProviderBySiteID(int.Parse(curID[0], Lang));
            if (provider != null)
            {
                result = await provider.Update(curID, cancellationToken).ConfigureAwait(false);

                if (result.Actors != null)
                {
                    var clearActors = await PhoenixAdultNETActors.Cleanup(result, cancellationToken).ConfigureAwait(false);

                    result.Actors.Clear();
                    result.Actors.AddRange(clearActors);
                }

                if (result.Genres != null)
                {
                    var clearGenres = PhoenixAdultNETGenres.Cleanup(result.Genres, result.Title);

                    result.Genres.Clear();
                    result.Genres.AddRange(clearGenres);
                }

                if (result.Posters != null)
                {
                    var clearPosters = new List<string>();

                    foreach (var image in result.Posters)
                    {
                        if (!clearPosters.Contains(image))
                        {
                            var http = await image.AllowAnyHttpStatus().HeadAsync(cancellationToken).ConfigureAwait(false);
                            if (http.IsSuccessStatusCode)
                            {
                                var img = Image.FromStream(await image.GetStreamAsync(cancellationToken).ConfigureAwait(false));

                                if (img.Width > 100)
                                    clearPosters.Add(image);
                            }
                        }
                    }

                    result.Posters.Clear();
                    result.Posters.AddRange(clearPosters);
                }

                var clearBackgrounds = new List<string>();
                foreach (var image in result.Backgrounds)
                {
                    if (!clearBackgrounds.Contains(image))
                    {
                        if (result.Posters.Contains(image))
                            clearBackgrounds.Add(image);
                        else
                        {
                            var http = await image.AllowAnyHttpStatus().HeadAsync(cancellationToken).ConfigureAwait(false);
                            if (http.IsSuccessStatusCode)
                            {
                                var img = Image.FromStream(await image.GetStreamAsync(cancellationToken).ConfigureAwait(false));

                                if (img.Width > 100)
                                    clearBackgrounds.Add(image);
                            }
                        }
                    }
                }

                result.Backgrounds.Clear();
                result.Backgrounds.AddRange(clearBackgrounds);
            }

            return result;
        }

        public static KeyValuePair<int[], string> GetSiteFromTitle(string title)
        {
            string clearName = Regex.Replace(title, @"\W", string.Empty);
            var possibleSites = new Dictionary<int[], string>();

            foreach (var site in PhoenixAdultNETList.SiteList)
                foreach (var siteData in site.Value)
                {
                    string clearSite = Regex.Replace(siteData.Value[0], @"\W", string.Empty);
                    if (clearName.StartsWith(clearSite, StringComparison.OrdinalIgnoreCase))
                        possibleSites.Add(new int[] { site.Key, siteData.Key }, clearSite);
                }

            if (possibleSites.Count > 0)
                return possibleSites.OrderByDescending(x => x.Value.Length).First();

            return new KeyValuePair<int[], string>(null, null);
        }

        public static string GetClearTitle(string title, string siteName)
        {
            if (string.IsNullOrEmpty(title))
                return title;

            string clearName = Lang.TextInfo.ToTitleCase(title),
                   clearSite = siteName;

            clearName = clearName.Replace(".com", string.Empty, StringComparison.OrdinalIgnoreCase);

            clearName = Regex.Replace(clearName, @"[^a-zA-Z0-9 ]", " ");
            clearSite = Regex.Replace(clearSite, @"\W", string.Empty);

            bool matched = false;
            while (clearName.Contains(' ', StringComparison.OrdinalIgnoreCase))
            {
                clearName = PhoenixAdultNETHelper.ReplaceFirst(clearName, " ", string.Empty);
                if (clearName.StartsWith(clearSite, StringComparison.OrdinalIgnoreCase))
                {
                    matched = true;
                    break;
                }
            }

            if (matched)
            {
                clearName = clearName.Replace(clearSite, string.Empty, StringComparison.OrdinalIgnoreCase);
                clearName = string.Join(" ", clearName.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }

            return clearName;
        }

        public static (string, DateTime?) GetDateFromTitle(string title)
        {
            string searchDate,
                   searchTitle = title;
            var regExRules = new Dictionary<string, string> {
                { @"\b\d{4} \d{2} \d{2}\b", "yyyy MM dd" },
                { @"\b\d{2} \d{2} \d{2}\b", "yy MM dd" }
            };
            (string, DateTime?) searchData = (searchTitle, null);

            foreach (var regExRule in regExRules)
            {
                var regEx = Regex.Match(searchTitle, regExRule.Key);
                if (regEx.Groups.Count > 0)
                    if (DateTime.TryParseExact(regEx.Groups[0].Value, regExRule.Value, Lang, DateTimeStyles.None, out DateTime searchDateObj))
                    {
                        searchDate = searchDateObj.ToString("yyyy-MM-dd", Lang);
                        searchTitle = Regex.Replace(searchTitle, regExRule.Key, string.Empty).Trim();

                        searchData = (searchTitle, searchDateObj);
                        break;
                    }
            }

            return searchData;
        }

        public static string ReplaceAbbrieviation(string title)
        {
            string newTitle = title;

            foreach (var abbrieviation in PhoenixAdultNETList.AbbrieviationList)
            {
                Regex regex = new Regex(abbrieviation.Key, RegexOptions.IgnoreCase);
                if (regex.IsMatch(title))
                {
                    newTitle = regex.Replace(title, abbrieviation.Value, 1);
                    break;
                }
            }

            return newTitle;
        }
    }
}
