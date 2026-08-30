using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bloxstrap.Models.Entities;

namespace Bloxstrap.Integrations
{
    public class GamePlayStat
    {
        public long UniverseId { get; set; }
        public long PlaceId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public TimeSpan TotalPlaytime { get; set; } = TimeSpan.Zero;
        public int SessionCount { get; set; } = 0;
        public DateTime FirstPlayed { get; set; } = DateTime.UtcNow;
        public DateTime LastPlayed { get; set; } = DateTime.UtcNow;
    }

    public class PlayStatsData
    {
        public TimeSpan TotalOverallPlaytime { get; set; } = TimeSpan.Zero;
        public int TotalSessionsCount { get; set; } = 0;
        public Dictionary<string, GamePlayStat> GameStats { get; set; } = new();
    }

    public class PlayHistoryManager
    {
        private const string LOG_IDENT = "PlayHistoryManager";
        private static readonly string StatsFilePath = Path.Combine(Paths.Base, "Data", "PlayStats.json");

        public static PlayHistoryManager Instance { get; } = new PlayHistoryManager();

        public PlayStatsData Data { get; private set; } = new();

        public PlayHistoryManager()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(StatsFilePath))
                {
                    Data = new PlayStatsData();
                    return;
                }

                string json = File.ReadAllText(StatsFilePath);
                Data = JsonSerializer.Deserialize<PlayStatsData>(json) ?? new PlayStatsData();
                App.Logger.WriteLine(LOG_IDENT, $"Loaded stats for {Data.GameStats.Count} games. Total playtime: {Data.TotalOverallPlaytime}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to load play stats: {ex.Message}");
                Data = new PlayStatsData();
            }
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(StatsFilePath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Data, options);
                File.WriteAllText(StatsFilePath, json);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to save play stats: {ex.Message}");
            }
        }

        public void RecordSession(ActivityData activity)
        {
            if (activity.UniverseId == 0 || activity.TimeJoined == default)
                return;

            DateTime leftTime = activity.TimeLeft ?? DateTime.UtcNow;
            TimeSpan duration = leftTime - activity.TimeJoined;
            if (duration < TimeSpan.FromSeconds(5))
                return;

            string key = activity.UniverseId.ToString();
            string gameName = activity.UniverseDetails?.Data.Name ?? $"Universe {activity.UniverseId}";

            if (!Data.GameStats.TryGetValue(key, out var stat))
            {
                stat = new GamePlayStat
                {
                    UniverseId = activity.UniverseId,
                    PlaceId = activity.PlaceId,
                    GameName = gameName,
                    FirstPlayed = activity.TimeJoined,
                    LastPlayed = leftTime,
                    SessionCount = 0,
                    TotalPlaytime = TimeSpan.Zero
                };
                Data.GameStats[key] = stat;
            }

            stat.GameName = gameName;
            stat.PlaceId = activity.PlaceId;
            stat.LastPlayed = leftTime;
            stat.SessionCount += 1;
            stat.TotalPlaytime += duration;

            Data.TotalOverallPlaytime += duration;
            Data.TotalSessionsCount += 1;

            Save();
            App.Logger.WriteLine(LOG_IDENT, $"Recorded session for {gameName}: {duration.TotalMinutes:F1} min (Total: {stat.TotalPlaytime.TotalHours:F1} hrs)");
        }
    }
}
