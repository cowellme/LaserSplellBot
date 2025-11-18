
using System.Net.Mime;
using LaserSplellBot;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace JobBe;

// ReSharper disable once InconsistentNaming
public class TBot
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TelegramBotClient? _bot;
    //private readonly AiApi _ai;

    public TBot(string token)
    {
        _bot = new TelegramBotClient(token);
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions { AllowedUpdates = { }, };
        _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken);
       
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cts)
    {
        switch (update.Type)
        {
            case UpdateType.Message:
                await NewMessage(update);
                break;
            case UpdateType.CallbackQuery:
                await NewCallback(update);
                break;
            case UpdateType.ChannelPost:
                await NewPostChannel(update);
                break;
        }

    }

    private async Task NewPostChannel(Update update)
    {
        if (update.ChannelPost != null)
        {
            await HandleChannelPost(update.ChannelPost);
        }

    }

    private async Task HandleChannelPost(Message channelPost)
    {
        var chatId = channelPost.Chat.Id;
        var message = channelPost.Text;

        try
        {
            if (long.TryParse(message, out var adminChatId))
            {
                await _bot.DeleteMessage(chatId, channelPost.Id);
                var title = channelPost.Chat.Title ?? "none";
                var channel = new Channel
                {
                    AdminChatId = adminChatId,
                    ChatId = chatId,
                    Name = title
                };

                if (await ApplicationContext.NewChannel(channel))
                {
                    KeyboardButton button = new KeyboardButton("Запустить розыгрыш");
                    ReplyMarkup buttons = new ReplyKeyboardMarkup(new []
                    {
                        button
                    });
                    await _bot.SendMessage(chatId, $"Под вашу эгиду добавлен чат {title}", replyMarkup: buttons);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async Task CreateGameChannelPost(Message updateChannelPost)
    {
        var chatId = updateChannelPost.Chat.Id;
        var inlineKeyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithUrl("Учавствовать!", "https://example.com")
        );

        await _bot.SendMessage(
            chatId: chatId,
            text: "text",
            replyMarkup: inlineKeyboard,
            parseMode: ParseMode.Html
        );

    }

    private async Task NewCallback(Update update)
    {
        if (update.CallbackQuery == null) return;
        if (string.IsNullOrEmpty(update.CallbackQuery.Data)) return;

        var message = update.CallbackQuery.Data;
        var chatId = update.CallbackQuery.Message?.Chat.Id;

    }

    private async Task NewMessage(Update update)
    {

        try
        {
            if (update.Message == null) return;
            if (string.IsNullOrEmpty(update.Message.Text)) return;

            var message = update.Message.Text!.ToLower();
            var chatId = update.Message.Chat.Id;

            if (message == "/start")
            {
                await _bot!.SendMessage(chatId, "Привет!");
            }
            
            if (message == "/id")
            {
                await _bot!.SendMessage(chatId, $"{chatId}");
            }

            if (message == "/game")
            {
                await _bot!.SendMessage(chatId, "Привет!");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient arg1, Exception arg2, HandleErrorSource arg3, CancellationToken arg4)
    {
        return null;
    }
}