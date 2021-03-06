﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using SteamInfoPlayerBot.Services;
using Telegram.Bot.Types.Enums;

namespace SteamInfoPlayerBot
{
    class Program
    {
        private static readonly int _telegramOwner = 29315313;
        private static readonly TelegramBotClient _botClient = new Telegram.Bot.TelegramBotClient(Environment.GetEnvironmentVariable("TELEGRAM_TOKEN"));
        private static readonly SteamService _steamClient = new SteamService(Environment.GetEnvironmentVariable("STEAM_TOKEN"));
        private static readonly GithubServices _githubClient = new GithubServices(Environment.GetEnvironmentVariable("GITHUB_TOKEN"));
        public static void Main()
        {
            // Initialize the bot
            // InitBotAsync().GetAwaiter().GetResult();
            // Bot starts to hear
            _botClient.StartReceiving();
            _botClient.OnMessage += hearMessages;
            _botClient.OnReceiveError += BotOnReceiveError;
            Console.ReadLine();
            _botClient.StopReceiving();
            // BuildWebHost(args).Run();
        }

        static async Task InitBotAsync()
        {
            var me = await _botClient.GetMeAsync();
            System.Console.WriteLine("Hello! My name is " + me.FirstName);
        }

        private static async void hearMessages(object sender, MessageEventArgs e)
        {
            if ((e.Message.Text is null) || (e.Message.Type != MessageType.TextMessage)) return;
            if (e.Message.Text.StartsWith("/steam"))
            {
                string[] str = e.Message.Text.Split("/steam ");
                if (str.Length < 2)
                {
                    await _botClient.SendTextMessageAsync(e.Message.Chat.Id, "Insert the ID or a Steam nickname");
                    return;
                }
                string msg = e.Message.Text.Split("/steam ")[1];

                try
                {
                    var playerInfo = await _steamClient.PlayerInfo(msg);

                    string optCaption = "";
                    if (playerInfo.PlayingGameId != null)
                        optCaption =
                            $"◾️ Is playing at {playerInfo.PlayingGameName}\n";
                    else optCaption = $"◾️ Last seen: {playerInfo.LastLoggedOffDate}";


                    string caption =
                        $"◾️ Nick: {playerInfo.Nickname}\n" +
                        $"◾️ Real Name: {playerInfo.RealName}\n" +
                        $"◾️ Status: {playerInfo.UserStatus}\n" +
                        $"{optCaption}";
                    
                    await _botClient.SendPhotoAsync(e.Message.Chat.Id, new Telegram.Bot.Types.FileToSend(playerInfo.AvatarFullUrl), caption);
                }

                catch (System.Exception err)
                {
                    var x = await _githubClient.OpenTelegramIssue(err, e.Message.From);
                    // For client
                    await _botClient.SendTextMessageAsync(e.Message.Chat.Id, 
                    "Please, insert the Steam ID or the Steam nickname. Without one of these information you can't retrieve any information.");
                    // For Bot Admin
                    await _botClient.SendTextMessageAsync(_telegramOwner, "An error has occurred, a report has been sent to ours bug tracker: " + x.HtmlUrl);
                }
            }
        }
        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            _githubClient.OpenGeneralIssue(receiveErrorEventArgs.ApiRequestException);
            Debugger.Break();
        }
    }
}
