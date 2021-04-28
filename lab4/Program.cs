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


namespace lab4
{
    internal class Program
    {
        private static System.Timers.Timer _sTimer;
        private static double _checkInterval = Constants.Interval5Min;

        private static List<Share> _addedShares = new List<Share>();
        private static int _sharesQuant;

        private class CommandFactory
        {
            public static Command Get(string message)
            {
                return message switch
                {
                    "/share" => new ShareCommand(),
                    "/add" => new AddCommand(),
                    "/list" => new ListCommand(),
                    "/help" => new HelpCommand(),
                    "/delete" => new DeleteCommand(),
                    "/start_checking" => new StartCheckingCommand(),
                    "/stop_checking" => new StopCheckingCommand(),
                    "/set_interval" => new SetIntervalCommand(),
                    _ => new WrongCommand()
                };
            }

            public  static Command Get(int messageType)
            {
                return messageType switch
                {
                    Constants.ExtendedMess => new ExtendedMessageCommand(),
                    Constants.ShortMess => new ShortMessageCommand(),
                    Constants.EventMess => new EventMessageCommand(),
                    _ => new WrongCommand()
                };
            }
        }

        private abstract class Command
        {
            public abstract void Process(TelegramBotClient botclient, MessageEventArgs eventArgs);
            public abstract void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share);
        }

        private class ExtendedMessageCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                throw new NotImplementedException();
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                var mess = share.Name + '\n' + share.Cost + ' ' + share.Val + '\n'
                           + "Previous close " + share.Closed + ' ' + share.Val + '\n'
                           + "Open " + share.Opened + ' ' + share.Val;
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, mess);
            }
        }

        private class ShortMessageCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                throw new NotImplementedException();
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                var shortMess = share.Name + '\n' + share.Cost + ' ' + share.Val;
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, shortMess);
            }
        }

        private class EventMessageCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                throw new NotImplementedException();
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                string eventMess = share.Name + '\n' + share.Cost + ' ' + share.Val;
                if (share.Costdif != null && share.Costdif != "0.00")
                    eventMess += '\n' + "Cost difference:" + share.Costdif + ' ' + share.Val;

                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, eventMess);
            }
        }

        private class ShareCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                var userMess = eventArgs.Message.Text;
                var userMessWord = userMess.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                Share_Info(botclient, eventArgs, Constants.ExtendedMess, userMessWord[1]);
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                throw new NotImplementedException();
            }
        }

        private class AddCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                var userMess = eventArgs.Message.Text;
                var userMessWord = userMess.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var count = userMessWord.Length;
                if (Add_Several_Shares(userMessWord))
                {
                    
                    botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id,
                        count > 2 ? "All shares were added in list" : "Share Added in list");
                }
                else
                {
                    botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id,
                        count > 2 ? "Some shares weren't added in list" : "Share was not added in list");
                }
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                throw new NotImplementedException();
            }
        }

        private class ListCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id,
                    List_Share(botclient, eventArgs) ? "All shares was showed" : "Nothing to show");
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                throw new NotImplementedException();
            }
        }

        private class HelpCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                botclient.SendTextMessageAsync(eventArgs.Message.Chat.Id, Constants.HelpDesk);
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                throw new NotImplementedException();
            }
        }

        private class DeleteCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                var userMess = eventArgs.Message.Text;
                var userMessWord = userMess.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var count = userMessWord.Length;
                if (userMessWord[1] == "&all")
                {
                    _addedShares.RemoveRange(0, _sharesQuant);
                    _sharesQuant = 0;
                    botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "All shares were deleted from list");
                }
                else
                {
                    if (Del_Several_Shares(userMessWord))
                        botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id,
                            count > 2 ? "Shares were deleted from list" : "Share was deleted from list");
                    else
                        botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id,
                            count > 2 ? "Some shares were not deleted from list" : "Share was not deleted from list");
                }
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                throw new NotImplementedException();
            }
        }

        private class StartCheckingCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Start checking shares from the list");
                Start_Checking(botclient, eventArgs);
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                throw new NotImplementedException();
            }
        }

        private class StopCheckingCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Checking shares from the list was stopped");
                _sTimer.Enabled = false;
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                throw new NotImplementedException();
            }
        }

        private class SetIntervalCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                var userMess = eventArgs.Message.Text;
                var userMessWord = userMess.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (userMessWord[1].Split('.').Length - 1 > 1)
                {
                    botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Incorrect value for interval");
                    return;
                }
                CultureInfo tempCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

                _checkInterval = double.Parse(userMessWord[1]);
                _sTimer.Interval = _checkInterval * 60 * 1000;
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Checking interval set to " + _checkInterval + " mins");

                Thread.CurrentThread.CurrentCulture = tempCulture;
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                throw new NotImplementedException();
            }
        }

        private class WrongCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Wrong Command");
            }

            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share)
            {
                throw new NotImplementedException();
            }
        }

        private static void Main()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            var unused = new HtmlWeb
            {
                PreRequest = OnPreRequest
            };

            var bot = new TelegramBotClient("1743657186:AAEyqLtqL95eKUZANc0hefOsySyWgcz7dVc");
            SetTimer();
            bot.StartReceiving();

            bot.OnMessage += Bot_OnMessage;

            Console.ReadLine();

            bot.StopReceiving();
        }

        private static void SetTimer()
        {
            _sTimer = new System.Timers.Timer(Constants.Interval5Min) {AutoReset = true, Enabled = false};
        }

        private static bool OnPreRequest(HttpWebRequest request)
        {
            request.AllowAutoRedirect = true;
            return true;
        }

        private static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var bot = sender as TelegramBotClient;
            var userMessWord = e.Message.Text.Split(new[] { " " },
                                            StringSplitOptions.RemoveEmptyEntries);

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

        private static bool Add_Share(string shareCode)
        {
            _addedShares.Add(new Share());
            _addedShares[_sharesQuant].Name = shareCode;
            _addedShares[_sharesQuant].Cost = null;
            _sharesQuant += 1;
            _addedShares = _addedShares.ToList();
            return true;
        }

        private static bool Add_Several_Shares(string[] shareCodes)
        {
            for (var i = 1; i < shareCodes.Length; i += 1)
                if (!Add_Share(shareCodes[i]))
                    return false;
            return true;
        }

        private static bool Del_Share(string shareCode)
        {
            foreach (var share in _addedShares.ToList())
                if (share.Name == shareCode)
                {
                    _addedShares.Remove(share);
                    _sharesQuant -= 1;
                }
            return true;
        }

        private static bool Del_Several_Shares(string[] shareCodes)
        {
            for (var i = 1; i < shareCodes.Length; i += 1)
                if (!Del_Share(shareCodes[i]))
                    return false;
            _addedShares = _addedShares.ToList();
            return true;
        }

        private static bool List_Share(TelegramBotClient sender, MessageEventArgs e)
        {
            foreach (var share in _addedShares) Share_Info(sender, e, Constants.ShortMess, share.Name);

            return _sharesQuant != -1;
        }

        private static void Start_Checking(TelegramBotClient bot, MessageEventArgs e)
        {
            _sTimer.Enabled = true;
            _sTimer.Elapsed += (tSender, tE) =>
            {
                foreach (var share in _addedShares.ToList())
                {
                    Share_Info(bot, e, Constants.EventMess, share.Name, share);
                }
                
            };
        }


        private class Share
        {
            public string Name { get; set; }
            public string Cost { get; set; }
            public string Val { get; set; }
            public string Opened { get; set; }
            public string Closed { get; set; }
            public string Costdif { get; set; }

            public static HtmlDocument GetUrl(MessageEventArgs e, string shareCode)
            {
                var url = "https://finance.yahoo.com/quote/";
                var userMessWord = e.Message.Text.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var count = userMessWord.Length;

                if (shareCode == null) shareCode = count > 1 ? userMessWord[1] : userMessWord[0];

                url = url + shareCode + '/';


                var handler = new HttpClientHandler { AllowAutoRedirect = true };
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
                    Cost = Math.Round(double.Parse(descHtml.InnerText[Range.EndAt(descHtml.InnerText.Length)]), 2).ToString(CultureInfo.InvariantCulture);

                descHtml = elements.SelectMany(x => x.DescendantsAndSelf("div"))
                    .Where(x => x.Attributes["class"] != null)
                    .FirstOrDefault(x => x.Attributes["class"].Value == "C($tertiaryColor) Fz(12px)");
                if (descHtml?.InnerText != null)
                { 
                    var sharePlat = descHtml.InnerText.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
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


        private class user
        {

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