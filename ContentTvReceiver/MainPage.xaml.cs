using System.IO;
using System.IO.Pipes;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ContentTvReceiver;

public partial class MainPage : ContentPage
{
    byte[] encryptedContent;


    public MainPage()
	{
		InitializeComponent();

        var bot = new TelegramBotClient("5539542686:AAFdCUf9ze542bqugblAe3pgK6XoZtB4yaU");

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { }, // receive all update types
        };

        bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken);
    }

    async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Некоторые действия
        Console.WriteLine(JsonConvert.SerializeObject(update));
        if (update.Type == UpdateType.Message)
        {
            var message = update.Message;
            if (message.Type == MessageType.Photo)
            {
                await SaveImage(botClient, message.Photo.Last().FileId);
            }
            else if (message.Type == MessageType.Document)
            {
                if (message.Document.MimeType == "video/mp4")
                {
                    await botClient.SendTextMessageAsync(message.Chat, "К сожалению я пока что не могу отображать анимированные изображения :(");
                    return;
                }
                await SaveImage(botClient, message.Document.FileId);
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat, "Вы отправили неизвестный мне формат, я не знаю как это отобразить :(");
                return;
            }
            await botClient.SendTextMessageAsync(message.Chat, "Изображение успешно принято и скоро отобразится :)");
        }
    }

    async Task SaveImage(ITelegramBotClient botClient, string fileId)
    {

        var file = await botClient.GetFileAsync(fileId);

        using (var fileStream = new MemoryStream())
        {
            await botClient.DownloadFileAsync(file.FilePath, fileStream);
            encryptedContent = fileStream.ToArray();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ImageContent.Source = ImageSource.FromStream(() => new MemoryStream(encryptedContent));
            });
        }
    }

    async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        // Некоторые действия
        Console.WriteLine(JsonConvert.SerializeObject(exception));
    }
}


