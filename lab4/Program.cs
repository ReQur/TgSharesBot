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