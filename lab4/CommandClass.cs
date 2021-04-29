using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace lab4
{
    internal partial class Program
    {
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

            public static Command Get(int messageType)
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
            public abstract void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null);
        }

        private class ExtendedMessageCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
            {
                var mess = share?.Name + '\n' + share?.Cost + ' ' + share?.Val + '\n'
                           + "Previous close " + share?.Closed + ' ' + share?.Val + '\n'
                           + "Open " + share?.Opened + ' ' + share?.Val;
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, mess);
            }
        }

        private class ShortMessageCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
            {
                var shortMess = share?.Name + '\n' + share?.Cost + ' ' + share?.Val;
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, shortMess);
            }
        }

        private class EventMessageCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
            {
                string eventMess = share?.Name + '\n' + share?.Cost + ' ' + share?.Val;
                if (share?.Costdif != null && share.Costdif != "0.00")
                    eventMess += '\n' + "Cost difference:" + share.Costdif + ' ' + share.Val;

                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, eventMess);
            }
        }

        private class ShareCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
            {
                var userMess = eventArgs.Message.Text;
                var userMessWord = userMess.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                Share_Info(botclient, eventArgs, Constants.ExtendedMess, userMessWord[1]);
            }

        }

        private class AddCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
            {
                var userMess = eventArgs.Message.Text;
                var userMessWord = userMess.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var count = userMessWord.Length;
                if (Add_Several_Shares(userMessWord, eventArgs))
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

        private class ListCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
            {
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id,
                    List_Share(botclient, eventArgs) ? "All shares was showed" : "Nothing to show");
            }
        }

        private class HelpCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
            {
                botclient.SendTextMessageAsync(eventArgs.Message.Chat.Id, Constants.HelpDesk);
            }
        }

        private class DeleteCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
            {
                var userMess = eventArgs.Message.Text;
                var userMessWord = userMess.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var count = userMessWord.Length;
                if (userMessWord[1] == "&all")
                {
                    _users[eventArgs.Message.Chat.Id].AddedShares.RemoveRange(0, _users[eventArgs.Message.Chat.Id].SharesQuant);
                    _users[eventArgs.Message.Chat.Id].SharesQuant = 0;
                    botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "All shares were deleted from list");
                }
                else
                {
                    if (Del_Several_Shares(userMessWord, eventArgs))
                        botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id,
                            count > 2 ? "Shares were deleted from list" : "Share was deleted from list");
                    else
                        botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id,
                            count > 2 ? "Some shares were not deleted from list" : "Share was not deleted from list");
                }
            }
        }

        private class StartCheckingCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
            {
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Start checking shares from the list");
                Start_Checking(botclient, eventArgs);
            }
        }

        private class StopCheckingCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
            {
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Checking shares from the list was stopped");
                _users[eventArgs.Message.Chat.Id].STimer.Enabled = false;
            }
        }

        private class SetIntervalCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
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

                _users[eventArgs.Message.Chat.Id].CheckInterval = double.Parse(userMessWord[1]);
                _users[eventArgs.Message.Chat.Id].STimer.Interval = _users[eventArgs.Message.Chat.Id].CheckInterval * 60 * 1000;
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id,
                    "Checking interval set to " + _users[eventArgs.Message.Chat.Id].CheckInterval + " mins");

                Thread.CurrentThread.CurrentCulture = tempCulture;
            }
        }

        private class WrongCommand : Command
        {
            public override void Process(TelegramBotClient botclient, MessageEventArgs eventArgs, Share share = null)
            {
                botclient?.SendTextMessageAsync(eventArgs.Message.Chat.Id, "Wrong Command");
            }
        }
    }
}
