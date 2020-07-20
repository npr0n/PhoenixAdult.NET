using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace PhoenixAdultNET.Providers.Helpers
{
    public static class PhoenixAdultNETActors
    {
        public async static Task<List<Actor>> Cleanup(Scene item, CancellationToken cancellationToken)
        {
            var newPeoples = new List<Actor>();

            if (item == null)
                return newPeoples;

            foreach (var people in item.Actors)
            {
                people.Name = PhoenixAdultNETProvider.Lang.TextInfo.ToTitleCase(people.Name);
                people.Name = people.Name.Split("(").First().Trim();
                people.Name = people.Name.Replace("™", string.Empty, StringComparison.OrdinalIgnoreCase);
                people.Name = Replace(people.Name, item.Studios);

                if (string.IsNullOrEmpty(people.Photo))
                {
                    var Photo = await GetActorPhoto(people.Name, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(Photo))
                        people.Photo = Photo;
                }

                if (!newPeoples.Any(person => person.Name == people.Name))
                    newPeoples.Add(people);
            }

            return newPeoples;
        }

        private static string Replace(string actorName, List<string> studios)
        {
            var newActorName = ReplaceList.FirstOrDefault(x => x.Value.Contains(actorName, StringComparer.OrdinalIgnoreCase)).Key;
            if (!string.IsNullOrEmpty(newActorName))
                return newActorName;

            int siteIndex = -1;
            foreach (var studio in studios)
            {
                var studioName = studio.Split('!').First().Trim();

                switch (studioName)
                {
                    case "21Sextury":
                    case "Footsie Babes":
                        siteIndex = 0;
                        break;

                    case "Babes":
                        siteIndex = 1;
                        break;

                    case "Bang Bros":
                        siteIndex = 2;
                        break;

                    case "Deeper":
                    case "Tushyraw":
                    case "Tushy":
                    case "Blacked":
                    case "Blackedraw":
                    case "Vixen":
                        siteIndex = 3;
                        break;

                    case "FuelVirtual":
                        siteIndex = 4;
                        break;

                    case "LegalPorno":
                        siteIndex = 5;
                        break;

                    case "Joymii":
                        siteIndex = 6;
                        break;

                    case "Kink":
                        siteIndex = 7;
                        break;

                    case "Nubiles":
                        siteIndex = 8;
                        break;

                    case "Porn Pros":
                        siteIndex = 9;
                        break;

                    case "TeamSkeet":
                        siteIndex = 10;
                        break;

                    case "Twistys":
                        siteIndex = 11;
                        break;

                    case "X-Art":
                        siteIndex = 12;
                        break;

                    case "DDFProd":
                        siteIndex = 13;
                        break;

                    case "Reality Kings":
                        siteIndex = 14;
                        break;

                    case "WowGirls":
                        siteIndex = 15;
                        break;

                    case "Private":
                        siteIndex = 16;
                        break;

                    case "VIPissy":
                        siteIndex = 17;
                        break;

                    case "Erika Lust Films":
                        siteIndex = 18;
                        break;

                    case "Bang":
                        siteIndex = 19;
                        break;

                    case "Milehigh":
                    case "Doghouse Digital":
                        siteIndex = 20;
                        break;
                }
            }

            if (siteIndex > -1)
            {
                newActorName = ReplaceListStudio[siteIndex].FirstOrDefault(item => item.Value.Contains(actorName, StringComparer.OrdinalIgnoreCase)).Key;
                if (!string.IsNullOrEmpty(newActorName))
                    return newActorName;
            }

            return actorName;
        }

        public static async Task<string> GetActorPhoto(string Name, CancellationToken cancellationToken)
        {
            string image;

            image = await GetFromAdultDVDEmpire(Name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                return image;

            image = await GetFromBoobpedia(Name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                return image;

            image = await GetFromBabepedia(Name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                return image;

            image = await GetFromIAFD(Name, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(image))
                return image;

            return string.Empty;
        }

        private static async Task<string> GetFromAdultDVDEmpire(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"https://www.adultdvdempire.com/performer/search?q={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorNode = actorData.SelectSingleNode("//div[@id='performerlist']/div//a");
            if (actorNode != null)
            {
                var actorPageURL = "https://www.adultdvdempire.com" + actorNode.Attributes["href"].Value;
                var actorPage = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);

                var img = actorPage.SelectSingleNode("//div[contains(@class, 'performer-image-container')]/a");
                if (img != null)
                    image = img.Attributes["href"].Value;
            }

            return image;
        }

        private static async Task<string> GetFromBoobpedia(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"http://www.boobpedia.com/wiki/index.php?search={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorImageNode = actorData.SelectSingleNode("//table[@class='infobox']//a[@class='image']//img");
            if (actorImageNode != null)
            {
                var img = actorImageNode.Attributes["src"].Value;
                if (!img.Contains("NoImage", StringComparison.OrdinalIgnoreCase))
                    image = "http://www.boobpedia.com" + actorImageNode.Attributes["src"].Value;
            }

            return image;
        }

        private static async Task<string> GetFromBabepedia(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string actorImage = $"http://www.babepedia.com/pics/{name}.jpg";

            var http = await actorImage.AllowAnyHttpStatus().WithHeader("User-Agent", "Googlebot-Image/1.0").HeadAsync(cancellationToken).ConfigureAwait(false);
            if (http.IsSuccessStatusCode)
                image = actorImage;

            return image;
        }

        private static async Task<string> GetFromIAFD(string name, CancellationToken cancellationToken)
        {
            string image = null;

            if (string.IsNullOrEmpty(name))
                return image;

            string encodedName = HttpUtility.UrlEncode(name),
                   url = $"http://www.iafd.com/results.asp?searchtype=comprehensive&searchstring={encodedName}";

            var actorData = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var actorNode = actorData.SelectSingleNode("//table[@id='tblFem']//tbody//a");
            if (actorNode != null)
            {
                var actorPageURL = "http://www.iafd.com" + actorNode.Attributes["href"].Value;
                var actorPage = await HTML.ElementFromURL(actorPageURL, cancellationToken).ConfigureAwait(false);

                var actorImage = actorPage.SelectSingleNode("//div[@id='headshot']//img").Attributes["src"].Value;
                if (!actorImage.Contains("nophoto", StringComparison.OrdinalIgnoreCase))
                    image = actorImage;
            }

            return image;
        }

        public static readonly Dictionary<string, string[]> ReplaceList = new Dictionary<string, string[]> {

            { "Abbey Rain", new string[] { "Abby Rains" } },
            { "Abby Lee Brazil", new string[] { "Abby Lee" } },
            { "Abella Danger", new string[] { "Bella Danger" } },
            { "Addie Juniper", new string[] { "Ms Addie Juniper" } },
            { "Adriana Chechik", new string[] { "Adrianna Chechik", "Adriana Chechick" } },
            { "Alex D.", new string[] { "Alex D" } },
            { "Alura Jenson", new string[] { "Alura Tnt Jenson", "Alura 'Tnt' Jenson" } },
            { "Amia Miley", new string[] { "Amia Moretti" } },
            { "Amy Ried", new string[] { "Amy Reid" } },
            { "Ana Foxxx", new string[] { "Ana Fox", "Ana Foxx" } },
            { "Anastasia Morna", new string[] { "Anna Morna" } },
            { "Andreina Deluxe", new string[] { "Andreina De Lux", "Andreina De Luxe", "Andreina Dlux" } },
            { "Angel Piaff", new string[] { "Angela Piaf", "Angel Piaf" } },
            { "Ani Blackfox", new string[] { "Ani Black Fox", "Ani Black" } },
            { "Anita Bellini Berlusconi", new string[] { "Anita Bellini" } },
            { "Anna De Ville", new string[] { "Anna Deville" } },
            { "Annika Albrite", new string[] { "Anikka Albrite" } },
            { "April O'Neil", new string[] { "April ONeil", "April O'neil" } },
            { "Ashlee Graham", new string[] { "Ashley Graham" } },
            { "Bridgette B", new string[] { "Bridgette B." } },
            { "Britney Beth", new string[] { "Bibi Jones" } },
            { "Bunny Colby", new string[] { "Nadya Nabakova", "Nadya Nabokova" } },
            { "Capri Cavanni", new string[] { "Capri Cavalli" } },
            { "CeCe Capella", new string[] { "Ce Ce Capella" } },
            { "Charlie Red", new string[] { "Charli Red" } },
            { "Chris Strokes", new string[] { "Criss Strokes" } },
            { "Clea Gaultier", new string[] { "CléA Gaultier" } },
            { "Connie Carter", new string[] { "Josephine", "Conny", "Conny Carter", "Connie" } },
            { "Cyrstal Rae", new string[] { "Crystal Rae" } },
            { "Eden Sinclair", new string[] { "Eden Sin" } },
            { "Elsa Jean", new string[] { "Elsa Dream" } },
            { "Emma Hix", new string[] { "Crissy Kay", "Emma Hicks", "Emma Hixx" } },
            { "Eva Elfie", new string[] { "Tiny Teen", "Tieny Mieny", "Lady Jay", "Tiny Teen / Eva Elfie" } },
            { "Eve Laurence", new string[] { "Eve Lawrence" } },
            { "Francesca DiCaprio", new string[] { "Francesca Di Caprio" } },
            { "Gina Gerson", new string[] { "Doris Ivy" } },
            { "Goldie Glock", new string[] { "Goldie" } },
            { "Gulliana Alexis", new string[] { "Guiliana Alexis" } },
            { "Haley Reed", new string[] { "Hailey Reed" } },
            { "Jaye Summers", new string[] { "Charlotte Lee" } },
            { "Jenna Ross", new string[] { "Jenna J Ross", "Jenna J. Ross" } },
            { "Jenny Fer", new string[] { "Jenny Ferri" } },
            { "Jessi Gold", new string[] { "Jassie Gold", "Jaggie Gold" } },
            { "Jessica Foxx", new string[] { "Jessica Blue", "Jessica Cute" } },
            { "Jojo Kiss", new string[] { "Jo Jo Kiss" } },
            { "Josephine Jackson", new string[] { "Josephina Jackson" } },
            { "Kagney Linn Karter", new string[] { "Kagney Lynn Karter" } },
            { "Kari Sweet", new string[] { "Kari Sweets" } },
            { "Katerina Hartlova", new string[] { "Katarina" } },
            { "Kendra Lust", new string[] { "Kendra May Lust" } },
            { "Khloe Kapri", new string[] { "Khloe Capri", "Chloe Capri" } },
            { "Krystal Boyd", new string[] { "Anjelica", "Ebbi", "Abby H", "Katherine A" } },
            { "Lilly Ford", new string[] { "Lilly Lit" } },
            { "Lily Labeau", new string[] { "Lilly LaBeau", "Lilly Labuea", "Lily La Beau", "Lily Luvs" } },
            { "Lora Craft", new string[] { "Lara Craft" } },
            { "Maddy O'Reilly", new string[] { "Maddy OReilly", "Maddy O'reilly" } },
            { "Melena Maria Rya", new string[] { "Maria Rya", "Melena Maria" } },
            { "Miss Jade Indica", new string[] { "Jade Indica" } },
            { "Moe Johnson", new string[] { "Moe The Monster Johnson" } },
            { "Nancy Ace", new string[] { "Nancy A.", "Nancy A" } },
            { "Nathaly Cherie", new string[] { "Nathaly", "Nathalie Cherie", "Natalie Cherie" } },
            { "Nika Noire", new string[] { "Nika Noir" } },
            { "Noemilk", new string[] { "Noe Milk", "Noemiek" } },
            { "Paula Shy", new string[] { "Christy Charming" } },
            { "Pinky June", new string[] { "Grace Hartley" } },
            { "Pristine Edge", new string[] { "Jane Doux" } },
            { "Remy Lacroix", new string[] { "Remy La Croix" } },
            { "Riley Jensen", new string[] { "Riley Jenson", "Riley Anne", "Rilee Jensen" } },
            { "Sara Luvv", new string[] { "Sara Luv" } },
            { "Skylar Vox", new string[] { "Dylann Vox", "Dylan Vox" } },
            { "Stella Banxxx", new string[] { "Stella Bankxxx", "Stella Ferrari" } },
            { "Stephanie Renee", new string[] { "Sedona", "Stefanie Renee" } },
            { "Steven St. Croix", new string[] { "Steven St.Croix" } },
            { "Sybil A", new string[] { "Sybil Kailena", "Sybil" } },
            { "Veronica Valentine", new string[] { "Veronica Vega" } },
        };

        public static readonly Dictionary<int, Dictionary<string, string[]>> ReplaceListStudio = new Dictionary<int, Dictionary<string, string[]>> {{
                0, new Dictionary<string, string[]> {
                    { "Henessy", new string[] { "Henna Ssy" } },
                    { "Katarina Muti", new string[] { "Ariel Temple" } },
                    { "Krystal Boyd", new string[] { "Abbie" } },
                }
            },{
                1, new Dictionary<string, string[]> {
                    { "Aika May", new string[] { "Aiko May" } },
                    { "Ariel Piper Fawn", new string[] { "Ariel" } },
                    { "Clover", new string[] { "Katya Clover" } },
                    { "Krystal Boyd", new string[] { "Angelica" } },
                }
            },{
                2, new Dictionary<string, string[]> {
                    { "Abella Anderson", new string[] { "Amy" } },
                    { "Noemie Bilas", new string[] { "Noemi Bilas" } },
                }
            },{
                3, new Dictionary<string, string[]> {
                    { "Vika Lita", new string[] { "Vikalita" } },
                    { "Vina Sky", new string[] { "Vina Skyy" } },
                }
            },{
                4, new Dictionary<string, string[]> {
                    { "Abigaile Johnson", new string[] { "Abigaile" } },
                    { "Aletta Ocean", new string[] { "Aletta" } },
                    { "Alexis Adams", new string[] { "Alexis" } },
                    { "Alina Li", new string[] { "Alina" } },
                    { "Allie Haze", new string[] { "Allie H" } },
                    { "Allie Rae", new string[] { "Allie" } },
                    { "Ashlyn Rae", new string[] { "Ashlyn" } },
                    { "August Ames", new string[] { "August" } },
                    { "Belle Knox", new string[] { "Belle" } },
                    { "Bibi Jones", new string[] { "Brittany" } },
                    { "Bibi jones", new string[] { "Britney B" } },
                    { "Brooklyn Chase", new string[] { "Brooklyn" } },
                    { "Casana Lei", new string[] { "Casana" } },
                    { "Casi James", new string[] { "Casi" } },
                    { "Dillion Harper", new string[] { "Dillion", "Dillon" } },
                    { "Ella Milano", new string[] { "Ella M" } },
                    { "Elsa Jean", new string[] { "Elsa" } },
                    { "Emily Grey", new string[] { "Emily" } },
                    { "Erin Stone", new string[] { "Erin" } },
                    { "Evilyn Fierce", new string[] { "Evilyn" } },
                    { "Haley Cummings", new string[] { "Haley" } },
                    { "Hayden Winters", new string[] { "Hayden" } },
                    { "Holly Michaels", new string[] { "Holly" } },
                    { "Hope Howell", new string[] { "Hope" } },
                    { "Isis Taylor", new string[] { "Isis" } },
                    { "Janice Griffith", new string[] { "Janice" } },
                    { "Jaslene Jade", new string[] { "Jaslene" } },
                    { "Jayden Taylors", new string[] { "Jayden" } },
                    { "Jenna Rose", new string[] { "Jenna" } },
                    { "Jessica Robbins", new string[] { "Jessica" } },
                    { "Jynx Maze", new string[] { "Jynx" } },
                    { "Karina White", new string[] { "Karina" } },
                    { "Kennedy Leigh", new string[] { "Kennedy" } },
                    { "Kodi Gamble", new string[] { "Kodi" } },
                    { "Lacy Channing", new string[] { "Lacy" } },
                    { "Lexi Belle", new string[] { "Lexi" } },
                    { "Lexi Bloom", new string[] { "Lexi B" } },
                    { "Lexi Diamond", new string[] { "Lexi D" } },
                    { "Lily Carter", new string[] { "Lily C", "Lily" } },
                    { "Lily Love", new string[] { "Lily" } },
                    { "Lizz Taylor", new string[] { "Lizz" } },
                    { "Lola Foxx", new string[] { "Lola" } },
                    { "Lucy Doll", new string[] { "Lucy" } },
                    { "Madison Ivy", new string[] { "Madison" } },
                    { "Mary Jane Johnson", new string[] { "Maryjane" } },
                    { "Mia Malkova", new string[] { "Mia" } },
                    { "Molly Bennett", new string[] { "Molly" } },
                    { "Naomi West", new string[] { "Naomi" } },
                    { "Nicole Ray", new string[] { "Nicole" } },
                    { "Presley Carter", new string[] { "Presley" } },
                    { "Pristine Edge", new string[] { "Pristine" } },
                    { "Rebecca Linares", new string[] { "Rebecca" } },
                    { "Remy LaCroix", new string[] { "Remy" } },
                    { "Riley Reid", new string[] { "Riley" } },
                    { "Scarlet Red", new string[] { "Scarlet" } },
                    { "Staci Silverstone", new string[] { "Staci" } },
                    { "Stacie Jaxx", new string[] { "Stacie" } },
                    { "Stephanie Cane", new string[] { "Stephanie C" } },
                    { "Teal Conrad", new string[] { "Tealey" } },
                    { "Tessa Taylor", new string[] { "Tessa" } },
                    { "Tori Black", new string[] { "Tori" } },
                    { "Vanessa Cage", new string[] { "Vanessa" } },
                    { "Victoria Rae Black", new string[] { "Victoria R", "Victoria" } },
                    { "Whitney Westgate", new string[] { "Whitney" } },
                    { "Zoey Kush", new string[] { "Zoey" } },
                }
            },{
                5, new Dictionary<string, string[]> {
                    { "Krystal Boyd", new string[] { "Abby" } },
                    { "Sophia Traxler", new string[] { "Olivia" } },
                }
            },{
                6, new Dictionary<string, string[]> {
                    { "Abigaile Johnson", new string[] { "Abigail" } },
                    { "Adele Sunshine", new string[] { "Sunny G." } },
                    { "Aleska Diamond", new string[] { "Aleska D." } },
                    { "Alessandra Jane", new string[] { "Alessandra J." } },
                    { "Alexa Tomas", new string[] { "Alexa" } },
                    { "Alexis Brill", new string[] { "Alexis B." } },
                    { "Alexis Crystal", new string[] { "Alexis" } },
                    { "Alexis Texas", new string[] { "Alexis T." } },
                    { "Alexis Venton", new string[] { "Alexis V." } },
                    { "Allie Jordan", new string[] { "Allie J." } },
                    { "Alyssa Branch", new string[] { "Alysa" } },
                    { "Amirah Adara", new string[] { "Amirah A." } },
                    { "Andie Darling", new string[] { "Andie" } },
                    { "Angel Blade", new string[] { "Angel B." } },
                    { "Anita Bellini", new string[] { "Anita B." } },
                    { "Apolonia Lapiedra", new string[] { "Apolonia" } },
                    { "Ariel Piper Fawn", new string[] { "Ariel" } },
                    { "Aubrey James", new string[] { "Aubrey J." } },
                    { "Avril Hall", new string[] { "Avril H." } },
                    { "Bailey Ryder", new string[] { "Bailey R." } },
                    { "Billie Star", new string[] { "Billie" } },
                    { "Blanche Bradburry", new string[] { "Blanche B." } },
                    { "Brittney Banxxx", new string[] { "Brittany" } },
                    { "Candice Luca", new string[] { "Candice" } },
                    { "Candy Blond", new string[] { "Candy B." } },
                    { "Cara Mell", new string[] { "Rena" } },
                    { "Carmen McCarthy", new string[] { "Carmen C." } },
                    { "Carolina Abril", new string[] { "Carolina" } },
                    { "Cayenne Klein", new string[] { "Anna P." } },
                    { "Cayla Lyons", new string[] { "Cayla L." } },
                    { "Celeste Star", new string[] { "Celeste" } },
                    { "Chastity Lynn", new string[] { "Chastity L." } },
                    { "Cherry Kiss", new string[] { "Cherry K." } },
                    { "Christen Courtney", new string[] { "Christen" } },
                    { "Cindy Carson", new string[] { "Cindy L." } },
                    { "Cindy Dollar", new string[] { "Cindy D." } },
                    { "Clea Gaultier", new string[] { "Clea G" } },
                    { "Coco de Mal", new string[] { "Coco" } },
                    { "Connie Carter", new string[] { "Josephine" } },
                    { "Dani Daniels", new string[] { "Dani D." } },
                    { "Defrancesca Gallardo", new string[] { "Defrancesca" } },
                    { "Denisa Heaven", new string[] { "Denisa" } },
                    { "Dido Angel", new string[] { "Lara" } },
                    { "Elaina Raye", new string[] { "Eliana R." } },
                    { "Ella Milano", new string[] { "Ella M." } },
                    { "Emma Brown", new string[] { "Jana Q." } },
                    { "Erika Kortni", new string[] { "Erika K." } },
                    { "Eufrat Mai", new string[] { "Eufrat" } },
                    { "Eveline Dellai", new string[] { "Eveline D." } },
                    { "Evi Fox", new string[] { "Evi F." } },
                    { "Evilyn Fierce", new string[] { "Evilyn F." } },
                    { "Faye Reagan", new string[] { "Faye R." } },
                    { "Ferrera Gomez", new string[] { "Ferrera" } },
                    { "Foxy Di", new string[] { "Medina U." } },
                    { "Frida Sante", new string[] { "Frida S." } },
                    { "Gina Devine", new string[] { "Gina V." } },
                    { "Gina Gerson", new string[] { "Gina G." } },
                    { "Ginebra Bellucci", new string[] { "Ginebra B." } },
                    { "Ginger Fox", new string[] { "Ginger" } },
                    { "Giselle Leon", new string[] { "Giselle L." } },
                    { "Hayden Winters", new string[] { "Hayden W." } },
                    { "Heather Starlet", new string[] { "Heather S." } },
                    { "Holly Anderson", new string[] { "Holly" } },
                    { "Holly Michaels", new string[] { "Holly M." } },
                    { "Iwia", new string[] { "Ivy" } },
                    { "Izzy Delphine", new string[] { "Delphine" } },
                    { "Jana Jordan", new string[] { "Jana J." } },
                    { "Jayden Cole", new string[] { "Jayden C." } },
                    { "Jennifer White", new string[] { "Jennifer W." } },
                    { "Jessica Bee", new string[] { "Jessica B." } },
                    { "Jessie Jazz", new string[] { "Jessie" } },
                    { "Julia Roca", new string[] { "Julia R." } },
                    { "Kaci Starr", new string[] { "Kaci S." } },
                    { "Kari Sweet", new string[] { "Kari" } },
                    { "Karlie Montana", new string[] { "Karlie" } },
                    { "Karol Lilien", new string[] { "Karol T." } },
                    { "Katie Jordin", new string[] { "Katie J." } },
                    { "Kattie Gold", new string[] { "Katie G." } },
                    { "Katy Rose", new string[] { "Katy R." } },
                    { "Katya Clover", new string[] { "Clover" } },
                    { "Kelly White", new string[] { "Kelly W" } },
                    { "Kiara Diane", new string[] { "Kiara D." } },
                    { "Kiara Lord", new string[] { "Kiara L." } },
                    { "Kira Thorn", new string[] { "Kira T." } },
                    { "Kitty Jane", new string[] { "Kitty J." } },
                    { "Lady Dee", new string[] { "Dee" } },
                    { "Lena Nicole", new string[] { "Lena N." } },
                    { "Leony April", new string[] { "Jessica" } },
                    { "Lexi Dona", new string[] { "Lexi D." } },
                    { "Lexi Swallow", new string[] { "Lexi S." } },
                    { "Lilu Moon", new string[] { "Lilu" } },
                    { "Lily Carter", new string[] { "Lily B." } },
                    { "Lily Labeau", new string[] { "Lily L." } },
                    { "Little Caprice", new string[] { "Caprice" } },
                    { "Lovenia Lux", new string[] { "Bailey D." } },
                    { "Lucy Heart", new string[] { "Lucy H." } },
                    { "Lucy Li", new string[] { "Lucy L." } },
                    { "Luna Corazon", new string[] { "Luna C." } },
                    { "Marie McCray", new string[] { "Maria C." } },
                    { "Marry Queen", new string[] { "Miela" } },
                    { "Merry Pie", new string[] { "Patricya L." } },
                    { "Mia Manarote", new string[] { "Mia D." } },
                    { "Michaela Isizzu", new string[] { "Mila K." } },
                    { "Milana Blanc", new string[] { "Milana R." } },
                    { "Milena Devi", new string[] { "Milena D." } },
                    { "Mira Sunset", new string[] { "Alice B." } },
                    { "Misty Stone", new string[] { "Misty S." } },
                    { "Mona Lee", new string[] { "Mona L." } },
                    { "Monika Benz", new string[] { "Stacey" } },
                    { "Nancy Ace", new string[] { "Jane F." } },
                    { "Natalie Nice", new string[] { "Natalie N." } },
                    { "Nataly Gold", new string[] { "Nataly G." } },
                    { "Nathaly Cherie", new string[] { "Natalli" } },
                    { "Niki Sweet", new string[] { "Niky S." } },
                    { "Nikki Daniels", new string[] { "Nikki" } },
                    { "Paula Shy", new string[] { "Paula S." } },
                    { "Paulina Soul", new string[] { "Paulina S" } },
                    { "Pinky June", new string[] { "Anneli" } },
                    { "Piper Perri", new string[] { "Piper P." } },
                    { "Presley Hart", new string[] { "Presley H." } },
                    { "Pristine Edge", new string[] { "Pristine E." } },
                    { "Reena Sky", new string[] { "Reena" } },
                    { "Renee Perez", new string[] { "Renee P." } },
                    { "Ria Rodrigez", new string[] { "Ria R." } },
                    { "Rihanna Samuel", new string[] { "Rihanna" } },
                    { "Riley Jensen", new string[] { "Riley" } },
                    { "Rina Ellis", new string[] { "Rina" } },
                    { "Rose Delight", new string[] { "Belinda" } },
                    { "Sage Evans", new string[] { "Sage E." } },
                    { "Sally Charles", new string[] { "Sally C." } },
                    { "Sandy Ambrosia", new string[] { "Sandy A." } },
                    { "Sara Jaymes", new string[] { "Sara" } },
                    { "Sara Luvv", new string[] { "Sara L." } },
                    { "Scarlet Banks", new string[] { "Scarlet B." } },
                    { "Shyla Jennings", new string[] { "Shyla G.", "Shyla" } },
                    { "Silvie Deluxe", new string[] { "Simona" } },
                    { "Sophia Jade", new string[] { "Sophia J." } },
                    { "Stella Cox", new string[] { "Stella C" } },
                    { "Suzie Carina", new string[] { "Suzie" } },
                    { "Sybil A", new string[] { "Davina E." } },
                    { "Tarra White", new string[] { "Tarra W." } },
                    { "Tasha Reign", new string[] { "Tasha R." } },
                    { "Taylor Vixen", new string[] { "Taylor V." } },
                    { "Teagan Summers", new string[] { "Tegan S." } },
                    { "Tess Lyndon", new string[] { "Tess L." } },
                    { "Tiffany Fox", new string[] { "Tiffany F." } },
                    { "Tiffany Thompson", new string[] { "Tiffany T." } },
                    { "Tina Blade", new string[] { "Tina" } },
                    { "Tina Hot", new string[] { "Tina H." } },
                    { "Tracy Gold", new string[] { "Tracy A." } },
                    { "Tracy Lindsay", new string[] { "Tracy" } },
                    { "Tracy Smile", new string[] { "Tracy S." } },
                    { "Tyra Moon", new string[] { "Athina" } },
                    { "Uma Zex", new string[] { "Uma Z." } },
                    { "Valentina Nappi", new string[] { "Valentina N." } },
                    { "Vanda Lust", new string[] { "Vanda" } },
                    { "Victoria Blaze", new string[] { "Victoria B." } },
                    { "Victoria Puppy", new string[] { "Victoria P." } },
                    { "Victoria Rae Black", new string[] { "Victoria R." } },
                    { "Victoria Sweet", new string[] { "Viktoria S." } },
                    { "Vinna Reed", new string[] { "Vinna R." } },
                    { "Viola Bailey", new string[] { "Vanea H." } },
                    { "Whitney Conroy", new string[] { "Whitney C." } },
                    { "Zazie Sky", new string[] { "Zazie S." } },
                    { "Zena Little", new string[] { "Lilly K." } },
                    { "Zoe Voss", new string[] { "Zoe V." } },
                }
            },{
                7, new Dictionary<string, string[]> {
                    { "Alana Evans", new string[] { "Alana" } },
                    { "Anna Ashton", new string[] { "Sandy" } },
                    { "Avy Scott", new string[] { "Avi Scott" } },
                    { "Boo Delicious", new string[] { "Boo" } },
                    { "Courtney Devine", new string[] { "Courtney" } },
                    { "Deviant Kade", new string[] { "Kade" } },
                    { "Diamond Foxxx", new string[] { "Diamond" } },
                    { "Elyse Stone", new string[] { "Elyse" } },
                    { "Emilie Davinci", new string[] { "Emily" } },
                    { "Emily Marilyn", new string[] { "Molly Matthews" } },
                    { "Harmony Rose", new string[] { "Harmony" } },
                    { "Heather Starlet", new string[] { "Heather Starlett" } },
                    { "Jassie James", new string[] { "Jassie" } },
                    { "Julie Knight", new string[] { "Julie Night" } },
                    { "Kristine Andrews", new string[] { "Kristine" } },
                    { "Leah Parker", new string[] { "Leah" } },
                    { "Liz Tyler", new string[] { "Cowgirl" } },
                    { "Lola Milano", new string[] { "Alexa Jaymes" } },
                    { "Lola Taylor", new string[] { "Lolita Taylor" } },
                    { "Melanie Jagger", new string[] { "Melanie" } },
                    { "Meriesa Arroyo", new string[] { "Meriesa" } },
                    { "Michelle Avanti", new string[] { "Michele Avanti" } },
                    { "Nadine Sage", new string[] { "Naidyne" } },
                    { "Natalia Wood", new string[] { "Danielle" } },
                    { "Phoenix Ray", new string[] { "Phoenix" } },
                    { "Phyllisha Anne", new string[] { "Phyllisha" } },
                    { "Porsha Blaze", new string[] { "Porsha" } },
                    { "Ramona Luv", new string[] { "Ramona" } },
                    { "Sabrine Maui", new string[] { "Sabrine" } },
                    { "Sara Jaymes", new string[] { "Sarah Jaymes" } },
                    { "Sasha Sin", new string[] { "Sascha Sin" } },
                    { "Teagan Summers", new string[] { "Tegan Summer" } },
                    { "Wanda Curtis", new string[] { "Wanda" } },
                }
            },{
                8, new Dictionary<string, string[]> {
                    { "Abby Cross", new string[] { "Penny" } },
                    { "Addie Moore", new string[] { "Addie" } },
                    { "Addison Rose", new string[] { "Addison" } },
                    { "Adel Bye", new string[] { "Adella" } },
                    { "Adrianna Gold", new string[] { "Adrianne" } },
                    { "Afrodite Night", new string[] { "Lauren" } },
                    { "Alana Jade", new string[] { "Alana G" } },
                    { "Alana Leigh", new string[] { "Alanaleigh" } },
                    { "Aleska Diamond", new string[] { "Alexa" } },
                    { "Aletta Ocean", new string[] { "Aletta" } },
                    { "Alexia Sky", new string[] { "Alexiasky" } },
                    { "Alexis Love", new string[] { "Alexis" } },
                    { "Alice Miller", new string[] { "Aliana" } },
                    { "Alicia Angel", new string[] { "Alicia" } },
                    { "Allie Haze", new string[] { "Alliehaze" } },
                    { "Ally Ann", new string[] { "Allyann" } },
                    { "Allyssa Hall", new string[] { "Allyssa" } },
                    { "Amai Liu", new string[] { "Amai" } },
                    { "Ami Emerson", new string[] { "Ami" } },
                    { "Amia Miley", new string[] { "Abbey" } },
                    { "Amy Brooke", new string[] { "Amybrooke" } },
                    { "Amy Reid", new string[] { "Amy" } },
                    { "Amy Sativa", new string[] { "Amysativa" } },
                    { "Andie Valentino", new string[] { "Andie" } },
                    { "Andrea Anderson", new string[] { "Aundrea" } },
                    { "Angelina Ashe", new string[] { "Angelinaash" } },
                    { "Angelina Brooke", new string[] { "Angellina" } },
                    { "Angie Emerald", new string[] { "Angela" } },
                    { "Angie Koks", new string[] { "Koks" } },
                    { "Anita Pearl", new string[] { "Amalie" } },
                    { "Anna Nova", new string[] { "Inus" } },
                    { "Anna Stevens", new string[] { "Annastevens" } },
                    { "Annabelle Lee", new string[] { "Annabelle" } },
                    { "Annette Allen", new string[] { "Annette" } },
                    { "Annika Eve", new string[] { "Annika" } },
                    { "Anoli Angel", new string[] { "Anoli" } },
                    { "April Aubrey", new string[] { "April" } },
                    { "April O'Neil", new string[] { "Apriloneil" } },
                    { "Ariadna Moon", new string[] { "Ariadna" } },
                    { "Ariel Piper Fawn", new string[] { "Gabriella" } },
                    { "Ariel Rebel", new string[] { "Ariel" } },
                    { "Ashlee Allure", new string[] { "Ashlee" } },
                    { "Ashley Bulgari", new string[] { "Bulgari" } },
                    { "Ashley Jane", new string[] { "Ashleyjane" } },
                    { "Ashley Jensen", new string[] { "Jensen" } },
                    { "Ashley Stillar", new string[] { "Nicol" } },
                    { "Ashlyn Rae", new string[] { "Ashlynrae" } },
                    { "Asuna Fox", new string[] { "Asuna" } },
                    { "Athena Faris", new string[] { "Ms Faris" } },
                    { "Athena Palomino", new string[] { "Palomino" } },
                    { "Austin Reines", new string[] { "Austin" } },
                    { "Ava Skye", new string[] { "Sarahjo" } },
                    { "Beata Undine", new string[] { "Beata" } },
                    { "Bella Blue", new string[] { "Bella B" } },
                    { "Bella Cole", new string[] { "Nikala" } },
                    { "Bella Rossi", new string[] { "Nadea" } },
                    { "Bernie Svintis", new string[] { "Bernie" } },
                    { "Billie Star", new string[] { "Pinkule" } },
                    { "Billy Raise", new string[] { "Billie" } },
                    { "Black Angelika", new string[] { "Angelica" } },
                    { "Black Panther", new string[] { "Alexcia" } },
                    { "Bliss Lei", new string[] { "Bliss" } },
                    { "Boroka Balls", new string[] { "Boroka" } },
                    { "Bree Olson", new string[] { "Brea" } },
                    { "Brigitte Hunter", new string[] { "Layna" } },
                    { "Brynn Tyler", new string[] { "Brynn" } },
                    { "Callie Dee", new string[] { "Calliedee" } },
                    { "Capri Anderson", new string[] { "Capri" } },
                    { "Carin Kay", new string[] { "Karin" } },
                    { "Carli Banks", new string[] { "Carli" } },
                    { "Carmen Gemini", new string[] { "Gemini" } },
                    { "Carmen Kinsley", new string[] { "Carman" } },
                    { "Carmen McCarthy", new string[] { "Carmin" } },
                    { "Casey Donell", new string[] { "Tonya" } },
                    { "Casey Nohrman", new string[] { "Elza A" } },
                    { "Cassandra Calogera", new string[] { "Cassandra" } },
                    { "Cate Harrington", new string[] { "Cate" } },
                    { "Celeste Star", new string[] { "Celeste" } },
                    { "Celina Cross", new string[] { "Celina" } },
                    { "Charlie Laine", new string[] { "Charlie" } },
                    { "Charlie Lynn", new string[] { "Charlielynn" } },
                    { "Charlotte Stokely", new string[] { "Charlotte" } },
                    { "Chastity Lynn", new string[] { "Chastity" } },
                    { "Chloe Couture", new string[] { "Chloe Cherry" } },
                    { "Chloe James", new string[] { "Chloejames" } },
                    { "Chloe Morgan", new string[] { "Britne" } },
                    { "Christie Lee", new string[] { "Christine" } },
                    { "Christine Alexis", new string[] { "Chris" } },
                    { "Cindy Dee", new string[] { "Nitca" } },
                    { "Cindy Hope", new string[] { "Cindy", "Klaudia" } },
                    { "Cindy Shine", new string[] { "Yvonne" } },
                    { "Cira Nerri", new string[] { "Katy P" } },
                    { "Connie Carter", new string[] { "Conny" } },
                    { "Connie Rose", new string[] { "Ninoska" } },
                    { "Courtney Cummz", new string[] { "Courtney" } },
                    { "Crissy Moon", new string[] { "Crissy" } },
                    { "Crissy Snow", new string[] { "Crissysnow" } },
                    { "Cristal Matthews", new string[] { "Cristal" } },
                    { "Crystal Maiden", new string[] { "Juliya" } },
                    { "Dana Sinnz", new string[] { "Dina" } },
                    { "Dani Jensen", new string[] { "Dani" } },
                    { "Danielle Maye", new string[] { "Danielle" } },
                    { "Danielle Trixie", new string[] { "Jess" } },
                    { "Daphne Angel", new string[] { "Daphne" } },
                    { "Deena Daniels", new string[] { "Deanna" } },
                    { "Deja Move", new string[] { "Lussy M" } },
                    { "Demi Scott", new string[] { "Sally" } },
                    { "Deny Moor", new string[] { "Tea" } },
                    { "Devi Emmerson", new string[] { "Devi" } },
                    { "Dido Angel", new string[] { "Kira" } },
                    { "Dolly Diore", new string[] { "Diore" } },
                    { "Dominic Anna", new string[] { "Dominica" } },
                    { "Eden Adams", new string[] { "Edenadams" } },
                    { "Eileen Sue", new string[] { "Eileen" } },
                    { "Elena Rivera", new string[] { "Elena" } },
                    { "Elina Mikki", new string[] { "Mikki" } },
                    { "Elizabeth Anne", new string[] { "Elizabethanne" } },
                    { "Emy Reyes", new string[] { "Emy" } },
                    { "Eufrat Mai", new string[] { "Ella" } },
                    { "Eva Gold", new string[] { "Eva" } },
                    { "Evah Ellington", new string[] { "Ellington" } },
                    { "Eve Angel", new string[] { "Eve" } },
                    { "Eveline Dellai", new string[] { "Evelin" } },
                    { "Evelyn Baum", new string[] { "Evelyn" } },
                    { "Evelyn Cage", new string[] { "Linna" } },
                    { "Faina Bona", new string[] { "Faina" } },
                    { "Faith Leon", new string[] { "Faith" } },
                    { "Faye Reagan", new string[] { "Faye" } },
                    { "Faye X Taylor", new string[] { "Fayex" } },
                    { "Federica Hill", new string[] { "Federica" } },
                    { "Felicia Rain", new string[] { "Lolly J" } },
                    { "Ferrera Gomez", new string[] { "Ferrera" } },
                    { "Franziska Facella", new string[] { "Franziska" } },
                    { "Frida Stark", new string[] { "Frida" } },
                    { "Gabriella Lati", new string[] { "Esegna" } },
                    { "Georgia Jones", new string[] { "Georgia" } },
                    { "Ginger Lee", new string[] { "Ginger" } },
                    { "Goldie Baby", new string[] { "Giselle" } },
                    { "Goldie Glock", new string[] { "Violet" } },
                    { "Haley Sweet", new string[] { "Haleysweet" } },
                    { "Hannah West", new string[] { "Hanna" } },
                    { "Heather Starlet", new string[] { "Katie" } },
                    { "Heidi Harper", new string[] { "Heidi C" } },
                    { "Holly Fox", new string[] { "Hollyfox" } },
                    { "Ivana Sugar", new string[] { "Vania" } },
                    { "Izzy Delphine", new string[] { "Delphine" } },
                    { "Jacqueline Sweet", new string[] { "Zara" } },
                    { "Jada Gold", new string[] { "Caise" } },
                    { "Jaelyn Fox", new string[] { "Jaelyn" } },
                    { "Jana Jordan", new string[] { "Felicity" } },
                    { "Jana Sheridan", new string[] { "Sheridan" } },
                    { "Jasmine Davis", new string[] { "Polly" } },
                    { "Jasmine Rouge", new string[] { "Jasmine" } },
                    { "Jassie James", new string[] { "Jassie" } },
                    { "Jayme Langford", new string[] { "Alana" } },
                    { "Jenna Presley", new string[] { "Jenna" } },
                    { "Jenni Carmichael", new string[] { "Lindsay" } },
                    { "Jenni Czech", new string[] { "Jenni" } },
                    { "Jenni Lee", new string[] { "Jenny" } },
                    { "Jenny Appach", new string[] { "Jenniah" } },
                    { "Jenny Sanders", new string[] { "Andrea" } },
                    { "Jeny Baby", new string[] { "Jeny" } },
                    { "Jessi Gold", new string[] { "Marsa" } },
                    { "Jessica Foxx", new string[] { "Serendipity" } },
                    { "Jessica Valentino", new string[] { "Dessa" } },
                    { "Jessie Cox", new string[] { "Jessie" } },
                    { "Jessika Lux", new string[] { "Miesha" } },
                    { "Jewel Affair", new string[] { "Jamie" } },
                    { "Joanna Pret", new string[] { "Marfa" } },
                    { "Jordan Bliss", new string[] { "Jordanbliss" } },
                    { "Judith Fox", new string[] { "Judith" } },
                    { "Juicy Pearl", new string[] { "Pearl" } },
                    { "Juliana Grandi", new string[] { "Julie" } },
                    { "Kacey Jordan", new string[] { "Kacey" } },
                    { "Kali Lane", new string[] { "Kalilane" } },
                    { "Kandi Milan", new string[] { "Kandi" } },
                    { "Kara Novak", new string[] { "Karanovak" } },
                    { "Kari Sweet", new string[] { "Kari S" } },
                    { "Karina Laboom", new string[] { "Karina" } },
                    { "Karlie Montana", new string[] { "Janelle" } },
                    { "Kathleen Kruz", new string[] { "Kate" } },
                    { "Katie Jordin", new string[] { "Katiejordin" } },
                    { "Katie Kay", new string[] { "Katiek" } },
                    { "Katya Clover", new string[] { "Clover" } },
                    { "Kayla Louise", new string[] { "Kaula" } },
                    { "Kaylee Heart", new string[] { "Kaylee" } },
                    { "Keira Albina", new string[] { "Adelle" } },
                    { "Kelly Summer", new string[] { "Lindie" } },
                    { "Kennedy Kressler", new string[] { "Kennedy" } },
                    { "Kimber Lace", new string[] { "Kimber" } },
                    { "Kimberly Allure", new string[] { "Kimberly" } },
                    { "Kimberly Cox", new string[] { "Fawn" } },
                    { "Kimmie Cream", new string[] { "Kimmie" } },
                    { "Kira Lanai", new string[] { "Kiralanai" } },
                    { "Kira Zen", new string[] { "Kirra" } },
                    { "Kirsten Andrews", new string[] { "Traci" } },
                    { "Kody Kay", new string[] { "Kody" } },
                    { "Kristina Manson", new string[] { "Krystyna" } },
                    { "Kristina Rose", new string[] { "Kristinarose" } },
                    { "Kristina Rud", new string[] { "Sharon" } },
                    { "Kristina Wood", new string[] { "Kristina" } },
                    { "Krisztina Banks", new string[] { "Christina" } },
                    { "Kyra Black", new string[] { "Toni" } },
                    { "Kyra Steele", new string[] { "Kyra" } },
                    { "Lady Dee", new string[] { "Lady D" } },
                    { "Lana Violet", new string[] { "Lanaviolet" } },
                    { "Laura Crystal", new string[] { "Lauracrystal" } },
                    { "Lea Tyron", new string[] { "Lea" } },
                    { "Leah Luv", new string[] { "Leah" } },
                    { "Leighlani Red", new string[] { "Leigh" } },
                    { "Leila Smith", new string[] { "Leila" } },
                    { "Lena Nicole", new string[] { "Kaela" } },
                    { "Leony April", new string[] { "Jesica" } },
                    { "Lexi Belle", new string[] { "Lexie" } },
                    { "Lexi Diamond", new string[] { "Lexidiamond" } },
                    { "Leyla Black", new string[] { "Jenet" } },
                    { "Lia Chalizo", new string[] { "Dimitra" } },
                    { "Lilian Lee", new string[] { "Lilian" } },
                    { "Liliane Tiger", new string[] { "Liliane" } },
                    { "Lily Cute", new string[] { "Lily" } },
                    { "Lily Lake", new string[] { "Violetta" } },
                    { "Lindsay Kay", new string[] { "Grace" } },
                    { "Lindsey Olsen", new string[] { "Talya" } },
                    { "Liona Levi", new string[] { "Krissie" } },
                    { "Lisa Musa", new string[] { "Roberta" } },
                    { "Lita Phoenix", new string[] { "Sveta" } },
                    { "Little Caprice", new string[] { "Lolashut" } },
                    { "Little Rita", new string[] { "Natalya" } },
                    { "Liz Honey", new string[] { "Britney" } },
                    { "Lola Chic", new string[] { "Lilit" } },
                    { "Lolly Gartner", new string[] { "Lolly" } },
                    { "Lora Craft", new string[] { "Lora" } },
                    { "Loreen Roxx", new string[] { "Loreen" } },
                    { "Lorena Garcia", new string[] { "Lorena" } },
                    { "Louisa Lanewood", new string[] { "Lanewood" } },
                    { "Lucie Theodorova", new string[] { "Alena" } },
                    { "Lucy Ive", new string[] { "Lucy" } },
                    { "Lucy Lux", new string[] { "Lucylux" } },
                    { "Lynn Love", new string[] { "Lynnlove" } },
                    { "Lynn Pleasant", new string[] { "Lynn" } },
                    { "Mackenzee Pierce", new string[] { "Kenzie" } },
                    { "Madison Parker", new string[] { "Maddy" } },
                    { "Maggie Gold", new string[] { "Maggies" } },
                    { "Mai Ly", new string[] { "Mai" } },
                    { "Mandy Dee", new string[] { "Stacy" } },
                    { "Maria Devine", new string[] { "Quenna" } },
                    { "Marina Mae", new string[] { "Marina" } },
                    { "Marissa Mendoza", new string[] { "Marissa" } },
                    { "Marlie Moore", new string[] { "Marlie" } },
                    { "Mary Kalisy", new string[] { "Kalisy" } },
                    { "Maya Hills", new string[] { "Maya" } },
                    { "Mckenzee Miles", new string[] { "Mckenzee" } },
                    { "Meggan Mallone", new string[] { "Meggan" } },
                    { "Melanie Taylor", new string[] { "Melanie" } },
                    { "Melena Maria Rya", new string[] { "Marta" } },
                    { "Melissa Matthews", new string[] { "Melissa" } },
                    { "Mellisa Medisson", new string[] { "Barbra" } },
                    { "Melody Kush", new string[] { "Melody" } },
                    { "Merry Pie", new string[] { "Patritcy" } },
                    { "Mia Hilton", new string[] { "Miahilton" } },
                    { "Mia Me", new string[] { "Lola" } },
                    { "Mia Moon", new string[] { "Mia" } },
                    { "Micah Moore", new string[] { "Micah" } },
                    { "Michelle Brown", new string[] { "Michele" } },
                    { "Michelle Maylene", new string[] { "Michelle" } },
                    { "Michelle Moist", new string[] { "Michellemoist" } },
                    { "Michelle Myers", new string[] { "Michellemyers" } },
                    { "Miley Ann", new string[] { "Mileyann" } },
                    { "Mili Jay", new string[] { "Mili" } },
                    { "Milla Yul", new string[] { "Minnie" } },
                    { "Missy Nicole", new string[] { "Missy" } },
                    { "Molly Madison", new string[] { "Mollymadison" } },
                    { "Monica Beluchi", new string[] { "Natosha" } },
                    { "Monica Sweat", new string[] { "Marsha" } },
                    { "Monica Sweet", new string[] { "Dana" } },
                    { "Monika Cajth", new string[] { "Monique" } },
                    { "Monika Thu", new string[] { "Sassy" } },
                    { "Monika Vesela", new string[] { "Monika" } },
                    { "Monna Dark", new string[] { "Monna" } },
                    { "Nadia Taylor", new string[] { "Angelina" } },
                    { "Nadine Greenlaw", new string[] { "Zenia" } },
                    { "Nancy Ace", new string[] { "Nancy A" } },
                    { "Nancy Bell", new string[] { "Nancy" } },
                    { "Naomi Cruise", new string[] { "Tyra" } },
                    { "Natali Blond", new string[] { "Natali" } },
                    { "Natalia Forrest", new string[] { "Nataliex" } },
                    { "Nataly Gold", new string[] { "Morgan" } },
                    { "Nathaly Cherie", new string[] { "Oprah" } },
                    { "Nelli Sulivan", new string[] { "Nelly" } },
                    { "Nessa Shine", new string[] { "Agnessa" } },
                    { "Neyla Small", new string[] { "Jorden" } },
                    { "Nici Dee", new string[] { "Nici" } },
                    { "Nicole Ray", new string[] { "Nicoleray" } },
                    { "Nika Noire", new string[] { "Niki" } },
                    { "Nikita Black", new string[] { "Alyssia" } },
                    { "Nikki Chase", new string[] { "Simone" } },
                    { "Nikki Vee", new string[] { "Nikkivee" } },
                    { "Nikko Jordan", new string[] { "Eriko" } },
                    { "Niky Sweet", new string[] { "Nikysweet" } },
                    { "Nina Stevens", new string[] { "Sierra" } },
                    { "Olivia Brown", new string[] { "Nyusha" } },
                    { "Olivia La Roche", new string[] { "Olivia" } },
                    { "Oxana Chic", new string[] { "Oxana" } },
                    { "Paige Starr", new string[] { "Kellie" } },
                    { "Paris Diamond", new string[] { "Diamond" } },
                    { "Paris Parker", new string[] { "Paris" } },
                    { "Paulina James", new string[] { "Paulina" } },
                    { "Pavlina St.", new string[] { "Paula" } },
                    { "Pearl Ami", new string[] { "Edyphia" } },
                    { "Persia DeCarlo", new string[] { "Persia" } },
                    { "Petra E", new string[] { "Carmen" } },
                    { "Playful Ann", new string[] { "Anne" } },
                    { "Presley Maddox", new string[] { "Presley" } },
                    { "Rebecca Blue", new string[] { "Rebeccablue" } },
                    { "Reena Sky", new string[] { "Reena" } },
                    { "Regina Prensley", new string[] { "Evonna" } },
                    { "Renee Perez", new string[] { "Renee" } },
                    { "Rose Delight", new string[] { "Rosea" } },
                    { "Roxanna Milana", new string[] { "Taylor" } },
                    { "Roxy Carter", new string[] { "Amber" } },
                    { "Roxy Panther", new string[] { "Roxy" } },
                    { "Ruby Flame", new string[] { "Ruby" } },
                    { "Sabina Blue", new string[] { "Viva" } },
                    { "Samantha Sin", new string[] { "Heather" } },
                    { "Samantha Wow", new string[] { "Lucie" } },
                    { "Sammie Rhodes", new string[] { "Sammie" } },
                    { "Sandra Bina", new string[] { "Adri" } },
                    { "Sandra Kalermen", new string[] { "Carrie" } },
                    { "Sandra Shine", new string[] { "Sandra" } },
                    { "Sandy Joy", new string[] { "Sandy" } },
                    { "Sandy Summers", new string[] { "Sandysummers" } },
                    { "Sarah Blake", new string[] { "Sarah" } },
                    { "Sarai Keef", new string[] { "Sarai" } },
                    { "Sasha Cane", new string[] { "Sasha" } },
                    { "Sasha Rose", new string[] { "Yanka" } },
                    { "Scarlett Fay", new string[] { "Scarlettfay" } },
                    { "Scarlett Nika", new string[] { "Nikka" } },
                    { "Sera Passion", new string[] { "Sera" } },
                    { "Shrima Malati", new string[] { "Sima" } },
                    { "Shyla Jennings", new string[] { "Shyla" } },
                    { "Silvie Deluxe", new string[] { "Sylvia" } },
                    { "Smokie Flame", new string[] { "Smokie" } },
                    { "Sonya Durganova", new string[] { "Tessa" } },
                    { "Stasia", new string[] { "Aniya" } },
                    { "Stephanie Sage", new string[] { "Sage" } },
                    { "Sugar Baby", new string[] { "Bridget" } },
                    { "Summer Breeze", new string[] { "Ianisha" } },
                    { "Summer Silver", new string[] { "Summersilver" } },
                    { "Summer Solstice", new string[] { "Solstice" } },
                    { "Suzie Diamond", new string[] { "Suzie" } },
                    { "Suzy Black", new string[] { "Suze" } },
                    { "Talia Shepard", new string[] { "Victoria" } },
                    { "Tanner Mayes", new string[] { "Tannermays" } },
                    { "Tarra White", new string[] { "Martha" } },
                    { "Teena Dolly", new string[] { "Pavla" } },
                    { "Tegan Jane", new string[] { "Tegan" } },
                    { "Tereza Ilova", new string[] { "Tereza" } },
                    { "Teri Sweet", new string[] { "Teresina" } },
                    { "Tess Lyndon", new string[] { "Tess" } },
                    { "Tiffany Diamond", new string[] { "Vendula" } },
                    { "Tiffany Sweet", new string[] { "Tiff" } },
                    { "Timea Bella", new string[] { "Luciana" } },
                    { "Traci Lynn", new string[] { "Hallee" } },
                    { "Tracy Gold", new string[] { "Noleta" } },
                    { "Tyra Moon", new string[] { "Athina" } },
                    { "Valerie Herrera", new string[] { "Valerie" } },
                    { "Vanessa Monroe", new string[] { "Vanessa" } },
                    { "Veronica Hill", new string[] { "Veronicahill" } },
                    { "Veronica Jones", new string[] { "Veronica" } },
                    { "Veronique Vega", new string[] { "Veronique" } },
                    { "Victoria Puppy", new string[] { "Victoria P" } },
                    { "Victoria Sweet", new string[] { "Victoriasweet" } },
                    { "Vika Lita", new string[] { "Viera" } },
                    { "Violette Pink", new string[] { "Violette" } },
                    { "Whitney Conroy", new string[] { "Whitney" } },
                    { "Yasmine Gold", new string[] { "Rachel" } },
                    { "Yulia Bright", new string[] { "Angie" } },
                    { "Zazie Sky", new string[] { "Zazie" } },
                    { "Zeina Heart", new string[] { "Zeina" } },
                    { "Zena Little", new string[] { "Xenia" } },
                    { "Zuzana Z", new string[] { "Jujana" } },
                }
            },{
                9, new Dictionary<string, string[]> {
                    { "Bailey Brooke", new string[] { "Bailey Brookes" } },
                }
            },{
                10, new Dictionary<string, string[]> {
                    { "Ada Sanchez", new string[] { "Ada S" } },
                    { "Adelle Booty", new string[] { "Parvin" } },
                    { "Alektra Sky", new string[] { "Darla" } },
                    { "Alice Marshall", new string[] { "Dunya" } },
                    { "Alice Smack", new string[] { "Mackenzie" } },
                    { "Alison Faye", new string[] { "Kendall" } },
                    { "Ananta Shakti", new string[] { "Lusil" } },
                    { "Anita Sparkle", new string[] { "Rebecca" } },
                    { "Anna Taylor", new string[] { "Madelyn" } },
                    { "Ariadna Moon", new string[] { "Ariadna" } },
                    { "Arian Joy", new string[] { "Dinara" } },
                    { "Ariel Rose", new string[] { "Ariel R" } },
                    { "Arika Foxx", new string[] { "Arika" } },
                    { "Aruna Aghora", new string[] { "Zarina" } },
                    { "Aubree Jade", new string[] { "Mariah" } },
                    { "Autumn Viviana", new string[] { "Viviana" } },
                    { "Ava Dalush", new string[] { "Ava" } },
                    { "Bella Rossi", new string[] { "Bella" } },
                    { "Bonnie Shai", new string[] { "Viki" } },
                    { "Callie Nicole", new string[] { "Callie" } },
                    { "Camila", new string[] { "Betty", "Jane" } },
                    { "Candy C", new string[] { "Carre" } },
                    { "Carol Miller", new string[] { "Serena" } },
                    { "Casi James", new string[] { "Casi J" } },
                    { "Chloe Blue", new string[] { "Ema" } },
                    { "Chloe Couture", new string[] { "Chloe Cherry" } },
                    { "Diana Dali", new string[] { "Avery" } },
                    { "Dominica Phoenix", new string[] { "Petra" } },
                    { "Emma Brown", new string[] { "Ariana" } },
                    { "Erika Bellucci", new string[] { "Gerta" } },
                    { "Grace C", new string[] { "Sophia" } },
                    { "Inga E", new string[] { "Colette" } },
                    { "Izi", new string[] { "Gabi" } },
                    { "Janna", new string[] { "Jana" } },
                    { "Jay Dee", new string[] { "Lada" } },
                    { "Jessi Gold", new string[] { "Catania" } },
                    { "Joanna Pret", new string[] { "Joanna" } },
                    { "Kajira Bound", new string[] { "Kajira" } },
                    { "Karina Grand", new string[] { "Madison" } },
                    { "Kate G.", new string[] { "Rosanna" } },
                    { "Katie Cummings", new string[] { "Katie C" } },
                    { "Katie Kay", new string[] { "Katie K" } },
                    { "Kortny", new string[] { "Kail" } },
                    { "Krista Evans", new string[] { "Krista" } },
                    { "Kristall Rush", new string[] { "Erica", "Sasha" } },
                    { "Lena Love", new string[] { "Bailey" } },
                    { "Lilu Tattoo", new string[] { "Vilia" } },
                    { "Lily Labeau", new string[] { "Lily L" } },
                    { "Liona Levi", new string[] { "Zoi" } },
                    { "Lisa C", new string[] { "Janette" } },
                    { "Lisa Smiles", new string[] { "Argentina" } },
                    { "Lola Taylor", new string[] { "Tori" } },
                    { "Mai Ly", new string[] { "Mai" } },
                    { "Margarita C", new string[] { "Peachy" } },
                    { "Marina Visconti", new string[] { "Sheila" } },
                    { "Mariya C", new string[] { "Fantina", "Olga" } },
                    { "Marly Romero", new string[] { "Mara" } },
                    { "Mia Reese", new string[] { "Eva" } },
                    { "Mila Beth", new string[] { "Mina" } },
                    { "Milana Blanc", new string[] { "Briana", "Brianna" } },
                    { "Milana Fox", new string[] { "Hannah" } },
                    { "Milla Yul", new string[] { "Yanie" } },
                    { "Miranda Deen", new string[] { "Veiki" } },
                    { "Nadia Bella", new string[] { "Nadya", "Seren" } },
                    { "Netta", new string[] { "Jade" } },
                    { "Nicoline", new string[] { "Anfisa" } },
                    { "Pola Sunshine", new string[] { "Kimberly" } },
                    { "Rahyndee James", new string[] { "Rahyndee" } },
                    { "Rebeca Taylor", new string[] { "Jordan" } },
                    { "Riley Jensen", new string[] { "Riley J" } },
                    { "Rima", new string[] { "Aliya" } },
                    { "Rita Rush", new string[] { "Luna" } },
                    { "Sabrina Moor", new string[] { "Nika" } },
                    { "Sadine Godiva", new string[] { "Sadine G" } },
                    { "Sandra Luberc", new string[] { "Kameya" } },
                    { "Sarai Keef", new string[] { "Sarai" } },
                    { "Selena Stuart", new string[] { "Katherine" } },
                    { "Sheri Vi", new string[] { "Nora R" } },
                    { "Sierra Sanders", new string[] { "Sierra" } },
                    { "Skye West", new string[] { "Skye" } },
                    { "Soliel Marks", new string[] { "Soleil" } },
                    { "Stella Banxxx", new string[] { "Stella" } },
                    { "Sunny Rise", new string[] { "Avina" } },
                    { "Taissia Shanti", new string[] { "Magda" } },
                    { "Valentina Cross", new string[] { "Artemida" } },
                }
            },{
                11, new Dictionary<string, string[]> {
                    { "Lena Anderson", new string[] { "Blaire Ivory" } },
                }
            },{
                12, new Dictionary<string, string[]> {
                    { "AJ Applegate", new string[] { "Danielle" } },
                    { "Aaliyah Love", new string[] { "Aliyah" } },
                    { "Abigaile Johnson", new string[] { "Abby" } },
                    { "Adel Morel", new string[] { "Adel M" } },
                    { "Adele Sunshine", new string[] { "Sunshine" } },
                    { "Adria Rae", new string[] { "Adria" } },
                    { "Adriana Chechik", new string[] { "Adriana" } },
                    { "Aidra Fox", new string[] { "Aidra" } },
                    { "Aika May", new string[] { "Aika" } },
                    { "Alecia Fox", new string[] { "Alecia" } },
                    { "Aleksa Slusarchi", new string[] { "Jessica" } },
                    { "Alessandra Jane", new string[] { "Aj" } },
                    { "Alexa Grace", new string[] { "Alexa" } },
                    { "Alexa Tomas", new string[] { "Alina" } },
                    { "Alexis Adams", new string[] { "Alexes" } },
                    { "Alexis Crystal", new string[] { "Carrie" } },
                    { "Alexis Love", new string[] { "Alexis" } },
                    { "Alicia A", new string[] { "Jenny" } },
                    { "Allie Haze", new string[] { "Allie" } },
                    { "Allison", new string[] { "Kristi" } },
                    { "Alyssa Branch", new string[] { "Sam" } },
                    { "Alyssia Kent", new string[] { "Alyssia" } },
                    { "Amarna Miller", new string[] { "Serena" } },
                    { "Ana Foxxx", new string[] { "Ana" } },
                    { "Anastasia Morna", new string[] { "Anna M" } },
                    { "Anetta V.", new string[] { "Veronika" } },
                    { "Angel Piaff", new string[] { "Adel" } },
                    { "Angelic Anya", new string[] { "Patsy" } },
                    { "Angelica Kitten", new string[] { "Angie" } },
                    { "Anita Bellini Berlusconi", new string[] { "Anita" } },
                    { "Anjelica", new string[] { "Angelica" } },
                    { "Anna Rose", new string[] { "Maria" } },
                    { "Annika Albrite", new string[] { "Anikka" } },
                    { "Antonia Sainz", new string[] { "Katerina" } },
                    { "Anya Olsen", new string[] { "Anya" } },
                    { "Ariana Marie", new string[] { "Arianna" } },
                    { "Ariel Piper Fawn", new string[] { "Ariel" } },
                    { "Ariel", new string[] { "Lillianne" } },
                    { "Artemis", new string[] { "Reina" } },
                    { "Ashlyn Molloy", new string[] { "Ashley S" } },
                    { "Aubrey Star", new string[] { "Aubrey" } },
                    { "Avril Hall", new string[] { "Avril" } },
                    { "Bailey Ryder", new string[] { "Bailey" } },
                    { "Barbamiska", new string[] { "Stevie" } },
                    { "Beata Undine", new string[] { "Beatrice" } },
                    { "Belle Knox", new string[] { "Belle" } },
                    { "Bethany", new string[] { "Crystal" } },
                    { "Blanche Bradburry", new string[] { "Barbie" } },
                    { "Blue Angel", new string[] { "Leila" } },
                    { "Brea Bennett", new string[] { "Breanne" } },
                    { "Bree Daniels", new string[] { "Bree" } },
                    { "Bridgit A", new string[] { "Becky" } },
                    { "Brooklyn Lee", new string[] { "Brooklyn" } },
                    { "Brynn Tyler", new string[] { "Brynn" } },
                    { "Candice Luca", new string[] { "Kaylee" } },
                    { "Capri Anderson", new string[] { "Capri" } },
                    { "Carla Cox", new string[] { "Carla" } },
                    { "Cassandra Nix", new string[] { "Emily" } },
                    { "Cassidey Rae", new string[] { "Cassidy" } },
                    { "Cassie Laine", new string[] { "Cassie" } },
                    { "Catie Parker", new string[] { "Catie" } },
                    { "Catina", new string[] { "Sasha D" } },
                    { "Cayla Lyons", new string[] { "Cayla", "Hannah" } },
                    { "Celine", new string[] { "Kaye" } },
                    { "Charity Crawford", new string[] { "Charity" } },
                    { "Charlie Red", new string[] { "Charlie" } },
                    { "Charlotte Stokely", new string[] { "Charlotte" } },
                    { "Cherry Kiss", new string[] { "Leony" } },
                    { "Chloe Amour", new string[] { "Amelie" } },
                    { "Chloe Foster", new string[] { "Bunny" } },
                    { "Chloe Lynn", new string[] { "Chloelynn" } },
                    { "Christine Paradise", new string[] { "Christine" } },
                    { "Connie Carter", new string[] { "Connie" } },
                    { "Dakoda Brookes", new string[] { "Riley" } },
                    { "Daphne Klyde", new string[] { "Daphne" } },
                    { "Davina Davis", new string[] { "Addison C" } },
                    { "Deina", new string[] { "Stacy" } },
                    { "Diana Fox", new string[] { "Diana" } },
                    { "Diana G", new string[] { "Mira" } },
                    { "Dido Angel", new string[] { "Susie" } },
                    { "Dillion Harper", new string[] { "Dillion" } },
                    { "Dominic Anna", new string[] { "Dominica" } },
                    { "Dominika C.", new string[] { "Dominique" } },
                    { "Eileen Sue", new string[] { "Stefanie" } },
                    { "Elisa", new string[] { "Liv" } },
                    { "Elle Alexandra", new string[] { "Elle" } },
                    { "Ellena Woods", new string[] { "Jocelyn" } },
                    { "Elouisa", new string[] { "Natali" } },
                    { "Emily Brix", new string[] { "Emily B" } },
                    { "Emily Grey", new string[] { "Emilie" } },
                    { "Emma Mae", new string[] { "Emma" } },
                    { "Erica Fontes", new string[] { "Erica" } },
                    { "Eufrat Mai", new string[] { "Eufrat" } },
                    { "Eveline Dellai", new string[] { "Eveline" } },
                    { "Faye Reagan", new string[] { "Faye" } },
                    { "Ferrera Gomez", new string[] { "Katka" } },
                    { "Foxxi Black", new string[] { "Foxi" } },
                    { "Foxy Di", new string[] { "Kate" } },
                    { "Franziska Facella", new string[] { "Francesca" } },
                    { "Georgia Jones", new string[] { "Georgia" } },
                    { "Gigi Allens", new string[] { "GiiGi" } },
                    { "Gigi Labonne", new string[] { "Gigi" } },
                    { "Gigi Rivera", new string[] { "Gigi R" } },
                    { "Gina Devine", new string[] { "Jasmine" } },
                    { "Gina Gerson", new string[] { "Gina" } },
                    { "Gina", new string[] { "Nicole" } },
                    { "Gwen", new string[] { "Anna" } },
                    { "Hannah Hawthorne", new string[] { "Hanna" } },
                    { "Hannah Hays", new string[] { "Rainbow" } },
                    { "Hayden Hawkens", new string[] { "Hayden H" } },
                    { "Hayden Winters", new string[] { "Hayden" } },
                    { "Heather Carolin", new string[] { "Scarlett" } },
                    { "Heather Night", new string[] { "Tina" } },
                    { "Heather Starlet", new string[] { "Ruby" } },
                    { "Heidi Romanova", new string[] { "Heidi R" } },
                    { "Henessy", new string[] { "Alina H" } },
                    { "Irina J", new string[] { "Michelle" } },
                    { "Irina K", new string[] { "Stasha" } },
                    { "Isabel B", new string[] { "Oliya" } },
                    { "Ivana Sugar", new string[] { "Eve A" } },
                    { "Iwia", new string[] { "Ivy" } },
                    { "Izabelle A", new string[] { "Sandy" } },
                    { "Izzy Delphine", new string[] { "Izzy" } },
                    { "Jade Baker", new string[] { "Jade" } },
                    { "Jana Mrhacova", new string[] { "Willow" } },
                    { "Janetta", new string[] { "Eve" } },
                    { "Janny Manson", new string[] { "Jenny M" } },
                    { "Jaslene Jade", new string[] { "Natalie" } },
                    { "Jayden Taylors", new string[] { "Jayden" } },
                    { "Jenna Ross", new string[] { "Jenna" } },
                    { "Jenni Czech", new string[] { "Jenni" } },
                    { "Jericha Jem", new string[] { "Jericha" } },
                    { "Jessica Rox", new string[] { "Kristen" } },
                    { "Jessie Andrews", new string[] { "Jessie" } },
                    { "Jessie Rogers", new string[] { "Carmen" } },
                    { "Jillian Janson", new string[] { "Jillian" } },
                    { "Joleyn Burst", new string[] { "Katia" } },
                    { "Joseline Kelly", new string[] { "Joseline" } },
                    { "Julia I", new string[] { "Jewel" } },
                    { "Kalea Taylor", new string[] { "Laura" } },
                    { "Kamila", new string[] { "Heather" } },
                    { "Kari Sweet", new string[] { "Katherine" } },
                    { "Karina White", new string[] { "Karina" } },
                    { "Karina", new string[] { "Baby" } },
                    { "Kasey Chase", new string[] { "Carlie" } },
                    { "Kassondra Raine", new string[] { "Kassondra" } },
                    { "Katerina", new string[] { "Mikah" } },
                    { "Katie Oliver", new string[] { "Gabriella" } },
                    { "Katy Jayne", new string[] { "Katie Jayne" } },
                    { "Katy Rose", new string[] { "Kim" } },
                    { "Katya Clover", new string[] { "Clover" } },
                    { "Keira Albina", new string[] { "Keira" } },
                    { "Kelly E", new string[] { "Natasha B" } },
                    { "Kendall White", new string[] { "Kendall" } },
                    { "Kenna James", new string[] { "Kenna" } },
                    { "Kennedy Kressler", new string[] { "Kennedy" } },
                    { "Kenze Thomas", new string[] { "Kenzie" } },
                    { "Kiara Lord", new string[] { "Alena" } },
                    { "Kiera Winters", new string[] { "Kiera" } },
                    { "Kimberly Kato", new string[] { "Kato" } },
                    { "Kimmy Granger", new string[] { "Kimmy" } },
                    { "Kinsley Ann", new string[] { "Kinsley" } },
                    { "Kira Thorn", new string[] { "Kira" } },
                    { "Kira Zen", new string[] { "Kirra" } },
                    { "Kirsten Nicole Lee", new string[] { "Kirsten Lee" } },
                    { "Kitty Jane", new string[] { "Kitty" } },
                    { "Kristen Scott", new string[] { "Kristin Scott" } },
                    { "Kya", new string[] { "Katy" } },
                    { "Kylie Nicole", new string[] { "Kylie" } },
                    { "Lady Dee", new string[] { "Dina" } },
                    { "Lauren Crist", new string[] { "Lisa" } },
                    { "Leila Smith", new string[] { "Janie" } },
                    { "Lena Anderson", new string[] { "Lena" } },
                    { "Lena Love", new string[] { "Pam" } },
                    { "Lena", new string[] { "Tasha" } },
                    { "Leo", new string[] { "Tatiana" } },
                    { "Leonie", new string[] { "Holly" } },
                    { "Lexi Belle", new string[] { "Lexi" } },
                    { "Lexi Dona", new string[] { "Carrol" } },
                    { "Lexi Foxy", new string[] { "Nella" } },
                    { "Lexi Layo", new string[] { "Lexy" } },
                    { "Lexie Fox", new string[] { "Lolita" } },
                    { "Lia Lor", new string[] { "Lia" } },
                    { "Lilit Sweet", new string[] { "Lilit" } },
                    { "Lilith Lee", new string[] { "Sweetie" } },
                    { "Lilu Moon", new string[] { "Lilu" } },
                    { "Lily Ivy ", new string[] { "Lilly Ivy" } },
                    { "Lily Labeau", new string[] { "Lilly" } },
                    { "Lindsey Olsen", new string[] { "Nastia" } },
                    { "Lisa Dawn", new string[] { "Liza Dawn" } },
                    { "Little Caprice", new string[] { "Caprice" } },
                    { "Livia Godiva", new string[] { "Olivia" } },
                    { "Lorena Garcia", new string[] { "Lorena" } },
                    { "Lovita Fate", new string[] { "Lovita" } },
                    { "Lucy Li", new string[] { "Teal" } },
                    { "Lynette", new string[] { "Maya" } },
                    { "Lyra Louvel", new string[] { "Lyra" } },
                    { "Mae Olsen", new string[] { "Anais" } },
                    { "Malena Morgan", new string[] { "Malena" } },
                    { "Marica Hase", new string[] { "Marica" } },
                    { "Marie McCray", new string[] { "Marie M." } },
                    { "Marry Queen", new string[] { "Mary" } },
                    { "Maryjane Johnson", new string[] { "Maryjane" } },
                    { "Masha D.", new string[] { "Jackie" } },
                    { "Megan Promesita", new string[] { "Ivana" } },
                    { "Melanie Rios", new string[] { "Melanie" } },
                    { "Melena Maria Rya", new string[] { "Malena A" } },
                    { "Mia Hilton", new string[] { "Lana" } },
                    { "Mia Lina", new string[] { "Mia" } },
                    { "Mia Malkova", new string[] { "Mia M" } },
                    { "Mia Manarote", new string[] { "Addison" } },
                    { "Michaela Isizzu", new string[] { "Mila K" } },
                    { "Mila Azul", new string[] { "Milla" } },
                    { "Monika Benz", new string[] { "Anastasia", "Monika" } },
                    { "Monika Thu", new string[] { "Monique" } },
                    { "Nadia Nickels", new string[] { "Nadia" } },
                    { "Nadine", new string[] { "Tess" } },
                    { "Nancy Ace", new string[] { "Nancy" } },
                    { "Naomi Bennet", new string[] { "Naomi B" } },
                    { "Naomi Nevena", new string[] { "Lily" } },
                    { "Natali Blond", new string[] { "Megan" } },
                    { "Nataly Gold", new string[] { "Linsay" } },
                    { "Nedda A", new string[] { "Miss Pac Man" } },
                    { "Nelly A", new string[] { "Chantal" } },
                    { "Nessa Devil", new string[] { "Katrina" } },
                    { "Nici Dee", new string[] { "Nikki" } },
                    { "Nika", new string[] { "Nicola" } },
                    { "Nikki Fox", new string[] { "Niki" } },
                    { "Nikki Peach", new string[] { "Nikki Peaches" } },
                    { "Nikki Stills", new string[] { "Nastya" } },
                    { "Nina James", new string[] { "Nina" } },
                    { "Olivia Grace", new string[] { "Bambi" } },
                    { "Paige Owens", new string[] { "Paige" } },
                    { "Paloma B", new string[] { "Reese" } },
                    { "Paula Shy", new string[] { "Misty" } },
                    { "Penelope Lynn", new string[] { "Lola" } },
                    { "Pinky June", new string[] { "Anneli" } },
                    { "Presley Hart", new string[] { "Presley" } },
                    { "Rebecca Volpetti", new string[] { "Rebecca" } },
                    { "Red Fox", new string[] { "The Red Fox" } },
                    { "Ria Sunn", new string[] { "Ria Sun" } },
                    { "Rima B", new string[] { "Ellie" } },
                    { "Rosemary Radeva", new string[] { "Angel" } },
                    { "Sabrisse", new string[] { "Miu" } },
                    { "Samantha Heat", new string[] { "Annemarie" } },
                    { "Samantha Jolie", new string[] { "Samantha" } },
                    { "Samantha Rone", new string[] { "Sammy" } },
                    { "Sapphira A", new string[] { "Maya M" } },
                    { "Sarka", new string[] { "Tabitha" } },
                    { "Sasha Grey", new string[] { "Sasha" } },
                    { "Satin Bloom", new string[] { "Marie" } },
                    { "Scyley Jam", new string[] { "Viktoria" } },
                    { "Shrima Malati", new string[] { "Shrima" } },
                    { "Sicilia", new string[] { "Cecilia" } },
                    { "Silvie Deluxe", new string[] { "Silvie" } },
                    { "Silvie Luca", new string[] { "Naomi" } },
                    { "Sindy Vega", new string[] { "Cindy" } },
                    { "Sinovia", new string[] { "Tracy" } },
                    { "Sophia Fiore", new string[] { "Sophia" } },
                    { "Sophia Knight", new string[] { "Sophie" } },
                    { "Stacy Cruz", new string[] { "Cornelia" } },
                    { "Stefanie", new string[] { "Stephanie" } },
                    { "Summer Breeze", new string[] { "Corinne" } },
                    { "Sunny A", new string[] { "Aria" } },
                    { "Susan Ayn", new string[] { "Paulina" } },
                    { "Suzie Carina", new string[] { "Suzie C" } },
                    { "Sybil A", new string[] { "Sybil" } },
                    { "Talia Mint", new string[] { "Talia" } },
                    { "Talia Shepard", new string[] { "Kat" } },
                    { "Taylor Sands", new string[] { "Heidi" } },
                    { "Teena Dolly", new string[] { "Grace" } },
                    { "Tiffany Fox", new string[] { "Tiffany F" } },
                    { "Tiffany Thompson", new string[] { "Tiffany" } },
                    { "Tori Black", new string[] { "Tori" } },
                    { "Tracy Lindsay", new string[] { "Summer" } },
                    { "Tracy Smile", new string[] { "Julie" } },
                    { "Vanessa", new string[] { "Casey" } },
                    { "Vanna Bardot", new string[] { "Vanna" } },
                    { "Veronica Clark", new string[] { "Veronica" } },
                    { "Veronica Radke", new string[] { "Scarlet" } },
                    { "Vicki Chase", new string[] { "Vicky" } },
                    { "Vicky Love", new string[] { "Vicki" } },
                    { "Victoria Blaze", new string[] { "Gianna" } },
                    { "Victoria Lynn", new string[] { "Chelsea" } },
                    { "Victoria Puppy", new string[] { "Bea", "Poppy" } },
                    { "Victoria Rae Black", new string[] { "Victoria" } },
                    { "Victoria Sweet", new string[] { "Chloe" } },
                    { "Vika T", new string[] { "Leah" } },
                    { "Vinna Reed", new string[] { "Vinna" } },
                    { "Violette Pink", new string[] { "Pink Violet" } },
                    { "Xandra B", new string[] { "Tara" } },
                    { "Zazie Sky", new string[] { "Zazie" } },
                    { "Zena Little", new string[] { "Sandra" } },
                    { "Zoe", new string[] { "Klara" } },
                    { "Zoey Kush", new string[] { "Star" } },
                }
            },{
                13, new Dictionary<string, string[]> {
                    { "Alena H", new string[] { "Helen" } },
                    { "Goldie Baby", new string[] { "Ms White-Kitten" } },
                }
            },{
                14, new Dictionary<string, string[]> {
                    { "Morgan Layne", new string[] { "Morgan" } },
                }
            },{
                15, new Dictionary<string, string[]> {
                    { "Katya Clover", new string[] { "Clover" } },
                }
            },{
                16, new Dictionary<string, string[]> {
                    { "Lola Taylor", new string[] { "Lolita Taylor" } },
                }
            },{
                17, new Dictionary<string, string[]> {
                    { "Susan Ayn", new string[] { "Susan Ayne" } },
                }
            },{
                18, new Dictionary<string, string[]> {
                    { "Luna Corazon", new string[] { "Luna Corazón" } },
                }
            },{
                19, new Dictionary<string, string[]> {
                    { "London Keyes", new string[] { "London Keys" } },
                }
            },{
                20, new Dictionary<string, string[]> {
                    { "Gabriella Lati", new string[] { "Gabrielle Lati" } },
                }
            }
        };
    }
}
