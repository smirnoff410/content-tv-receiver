using ContentTvReceiver.Config;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ContentTvReceiver;

public partial class MainPage : ContentPage
{
	private HtmlWebViewSource htmlSource = new HtmlWebViewSource();
	public MainPage(IConfiguration configuration)
	{
		InitializeComponent();
		VideoContent.Source = htmlSource;

		var settings = configuration.GetRequiredSection("TelegramSettings").Get<TelegramSettings>();
		var bot = new TelegramBotClient(settings.BotToken);

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
			if (message.Type == MessageType.Text)
			{
				var lowerText = message.Text.ToLower();
				if (lowerText == "/start")
				{
					await botClient.SendTextMessageAsync(message.Chat, "Дарова бродяга, сюда ты можешь закинуть гифку, картинку, стикер или записать голосовое сообщение и оно отобразится у меня на телевизоре в прямом эфире");
				}
			}
			if (message.Type == MessageType.Photo)
			{
				await SaveImage(botClient, message.Photo.Last().FileId);
				await botClient.SendTextMessageAsync(message.Chat, "Изображение успешно принято и скоро отобразится :)");
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
				await botClient.SendTextMessageAsync(message.Chat, "Изображение успешно принято и скоро отобразится :)");
			}
			else if (message.Type == MessageType.Sticker)
			{
				if (message.Sticker.IsVideo)
					await SaveVideo(botClient, message.Sticker.FileId, null, message.Sticker.Height, message.Sticker.Width);
				else if (message.Sticker.IsAnimated)
				{
					await botClient.SendTextMessageAsync(message.Chat, "К сожалению я пока что не умею отображать этот вид стикеров, попробуйте другой :(");
					return;
				}
					
				else
					await SaveImage(botClient, message.Sticker.FileId);

				await botClient.SendTextMessageAsync(message.Chat, "Стикер успешно принят и скоро отобразится :)");
			}
			else if (message.Type == MessageType.Voice)
			{
				await SaveAudio(botClient, message.Voice.FileId, message.Voice.MimeType);
				await botClient.SendTextMessageAsync(message.Chat, "Голосовое успешно принято и скоро воспроизведется :)");
			}
			else
			{
				await botClient.SendTextMessageAsync(message.Chat, "Вы отправили неизвестный мне формат, я не знаю как это отобразить :(");
				return;
			}
			MainThread.BeginInvokeOnMainThread(() => UserName.Text = message.From.Username);
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
			var type = mimeType ?? "video/mp4";
			var base64String = Convert.ToBase64String(encryptedContent);
			htmlSource.Html = @$"<html><body><video autoplay muted loop playsinline class='slideContent' style='width:100%'><source src='data:{type};base64,{base64String}' type='{type}'></video></body></html>";
			ImageContent.IsVisible = false;
			VideoContent.IsVisible = true;
			VideoContent.HeightRequest = height;
			VideoContent.WidthRequest = width;
		});
	}

	async Task SaveAudio(ITelegramBotClient botClient, string fileId, string mimeType)
	{
		var encryptedContent = await GetBytesFromFile(botClient, fileId);
		MainThread.BeginInvokeOnMainThread(() =>
		{
			var base64String = Convert.ToBase64String(encryptedContent);
			htmlSource.Html = @$"<html><body><audio id='audioClass' controls src='data:{mimeType};base64,{base64String}'></audio></body></html>";
			ImageContent.IsVisible = false;
			VideoContent.IsVisible = true;
			
		});
		await TextToSpeech.Default.SpeakAsync("Check voice message");
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


