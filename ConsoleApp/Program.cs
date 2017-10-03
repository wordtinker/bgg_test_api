using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Net;
using System.Xml.Linq;

namespace ConsoleApp
{

    public static class FilterExtensions
    {
        public static void FilterGames(this XDocument doc)
        {
            string totalItems = doc.Root.Attribute("totalitems").Value;
            int x = 0;
            int.TryParse(totalItems, out x);
            Console.WriteLine("Found {0} games:", x);

            var games = from node in doc.Root.Elements("item")
                        select new
                        {
                            Name = node.Value,
                            Id = node.Attribute("objectid").Value,
                            Rating = node.Descendants("rating").Single().Attribute("value").Value
                        };
            foreach (var item in games)
            {
                Console.WriteLine("\t {0}({1}) : {2}", item.Name, item.Id, item.Rating);
            }
        }
        public static void FilterNames(this XDocument doc)
        {
            string totalItems = doc.Root.Descendants("comments").Single().Attribute("totalitems").Value;
            Console.WriteLine("Found {0} ratings.", totalItems);

            var users = from node in doc.Root.Descendants("comment")
                        select new
                        {
                            Name = node.Attribute("username").Value,
                            Rating = node.Attribute("rating").Value
                        };
            string lastRating = users.Last().Rating;
            Console.WriteLine("Last seen rating: {0}", lastRating);
        }

    }

    class Program
    {
        static async Task<XDocument> GetXMLFrom(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                do
                {
                    using (HttpResponseMessage response = await client.GetAsync(uri))
                    {
                        // wait before asking for result again
                        await Task.Delay(500);
                        // throw on errors
                        response.EnsureSuccessStatusCode();

                        Console.WriteLine(response.StatusCode);
                        if (response.StatusCode != HttpStatusCode.OK) continue;

                        // Gather results
                        using (HttpContent content = response.Content)
                        {
                            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                            XDocument doc;
                            using (MemoryStream ms = new MemoryStream(bytes))
                            {
                                doc = XDocument.Load(ms);
                            }
                            return doc;
                        }
                    }
                } while (true);
            }
        }

        static void GetGames()
        {
            Console.WriteLine("Username: ?");
            string username = Console.ReadLine();
            Console.WriteLine("Min rating: ?");
            string minrating = Console.ReadLine();
            Console.WriteLine("Max rating: ?");
            string rating = Console.ReadLine();

            string baseURI = "https://www.boardgamegeek.com/xmlapi2/collection?username={0}&subtype=boardgame&excludesubtype=boardgameexpansion&rated=1&stats=1&brief=1&minrating={1}&rating={2}";

            try
            {
                string URI = string.Format(baseURI, username, minrating, rating);
                Task<XDocument> task = GetXMLFrom(URI);
                task.Wait();
                XDocument doc = task.Result;
                doc.FilterGames();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        static void GetNames()
        {
            Console.WriteLine("Game Id: ?");
            string gameId = Console.ReadLine();
            //Console.WriteLine("Min rating: ?");
            //string minrating = Console.ReadLine();
            //Console.WriteLine("Max rating: ?");
            //string rating = Console.ReadLine();

            string baseURI = "https://www.boardgamegeek.com/xmlapi2/thing?type=boardgame&id={0}&ratingcomments=1&page={1}&pagesize=100";
            int page = 1; // TODO

            try
            {
                string URI = string.Format(baseURI, gameId, page);
                Task<XDocument> task = GetXMLFrom(URI);
                task.Wait();
                XDocument doc = task.Result;
                doc.FilterNames();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("1 - Get Games. 2 - Get Users");
            switch (Console.ReadLine())
            {
                case "1":
                    GetGames();
                    break;
                case "2":
                    GetNames();
                    break;
                default:
                    break;
            }
        }
    }
}
