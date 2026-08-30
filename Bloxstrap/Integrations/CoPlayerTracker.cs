using System.IO;
using System.Text.Json;

namespace Bloxstrap.Integrations
{
    public class CoPlayerRecord
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime FirstMet { get; set; } = DateTime.UtcNow;
        public DateTime LastMet { get; set; } = DateTime.UtcNow;
        public int EncounterCount { get; set; } = 1;
        public long LastPlaceId { get; set; } = 0;
        public string LastJobId { get; set; } = string.Empty;
    }

    public class CoPlayerTracker
    {
        private const string LOG_IDENT = "CoPlayerTracker";
        private static readonly string DatabaseFilePath = Path.Combine(Paths.Base, "Data", "CoPlayers.json");

        public static CoPlayerTracker Instance { get; } = new CoPlayerTracker();

        public Dictionary<long, CoPlayerRecord> Players { get; private set; } = new();

        public CoPlayerTracker()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(DatabaseFilePath))
                {
                    Players = new Dictionary<long, CoPlayerRecord>();
                    return;
                }

                string json = File.ReadAllText(DatabaseFilePath);
                Players = JsonSerializer.Deserialize<Dictionary<long, CoPlayerRecord>>(json) ?? new();
                App.Logger.WriteLine(LOG_IDENT, $"Loaded {Players.Count} co-player records");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to load co-player database: {ex.Message}");
                Players = new();
            }
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(DatabaseFilePath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Players, options);
                File.WriteAllText(DatabaseFilePath, json);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to save co-player database: {ex.Message}");
            }
        }

        public void RecordPlayer(long userId, string username, string displayName, long placeId, string jobId)
        {
            if (userId <= 0) return;

            if (Players.TryGetValue(userId, out var existing))
            {
                existing.Username = username;
                existing.DisplayName = displayName;
                existing.LastMet = DateTime.UtcNow;
                existing.EncounterCount += 1;
                existing.LastPlaceId = placeId;
                existing.LastJobId = jobId;
            }
            else
            {
                Players[userId] = new CoPlayerRecord
                {
                    UserId = userId,
                    Username = username,
                    DisplayName = displayName,
                    FirstMet = DateTime.UtcNow,
                    LastMet = DateTime.UtcNow,
                    EncounterCount = 1,
                    LastPlaceId = placeId,
                    LastJobId = jobId
                };
            }

            Save();
        }
    }
}
