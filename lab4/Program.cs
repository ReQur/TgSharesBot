﻿using System;
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

        public class CommandFactory
        {
            public static Command Get(string message)
            {
                return message switch
                {
                    "/share" => new ShareCommand(),
                    "/add" => new AddCommand(),
                    "/list" => new ListCommand(),
                    "/help" => new HelpCommand(),
                    "/delete" => new DeleteCommad(),
                    "/start_checking" => new StartCheckingCommand(),
                    "/stop_checking" => new StopCheckingCommand(),
                    "/set_interval" => new SetIntervalCommand(),
                    _ => new WrongCommand()
                };
            }
        }
        public abstract class Command
        {
            public abstract void Process(TelegramBotClient botclient, MessageEventArgs eventArgs);
            
        }
        public class ShareCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                Share_Info(botclient, eventArgs);
            }
        }
        public class AddCommand : Command
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
        }
        public class ListCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                if (List_Share(botclient, eventArgs))
                {
                    botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "All shares was showed");
                }
                else
                {
                    botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Nothing to show");
                }
            }
        }
        public class HelpCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                botclient.SendTextMessageAsync(eventArgs.Message.Chat.Id, Constants.HelpDesk);
            }
        }
        public class DeleteCommad : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                var userMess = eventArgs.Message.Text;
                var userMessWord = userMess.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var count = userMessWord.Length;
                if (userMessWord[1] == "&all")
                {
                    _addedShares.RemoveRange(0, _sharesQuant + 1);
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
        }
        public class StartCheckingCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Start checking shares from the list");
                Start_Checking(botclient, eventArgs);
            }
        }
        public class StopCheckingCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Checking shares from the list was stopped");
                _sTimer.Enabled = false;
            }
        }
        public class SetIntervalCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                var userMess = eventArgs.Message.Text;
                var userMessWord = userMess.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                CultureInfo tempCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

                _checkInterval = double.Parse(userMessWord[1]);
                _sTimer.Interval = _checkInterval * 60 * 1000;
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Checking interval set to " + _checkInterval + " mins");

                Thread.CurrentThread.CurrentCulture = tempCulture;
            }
        }
        public class WrongCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs)
            {
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Wrong Command");
            }
        }



        private static void Main()
        {

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

        private static void Share_Info(object sender, MessageEventArgs e, int messType = Constants.ExtendedMess,
            string shareCode = null, Share share = null)
        {
            var bot = sender as TelegramBotClient;
            var url = "https://finance.yahoo.com/quote/";
            var userMessWord = e.Message.Text.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            var count = userMessWord.Length;

            if (shareCode == null) shareCode = count > 1 ? userMessWord[1] : userMessWord[0];

            url = url + shareCode + '/';


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
                bot?.SendTextMessageAsync(e.Message.Chat.Id, "Wrong share name");
                return;
            }

            var descHtml = elements.SelectMany(x => x.Descendants("span"))
                .Where(x => x.Attributes["class"] != null)
                .FirstOrDefault(x => x.Attributes["class"].Value == "Trsdu(0.3s) Fw(b) Fz(36px) Mb(-4px) D(ib)");

            var sharesCost = descHtml?.InnerText;


            descHtml = elements.SelectMany(x => x.Descendants("h1"))
                .Where(x => x.Attributes["class"] != null)
                .FirstOrDefault(x => x.Attributes["class"].Value == "D(ib) Fz(18px)");

            var shareName = descHtml?.InnerText;


            descHtml = elements.SelectMany(x => x.DescendantsAndSelf("div"))
                .Where(x => x.Attributes["class"] != null)
                .FirstOrDefault(x => x.Attributes["class"].Value == "C($tertiaryColor) Fz(12px)");

            var sharePlat = descHtml?.InnerText.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            var shareVal = sharePlat?[^1]; // Takes last word in string, usual that is "USD"


            if (messType == Constants.ShortMess)  //exiting from method if needs only short message
            {
                var shortMess = shareName + '\n' + sharesCost + ' ' + shareVal;
                bot?.SendTextMessageAsync(e.Message.Chat.Id, shortMess);
                Console.WriteLine(response);
                return;
            }

            if (messType == Constants.EventMess)
            {
                CultureInfo tempCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
                if (share?.Cost == 0)
                {
                    share.Cost = double.Parse(sharesCost?[Range.EndAt(4)]);
                }
                var costDif = (share.Cost - double.Parse(sharesCost?[Range.EndAt(4)])).ToString(CultureInfo.InvariantCulture);

                var eventMess = shareName + '\n' + sharesCost + ' ' + shareVal + '\n'
                                        + "Cost difference:" + costDif[Range.EndAt(4)] + ' ' + shareVal;

                bot?.SendTextMessageAsync(e.Message.Chat.Id, eventMess);
                Console.WriteLine(response);
                Thread.CurrentThread.CurrentCulture = tempCulture;
                return;
            }



            if (messType == Constants.ExtendedMess)  //exiting from method for extended message
            {
                var elements2 = doc.DocumentNode.Descendants("span")
                    .Where(x => x.Attributes["class"] != null)
                    .Where(x => x.Attributes["class"].Value == "Trsdu(0.3s) " &&
                                (x.Attributes["data-reactid"].Value == "44" ||
                                 x.Attributes["data-reactid"].Value == "49")).ToList();

                descHtml = elements2[0];
                var prevClose = descHtml.InnerText;

                descHtml = elements2[1];
                var opened = descHtml.InnerText;


                var mess = shareName + '\n' + sharesCost + ' ' + shareVal + '\n'
                    + "Previous close " + prevClose + ' ' + shareVal + '\n'
                    + "Open " + opened + ' ' + shareVal;
                bot?.SendTextMessageAsync(e.Message.Chat.Id, mess);
                Console.WriteLine(response);
            }
        }

        private static bool Add_Share(string shareCode)
        {
            _addedShares.Add(new Share());
            _addedShares[_sharesQuant].Name = shareCode;
            _addedShares[_sharesQuant].Cost = 0;
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

        private static bool List_Share(object sender, MessageEventArgs e)
        {
            foreach (var share in _addedShares) Share_Info(sender, e, 1, share.Name);

            return _sharesQuant != -1;
        }

        private static void Start_Checking(object sender, MessageEventArgs e)
        {
            _sTimer.Enabled = true;
            _sTimer.Elapsed += (tSender, tE) =>
            {
                foreach (var share in _addedShares.ToList())
                {
                    Share_Info(sender, e, Constants.EventMess, share.Name, share);
                }
                
            };
        }


        private class Share
        {
            public string Name { get; set; }

            public double Cost { get; set; }
        }

        public static class Constants
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