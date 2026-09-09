using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class TaskItem
{
    public string Description { get; set; } = "";
    public DateTime Start { get; set; } // время начала
    public DateTime End { get; set; }
    public string File { get; set; } = "";
}

class Program
{
    private static readonly Regex _taskRegex = new(
        @"-\s*\[ \]\s*(\d{2}:\d{2})\s*-\s*(\d{2}:\d{2})\s+(.*?)\s*⏳\s*(\d{4}-\d{2}-\d{2})",
        RegexOptions.Compiled);

    // Интервал предупреждения: 5 минут
    private static readonly TimeSpan WarningWindow = TimeSpan.FromMinutes(5);

    static async Task Main(string[] args)
    {
        string vaultPath = Environment.GetEnvironmentVariable("VAULT_PATH") ?? ".";
        string stateFile = Path.Combine(Directory.GetCurrentDirectory(), "state.json");

        var state = await LoadStateAsync(stateFile);
        var tasks = FindTasks(vaultPath);
        tasks.Sort((a, b) => a.Start.CompareTo(b.Start));

        Console.WriteLine($"Найдено задач: {tasks.Count}");
        foreach (var t in tasks.Take(10))
            Console.WriteLine($"  {t.Start:yyyy-MM-dd HH:mm} - {t.Description}");

        var now = DateTime.Now;

        // Все задачи, которые начнутся в течение ближайших 5 минут (включая прямо сейчас)
        var upcomingTasks = tasks
            .Where(t => t.Start > now && t.Start - now <= WarningWindow)
            .OrderBy(t => t.Start)
            .ToList();

        Console.WriteLine($"Задач в окне: {upcomingTasks.Count}");
        foreach (var task in upcomingTasks)
        {
            var timeLeft = task.Start - now;
            string message = $"⏰ Через {(int)timeLeft.TotalMinutes} мин: " +
                             $"<b>{task.Description}</b>\n" +
                             $"🕒 {task.Start:HH:mm} – {task.End:HH:mm}";

            // Уникальный ключ для предотвращения повторной отправки
            string key = $"{task.File}:{task.Description}:{task.Start:yyyy-MM-dd HH:mm}";

            if (!state.ContainsKey(key))
            {
                await SendNotificationAsync(message);
                state[key] = true;
                Console.WriteLine($"Отправлено уведомление: {key}");
            }
            else
            {
                Console.WriteLine($"Уведомление уже отправлено: {key}");
            }
        }


        await SaveStateAsync(stateFile, state);
    }

    private static List<TaskItem> FindTasks(string rootPath)
    {
        var tasks = new List<TaskItem>();
        var mdFiles = Directory.EnumerateFiles(rootPath, "*.md", SearchOption.AllDirectories)
            .Where(f => !f.Contains(".git"));

        foreach (var file in mdFiles)
        {
            string content;
            try
            {
                content = File.ReadAllText(file);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения файла {file}: {ex.Message}");
                continue;
            }

            foreach (Match match in _taskRegex.Matches(content))
            {
                var startTimeStr = match.Groups[1].Value;
                var endTimeStr = match.Groups[2].Value;
                var description = match.Groups[3].Value.Trim();
                var dateStr = match.Groups[4].Value;

                if (DateTime.TryParseExact($"{dateStr} {startTimeStr}", "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var start) &&
                    DateTime.TryParseExact($"{dateStr} {endTimeStr}", "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var end))
                {
                    tasks.Add(new TaskItem
                    {
                        Description = description,
                        Start = start,
                        End = end,
                        File = file
                    });
                }
            }
        }

        return tasks;
    }

    static async Task SendNotificationAsync(string text)
    {
        string topic = Environment.GetEnvironmentVariable("NTFY_TOPIC")
                       ?? throw new InvalidOperationException("NTFY_TOPIC не задан");

        string url = $"https://ntfy.sh/{topic}";

        using var client = new HttpClient();
        var content = new StringContent(text, Encoding.UTF8, "text/plain");
        content.Headers.Add("Title", "Task Reminder");
        content.Headers.Add("Priority", "high");
        content.Headers.Add("Markdown", "yes");

        HttpResponseMessage response = await client.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"Ошибка отправки в ntfy: {response.StatusCode}");
    }

    private static async Task<Dictionary<string, bool>> LoadStateAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return new Dictionary<string, bool>();

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new Dictionary<string, bool>();
    }

    static async Task SaveStateAsync(string filePath, Dictionary<string, bool> state)
    {
        string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
}
