﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Net;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace VIR.Services
{
    //TODO: Comment this mess
    public class CommandHandlingService
    {
        private readonly CommandService __commands;
        private readonly DiscordSocketClient __client;
        private readonly IServiceProvider __services;

        public CommandHandlingService(IServiceProvider services)
        {
            __commands = services.GetRequiredService<CommandService>();
            __client = services.GetRequiredService<DiscordSocketClient>();
            __services = services;

            //__commands.CommandExecuted += CommandExecutedAsync;
            __commands.Log += Logging;
            __client.MessageReceived += MessageReceivedAsync;

        }

        public async Task InitializeAsync()
        {
            await __commands.AddModulesAsync(Assembly.GetEntryAssembly(), __services);
        }

        public async Task MessageReceivedAsync(SocketMessage msg)
        {
            if (!(msg is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            int argPos = 0;
            if (!message.HasMentionPrefix(__client.CurrentUser, ref argPos) && !message.HasCharPrefix('&', ref argPos)) return;
            
            SocketCommandContext context = new SocketCommandContext(__client, message);
            IResult res = __commands.ExecuteAsync(context, argPos, __services).GetAwaiter().GetResult();
            await CommandExecutedAsync(context, res);
        }

        public async Task CommandExecutedAsync(ICommandContext context, IResult result)
        {
            /*if (!command.IsSpecified)
                return;*/

            //await Log.Logger(Log.Logs.INFO, result.IsSuccess.ToString());
            if (result.Error == null)
            {
                //Console.WriteLine("Result Error Null");
                return;
            }
            if (result.Error.Value == CommandError.UnknownCommand)
            {
                //Console.WriteLine("Unknown Command");
                return;
            }
            if (result.Error.Value == CommandError.UnmetPrecondition)
            {
                await context.Channel.SendMessageAsync($"The following condition has failed: {result.ErrorReason}");
                //Console.WriteLine("Unmet Precondition");
                return;
            }
            if (result.Error.Value == CommandError.BadArgCount)
            {
                await context.Channel.SendMessageAsync($"Not enough arguments for the command!");
                //Console.WriteLine("Not enough arguments");
                return;
            }

            if (result.IsSuccess) { 
                //Console.WriteLine("Success!");
                return;
            }   
            await context.Channel.SendMessageAsync($"There was an error running the command, please try it again and if the problem persists contact towergame#9726. {result.ErrorReason}");
            await Log.Logger(Log.Logs.ERROR, $"A problem occured running a command: {result.ToString()}.");
        }

        private Task Logging(LogMessage log)
        {
            Log.Logger(Log.Logs.INFO, log.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Posts a message to the specified channel
        /// </summary>
        /// <param name="channel">The channel to post the message in</param>
        /// <param name="message">The message to be posted.</param>
        /// <returns></returns>
        public async Task PostMessageTask(string channel, string message)
        {
            await ((ISocketMessageChannel)__client.GetChannel(Convert.ToUInt64(channel))).SendMessageAsync(message);
        }

        /// <summary>
        /// Posts an embed to the specified channel
        /// </summary>
        /// <param name="channel">The channel to post the message in</param>
        /// <param name="embed">The embed to be posted.</param>
        public async Task PostEmbedTask(string channel, Embed embed)
        {
            await ((ISocketMessageChannel)__client.GetChannel(Convert.ToUInt64(channel))).SendMessageAsync("", false, embed);
        }
    }
}
