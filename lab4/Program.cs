using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using HtmlAgilityPack;
using Telegram.Bot;
using Telegram.Bot.Args;
using Timer = System.Timers.Timer;


namespace lab4
{
    internal partial class Program
    {
        private static void Main()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            var unused = new HtmlWeb
            {
                PreRequest = OnPreRequest
            };

            var bot = new TelegramBotClient("1743657186:AAEyqLtqL95eKUZANc0hefOsySyWgcz7dVc");
            bot.StartReceiving();

            bot.OnMessage += Bot_OnMessage;

            Console.ReadLine();

            bot.StopReceiving();
        }
        
        private static void SetTimer(MessageEventArgs e)
        {
            _users[e.Message.Chat.Id].STimer = new Timer(Constants.Interval5Min) {AutoReset = true, Enabled = false};
        }

        private static bool OnPreRequest(HttpWebRequest request)
        {
            request.AllowAutoRedirect = true;
            return true;
        }

        private static void Add_User(MessageEventArgs e)
        {
            if (_users.ContainsKey(e.Message.Chat.Id) == false)
            {
                _users.Add(e.Message.Chat.Id, new User());
                SetTimer(e);
            }
        }
        private static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var bot = sender as TelegramBotClient;
            var userMessWord = e.Message.Text.Split(new[] {" "},
                StringSplitOptions.RemoveEmptyEntries);

            Add_User(e);

            var command = CommandFactory.Get(userMessWord[0]);
            command.Process(bot, e);
        }

        private static void Share_Info(TelegramBotClient bot, MessageEventArgs e, int messType = Constants.ExtendedMess,
            string shareCode = null, Share share = null)
        {
            var printcommand = CommandFactory.Get(messType);

            var doc = Share.GetUrl(e, shareCode);
            var elements = Share.ParseDoc(doc);
            if (elements.Count == 0)
            {
                bot?.SendTextMessageAsync(e.Message.Chat.Id, "Wrong share name");
                return;
            }


            if (share == null)
            {
                share = new Share();
                share.ParsEl(elements);
            }
            else
            {
                var compshare = new Share();
                compshare.ParsEl(elements);
                share.ShareComparison(compshare);
            }

            printcommand.Process(bot, e, share);
        }

        private static bool Add_Share(string shareCode, MessageEventArgs e)
        {
            _users[e.Message.Chat.Id].AddedShares.Add(new Share());
            _users[e.Message.Chat.Id].AddedShares[_users[e.Message.Chat.Id].SharesQuant].Name = shareCode;
            _users[e.Message.Chat.Id].AddedShares[_users[e.Message.Chat.Id].SharesQuant].Cost = null;
            _users[e.Message.Chat.Id].SharesQuant += 1;
            _users[e.Message.Chat.Id].AddedShares = _users[e.Message.Chat.Id].AddedShares.ToList();
            return true;
        }

        private static bool Add_Several_Shares(string[] shareCodes, MessageEventArgs e)
        {
            for (var i = 1; i < shareCodes.Length; i += 1)
                if (!Add_Share(shareCodes[i], e))
                    return false;
            return true;
        }

        private static bool Del_Share(string shareCode, MessageEventArgs e)
        {
            foreach (var share in _users[e.Message.Chat.Id].AddedShares.ToList())
                if (share.Name == shareCode)
                {
                    _users[e.Message.Chat.Id].AddedShares.Remove(share);
                    _users[e.Message.Chat.Id].SharesQuant -= 1;
                }

            return true;
        }

        private static bool Del_Several_Shares(string[] shareCodes, MessageEventArgs e)
        {
            for (var i = 1; i < shareCodes.Length; i += 1)
                if (!Del_Share(shareCodes[i], e))
                    return false;
            _users[e.Message.Chat.Id].AddedShares = _users[e.Message.Chat.Id].AddedShares.ToList();
            return true;
        }

        private static bool List_Share(TelegramBotClient sender, MessageEventArgs e)
        {
            foreach (var share in _users[e.Message.Chat.Id].AddedShares) Share_Info(sender, e, Constants.ShortMess, share.Name);

            return _users[e.Message.Chat.Id].SharesQuant != -1;
        }

        private static void Start_Checking(TelegramBotClient bot, MessageEventArgs e)
        {
            _users[e.Message.Chat.Id].STimer.Enabled = true;
            _users[e.Message.Chat.Id].STimer.Elapsed += (tSender, tE) =>
            {
                foreach (var share in _users[e.Message.Chat.Id].AddedShares.ToList())
                {
                    Share_Info(bot, e, Constants.EventMess, share.Name, share);
                }

            };
        }

        private class Share
        {
            private sealed class NameCostEqualityComparer : IEqualityComparer<Share>
            {
                public bool Equals(Share x, Share y)
                {
                    if (ReferenceEquals(x, y)) return true;
                    if (ReferenceEquals(x, null)) return false;
                    if (ReferenceEquals(y, null)) return false;
                    if (x.GetType() != y.GetType()) return false;
                    return x.Name == y.Name && x.Cost == y.Cost;
                }

                public int GetHashCode(Share obj)
                {
                    return HashCode.Combine(obj.Name, obj.Cost);
                }
            }

            public static IEqualityComparer<Share> NameCostComparer { get; } = new NameCostEqualityComparer();

            public string Name { get; set; }
            public string Cost { get; set; }
            public string Val { get; set; }
            public string Opened { get; set; }
            public string Closed { get; set; }
            public string Costdif { get; set; }

            public static HtmlDocument GetUrl(MessageEventArgs e, string shareCode)
            {
                var url = "https://finance.yahoo.com/quote/";
                var userMessWord = e.Message.Text.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
                var count = userMessWord.Length;

                if (shareCode == null) shareCode = count > 1 ? userMessWord[1] : userMessWord[0];

                url = url + shareCode + '/';


                var handler = new HttpClientHandler {AllowAutoRedirect = true};
                var httpClient = new HttpClient(handler);
                var response = httpClient.GetAsync(url).Result;
                Console.WriteLine(response);


                var redirectUri = response.RequestMessage.RequestUri;

                var web = new HtmlWeb();
                var doc = web.Load(redirectUri);
                return doc;
            }

            public static List<HtmlNode> ParseDoc(HtmlDocument doc)
            {
                var elements = doc.DocumentNode.Descendants("div")
                    .Where(x => x.Attributes["class"] != null)
                    .Where(x => x.Attributes["class"].Value == "D(ib) Mend(20px)" ||
                                x.Attributes["class"].Value == "D(ib) " ||
                                x.Attributes["class"].Value == "C($tertiaryColor) Fz(12px)").ToList();
                var elements2 = doc.DocumentNode.Descendants("span")
                    .Where(x => x.Attributes["class"] != null)
                    .Where(x => x.Attributes["class"].Value == "Trsdu(0.3s) " &&
                                (x.Attributes["data-reactid"].Value == "44" ||
                                 x.Attributes["data-reactid"].Value == "49")).ToList();
                elements.Add(elements2[0]);
                elements.Add(elements2[1]);
                return elements;
            }

            public void ParsEl(List<HtmlNode> elements)
            {
                var descHtml = elements.SelectMany(x => x.Descendants("h1"))
                    .Where(x => x.Attributes["class"] != null)
                    .FirstOrDefault(x => x.Attributes["class"].Value == "D(ib) Fz(18px)");
                if (descHtml?.InnerText != null)
                    Name = descHtml.InnerText;

                descHtml = elements.SelectMany(x => x.Descendants("span"))
                    .Where(x => x.Attributes["class"] != null)
                    .FirstOrDefault(x => x.Attributes["class"].Value == "Trsdu(0.3s) Fw(b) Fz(36px) Mb(-4px) D(ib)");
                if (descHtml?.InnerText != null)
                    Cost = Math.Round(double.Parse(descHtml.InnerText[Range.EndAt(descHtml.InnerText.Length)]), 2)
                        .ToString(CultureInfo.InvariantCulture);

                descHtml = elements.SelectMany(x => x.DescendantsAndSelf("div"))
                    .Where(x => x.Attributes["class"] != null)
                    .FirstOrDefault(x => x.Attributes["class"].Value == "C($tertiaryColor) Fz(12px)");
                if (descHtml?.InnerText != null)
                {
                    var sharePlat = descHtml.InnerText.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
                    Val = sharePlat[^1]; // Takes last word in string, usual that is "USD"
                }

                descHtml = elements[3];
                Closed = descHtml.InnerText;

                descHtml = elements[4];
                Opened = descHtml.InnerText;
            }

            public void ShareComparison(Share compShare)
            {
                if (Cost == null)
                {
                    Costdif = "0.00";
                    Val = compShare.Val;
                }

                else
                    Costdif = (Math.Round(double.Parse(Cost)
                                          - double.Parse(compShare.Cost)), 2).ToString();

                Cost = compShare.Cost;
                Name = compShare.Name;
            }


        }

        private static Dictionary<long, User> _users = new Dictionary<long, User> ();

        private class User
        {
            public Timer STimer;
            public double CheckInterval;

            public List<Share> AddedShares = new List<Share>();
            public int SharesQuant;

            public User() { CheckInterval = Constants.Interval5Min; }
        }

        private static class Constants
        {
            public const int ShortMess = 1;
            public const int ExtendedMess = 2;
            public const int EventMess = 0;
            public const int Interval5Min = 30000;
            public const string HelpDesk = "\t/share *name of share* - returns extended information about the share\n" +
                                            "\t/add *name of share* [*name of share*...] - adds one or several shares in list\n" +
                                            "\t/list shows you all short information about all shares in list\n" +
                                            "\t/delete *name of share* [*name of share*...] - deletes one or several shares from list\n" +
                                            "\t/delete &all - clean list of shares\n" +
                                            "\t/start_checking - starts send shares info non-stop\n" +
                                            "\t/stop_checking - stops send shares info\n" +
                                            "\t/set_interval - setting requesting interval";

        }
    }
}