using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Newtonsoft.Json.Linq;

class Program
{
    static string BOT_TOKEN = "8789122416:AAE3tbnY1ZwC09PHqMmr9fEwbglcTktRgF8";
    static string UNSPLASH_KEY = "https://api.unsplash.com/search/photos?query=cat&per_page=3&client_id=API_KEY";

    static TelegramBotClient bot = new TelegramBotClient(BOT_TOKEN);
    static HttpClient httpClient = new HttpClient();

    static async Task Main()
    {
        using var cts = new CancellationTokenSource();

        bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandleErrorAsync,
            cancellationToken: cts.Token
        );

        Console.WriteLine("Bot ishga tushdi...");
        await Task.Delay(-1);
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        if (update.Message is not { } msg || msg.From == null || string.IsNullOrEmpty(msg.Text))
            return;

        if (msg.Text.Trim().ToLower() == "/start")
        {
            await botClient.SendTextMessageAsync(msg.Chat.Id,
                "Salom! Rasm qidirish uchun so'zni yozing. Masalan: cat, car, phone");
            return;
        }

        string query = msg.Text.Trim();
        await botClient.SendTextMessageAsync(msg.Chat.Id, $"Rasm qidirilyapti: {query} ...");

        var images = await GetImages(query);
        if (images.Count == 0)
        {
            await botClient.SendTextMessageAsync(msg.Chat.Id, "Rasm topilmadi 😔");
            return;
        }

        int i = 1;
        foreach (var url in images)
        {
            try
            {
                byte[] bytes = await httpClient.GetByteArrayAsync(url);

                using var ms = new MemoryStream(bytes);
                var photo = new InputOnlineFile(ms, $"{query}_{i}.jpg");

                await botClient.SendPhotoAsync(
                    chatId: msg.Chat.Id,
                    photo: photo,
                    caption: $"Rasm {i}"
                );

                i++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Rasm yuborishda xato: {ex.Message}");
            }
        }
    }

    static async Task<List<string>> GetImages(string query)
    {
        List<string> urls = new List<string>();
        try
        {
            string url = $"https://api.unsplash.com/search/photos?query={query}&per_page=3&client_id={UNSPLASH_KEY}";
            var response = await httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);

            foreach (var item in json["results"])
            {
                if (item["urls"]?["regular"] != null)
                    urls.Add((string)item["urls"]["regular"]);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("API xatosi: " + ex.Message);
        }
        return urls;
    }

    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception ex, CancellationToken ct)
    {
        Console.WriteLine("Error: " + ex.Message);
        return Task.CompletedTask;
    }
}