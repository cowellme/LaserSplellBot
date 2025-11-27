
using System.Net.Mime;
using LaserSplellBot;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using User = LaserSplellBot.User;

namespace JobBe;

// ReSharper disable once InconsistentNaming
public class TBot
{
    private readonly TelegramBotClient? _bot;
    private static List<Post> _posts = new List<Post>();
    public TBot(string token)
    {
        _bot = new TelegramBotClient(token);
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        UpdateType[] allowUpdate =
        {
            UpdateType.ChannelPost,
            UpdateType.Message,
            UpdateType.CallbackQuery
        };
        var receiverOptions = new ReceiverOptions { AllowedUpdates = allowUpdate, };
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
        var url = channelPost.Chat.Username ?? String.Empty;

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
                    KeyboardButton buttonCreate = new KeyboardButton("Запустить розыгрыш");
                    KeyboardButton buttonPublic = new KeyboardButton("Опубликовать");


                    ReplyMarkup buttons = new ReplyKeyboardMarkup(buttonCreate, buttonPublic);
                    
                    await _bot.SendMessage(adminChatId, $"Под вашу эгиду добавлен чат {url}", replyMarkup: buttons);
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
            var chatId = update.Message.Chat.Id;
            var user = chatId > 0 ? await ApplicationContext.GetUser(chatId) : null;

            if (user is {PostCreate: true})
            {
                var post = _posts.FirstOrDefault(x => x.AuthorChatId == user.ChatId);

                if (post != null)
                {
                    PostCreate(user, update, post);

                    if (post.PhotoId != string.Empty && post.Text != string.Empty && post.ButtonText != string.Empty)
                    {

                        await _bot.SendMessage(user.ChatId, "Пример поста");
                        ReplyMarkup inlineButton = new []
                        {
                            new InlineKeyboardButton(post.ButtonText, post.Id.ToString()),
                            new InlineKeyboardButton("Сохранить", post.Id.ToString()),

                        };
                        //await ApplicationContext.AddPost(post);
                        await _bot.SendPhoto(user.ChatId, InputFile.FromFileId(post.PhotoId), post.Text, replyMarkup: inlineButton);
                    }
                }
            }

            if (update.Message == null) return;
            if (string.IsNullOrEmpty(update.Message.Text)) return;
            var message = update.Message.Text!.ToLower();



            if (user != null)
            {
                

                

                if (message == "/id")
                {
                    await _bot!.SendMessage(chatId, $"{chatId}");
                }

                if (message == "запустить розыгрыш")
                {
                    await _bot.SendMessage(user.ChatId, "Пришли текст поста");
                    var newPost = new Post
                    {
                        AuthorChatId = user.ChatId,
                        ButtonText = string.Empty,
                        DateCreated = null,
                        DateDelete = null,
                        Text = string.Empty,
                    };
                    user.PostCreate = true;
                    _posts.Add(newPost);
                    await ApplicationContext.UpdUser(user);
                }
            }
            else
            {
                if (message == "/start")
                {
                    user = new User
                    {
                        ChatId = chatId,
                        DateCreated = DateTime.UtcNow,
                        Name = update.Message.Chat.FirstName ?? "none",

                    };
                    await _bot!.SendMessage(chatId, "Привет!");
                    await ApplicationContext.AddUser(user);
                }
            }

            
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }

    private void PostCreate(User user, Update update, Post? post)
    {
        if (post != null)
        {
            if (post.Text == string.Empty)
            {
                var message = update.Message!.Text ?? "Ошибка!";
                post.Text = message;
                _bot.SendMessage(post.AuthorChatId, "Пришли название кнопки");
                return;
            }

            if (post.ButtonText == string.Empty)
            {
                var message = update.Message!.Text ?? "Ошибка!";
                post.ButtonText = message;
                _bot.SendMessage(post.AuthorChatId, "Пришли фото для поста");
                return;
            }

            if (update.Message.Photo != null)
            {
                var photoId = update.Message.Photo.First().FileId;
                if (post.PhotoId == string.Empty) 
                {
                    var message = update.Message!.Text ?? "Ошибка!";
                    post.PhotoId = photoId;
                    return;
                }
            }

        }
    }

    private Task HandleErrorAsync(ITelegramBotClient arg1, Exception arg2, HandleErrorSource arg3, CancellationToken arg4)
    {
        return null;
    }
}