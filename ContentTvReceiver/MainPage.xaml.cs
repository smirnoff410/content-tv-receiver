using Android.Hardware.Lights;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ContentTvReceiver;

public partial class MainPage : ContentPage
{
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
				if (message.Animation is { } animation)
				{
					await SaveVideo(botClient, animation.FileId, animation.MimeType, animation.Height, animation.Width);
					await botClient.SendTextMessageAsync(message.Chat, "Анимация успешно принята и скоро отобразится :)");
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
		var encryptedContent = await GetBytesFromFile(botClient, fileId);
		MainThread.BeginInvokeOnMainThread(() =>
		{
			ImageContent.Source = ImageSource.FromStream(() => new MemoryStream(encryptedContent));
			VideoContent.IsVisible = false;
			ImageContent.IsVisible = true;
		});
	}

	async Task SaveVideo(ITelegramBotClient botClient, string fileId, string? mimeType, int height, int width)
	{
		var encryptedContent = await GetBytesFromFile(botClient, fileId);
		MainThread.BeginInvokeOnMainThread(() =>
		{
			var htmlSource = new HtmlWebViewSource();
			htmlSource.Html = @$"<html><body><video autoplay muted loop playsinline class='slideContent' style='width:100%'><source src='data:{mimeType ?? "video/mp4"};base64,{Convert.ToBase64String(encryptedContent)}' type='video/mp4'></video></body></html>";
			VideoContent.Source = htmlSource;
			ImageContent.IsVisible = false;
			VideoContent.IsVisible = true;
			VideoContent.HeightRequest = height;
			VideoContent.WidthRequest = width;
		});
	}

	async Task<byte[]> GetBytesFromFile(ITelegramBotClient botClient, string fileId)
	{
		var file = await botClient.GetFileAsync(fileId);

		byte[] encryptedContent;
		using (var fileStream = new MemoryStream())
		{
			await botClient.DownloadFileAsync(file.FilePath, fileStream);
			encryptedContent = fileStream.ToArray();
		}
		return encryptedContent;
	}

	async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
	{
		// Некоторые действия
		Console.WriteLine(JsonConvert.SerializeObject(exception));
	}
}


