using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using HtmlAgilityPack;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace lab4
{
    internal class Program
    {
        private const string HelpDesk = "\t/share *name of share* - returns extended information about the share\n" +
                                        "\t/add *name of share* [*name of share*...] - adds one or several shares in list\n" +
                                        "\t/list shows you all short information about all shares in list\n" +
                                        "\t/delete *name of share* [*name of share*...] - deletes one or several shares from list\n" +
                                        "\t/delete &all - clean list of shares";

        private static List<Share> AddedShares = new List<Share>();
        private static int SharesQuant = -1;

        private static void Main()
        {
            var web = new HtmlWeb
            {
                PreRequest = OnPreRequest
            };

            var bot = new TelegramBotClient("1743657186:AAEyqLtqL95eKUZANc0hefOsySyWgcz7dVc");

            bot.StartReceiving();

            bot.OnMessage += Bot_OnMessage;

            Console.ReadLine();

            bot.StopReceiving();
        }

        private static bool OnPreRequest(HttpWebRequest request)
        {
            request.AllowAutoRedirect = true;
            return true;
        }


        private static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var bot = sender as TelegramBotClient;
            var userMess = e.Message.Text;
            var userMessWord = userMess.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            var count = userMessWord.Length;


            if (count == 1 && userMessWord[0][0] != '/' || userMessWord[0] == "/share")
            {
                Share_Info(sender, e);
                return;
            }


            if (userMessWord[0] == "/list")
            {
                if (List_Share(sender, e))
                {
                    bot = sender as TelegramBotClient;
                    bot.SendTextMessageAsync(e.Message.Chat.Id, "All shares was showed");
                }
                else
                {
                    bot = sender as TelegramBotClient;
                    bot.SendTextMessageAsync(e.Message.Chat.Id, "Nothing to show");
                }

                return;
            }


            if (userMessWord[0] == "/add")
            {
                if (Add_Several_Shares(userMessWord))
                {
                    bot = sender as TelegramBotClient;
                    if (count > 2)
                        bot.SendTextMessageAsync(e.Message.Chat.Id, "All shares were added in list");
                    else
                        bot.SendTextMessageAsync(e.Message.Chat.Id, "Share Added in list");
                }
                else
                {
                    bot = sender as TelegramBotClient;
                    if (count > 2)
                        bot.SendTextMessageAsync(e.Message.Chat.Id, "Some shares weren't added in list");
                    else
                        bot.SendTextMessageAsync(e.Message.Chat.Id, "Share was not added in list");
                }

                return;
            }


            if (userMessWord[0] == "/help")
            {
                bot = sender as TelegramBotClient;
                bot.SendTextMessageAsync(e.Message.Chat.Id, HelpDesk);
                return;
            }


            if (userMessWord[0] == "/delete")
            {
                if (userMessWord[1] == "&all")
                {
                    AddedShares.RemoveRange(0, SharesQuant + 1);
                    SharesQuant = -1;
                    bot = sender as TelegramBotClient;
                    bot.SendTextMessageAsync(e.Message.Chat.Id, "All shares were deleted from list");
                }
                else
                {
                    bot = sender as TelegramBotClient;
                    if (Del_Several_Shares(userMessWord))
                        bot.SendTextMessageAsync(e.Message.Chat.Id, "Shares were deleted from list");
                    else
                        bot.SendTextMessageAsync(e.Message.Chat.Id, "Some shares were not deleted from list");
                }

                return;
            }

            bot.SendTextMessageAsync(e.Message.Chat.Id, "Wrong Command");
        }

        private static void Share_Info(object sender, MessageEventArgs e, bool shortmess = false,
            string ShareCode = null, int cost = 0)
        {
            var bot = sender as TelegramBotClient;

            var url = "https://finance.yahoo.com/quote/";

            var userMessWord = e.Message.Text.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            var count = userMessWord.Length;

            if (ShareCode == null)
                if (count > 1)
                    ShareCode = userMessWord[1];
                else
                    ShareCode = userMessWord[0];

            url = url + ShareCode + '/';


            var handler = new HttpClientHandler {AllowAutoRedirect = true};
            var httpClient = new HttpClient(handler);

            var response = httpClient.GetAsync(url).Result;

            var redirectUri = response.RequestMessage.RequestUri;

            var web = new HtmlWeb();

            var doc = web.Load(redirectUri);


            var elements = doc.DocumentNode.Descendants("div")
                .Where(x => x.Attributes["class"] != null)
                .Where(x => x.Attributes["class"].Value == "D(ib) Mend(20px)" ||
                            x.Attributes["class"].Value == "D(ib) " ||
                            x.Attributes["class"].Value == "C($tertiaryColor) Fz(12px)").ToList();

            if (elements.Count == 0)
            {
                var errbot = sender as TelegramBotClient;
                errbot.SendTextMessageAsync(e.Message.Chat.Id, "Wrong share name");
                return;
            }

            var descHtml = elements.SelectMany(x => x.Descendants("span"))
                .Where(x => x.Attributes["class"] != null)
                .FirstOrDefault(x => x.Attributes["class"].Value == "Trsdu(0.3s) Fw(b) Fz(36px) Mb(-4px) D(ib)");

            var SharesCost = descHtml.InnerText;


            descHtml = elements.SelectMany(x => x.Descendants("h1"))
                .Where(x => x.Attributes["class"] != null)
                .FirstOrDefault(x => x.Attributes["class"].Value == "D(ib) Fz(18px)");

            var ShareName = descHtml.InnerText;


            descHtml = elements.SelectMany(x => x.DescendantsAndSelf("div"))
                .Where(x => x.Attributes["class"] != null)
                .FirstOrDefault(x => x.Attributes["class"].Value == "C($tertiaryColor) Fz(12px)");


            var SharePlat = descHtml.InnerText;
            var ShareVal = SharePlat[SharePlat.Length - 3] +
                           SharePlat[SharePlat.Length - 2].ToString() + SharePlat[SharePlat.Length - 1];


            if (shortmess)
            {
                var shortMess = ShareName + '\n' + SharesCost + ' ' + ShareVal;
                bot.SendTextMessageAsync(e.Message.Chat.Id, shortMess);
                Console.WriteLine(response);
                return;
            }


            var Mess = ShareName + '\n' + SharesCost + ' ' + ShareVal;
            bot.SendTextMessageAsync(e.Message.Chat.Id, Mess);
            Console.WriteLine(response);
        }

        private static bool Add_Share(string ShareCode)
        {
            AddedShares.Add(new Share());
            SharesQuant += 1;
            AddedShares[SharesQuant].Name = ShareCode;
            return true;
        }

        private static bool Add_Several_Shares(string[] ShareCodes)
        {
            for (var i = 1; i < ShareCodes.Length; i += 1)
                if (!Add_Share(ShareCodes[i]))
                    return false;
            return true;
        }

        private static bool Del_Share(string ShareCode)
        {
            foreach (var share in AddedShares.ToList())
                if (share.Name == ShareCode)
                    AddedShares.Remove(share);
            SharesQuant -= 1;
            return true;
        }

        private static bool Del_Several_Shares(string[] ShareCodes)
        {
            for (var i = 1; i < ShareCodes.Length; i += 1)
                if (!Del_Share(ShareCodes[i]))
                    return false;
            AddedShares = AddedShares.ToList();
            return true;
        }

        private static bool List_Share(object sender, MessageEventArgs e)
        {
            foreach (var share in AddedShares) Share_Info(sender, e, true, share.Name);

            return SharesQuant != -1;
        }

        private class Share
        {
            public string Name { get; set; }

            public int Cost { get; set; }
        }
    }
}