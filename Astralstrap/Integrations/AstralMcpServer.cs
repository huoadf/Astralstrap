using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Bloxstrap.Integrations
{
    public class AstralMcpServer : IDisposable
    {
        private const string LOG_IDENT = "AstralMcpServer";
        public const int DefaultPort = 37482;

        private HttpListener? _listener;
        private CancellationTokenSource _cts = new();
        private readonly ActivityWatcher? _activityWatcher;

        public static AstralMcpServer? Shared { get; private set; }

        public static void Initialize(ActivityWatcher? watcher)
        {
            try
            {
                Shared = new AstralMcpServer(watcher);
                Shared.Start();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Failed to initialize MCP server: {ex.Message}");
            }
        }

        public AstralMcpServer(ActivityWatcher? watcher)
        {
            _activityWatcher = watcher;
        }

        public void Start(int port = DefaultPort)
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                _listener.Start();

                Task.Run(() => ListenLoop(_cts.Token));
                App.Logger.WriteLine(LOG_IDENT, $"Astralstrap MCP / Local Stats endpoint running on http://127.0.0.1:{port}/");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Could not start HTTP listener on port {port}: {ex.Message}");
            }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context));
                }
                catch (HttpListenerException) { break; }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Error receiving request: {ex.Message}");
                }
            }
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            res.ContentType = "application/json; charset=utf-8";

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 204;
                res.Close();
                return;
            }

            try
            {
                string path = req.Url?.AbsolutePath.TrimEnd('/').ToLowerInvariant() ?? "";
                object responseData;

                switch (path)
                {
                    case "/status":
                    case "/api/status":
                        responseData = new
                        {
                            project = App.ProjectName,
                            version = App.Version,
                            inGame = _activityWatcher?.InGame ?? false,
                            inStudio = _activityWatcher?.InRobloxStudio ?? false,
                            placeId = _activityWatcher?.Data.PlaceId ?? 0,
                            universeId = _activityWatcher?.Data.UniverseId ?? 0,
                            gameName = _activityWatcher?.Data.UniverseDetails?.Data.Name ?? "None",
                            timeJoined = _activityWatcher?.Data.TimeJoined
                        };
                        break;

                    case "/playtime":
                    case "/api/playtime":
                    case "/stats":
                        responseData = new
                        {
                            totalHours = PlayHistoryManager.Instance.Data.TotalOverallPlaytime.TotalHours,
                            totalSessions = PlayHistoryManager.Instance.Data.TotalSessionsCount,
                            games = PlayHistoryManager.Instance.Data.GameStats.Values.OrderByDescending(x => x.TotalPlaytime).Take(50)
                        };
                        break;

                    case "/coplayers":
                    case "/api/coplayers":
                        responseData = new
                        {
                            count = CoPlayerTracker.Instance.Players.Count,
                            players = CoPlayerTracker.Instance.Players.Values.OrderByDescending(x => x.LastMet).Take(100)
                        };
                        break;

                    case "/mcp":
                    case "/rpc":
                    default:
                        responseData = new
                        {
                            name = "astralstrap-local-mcp",
                            version = App.Version,
                            endpoints = new[] { "/status", "/playtime", "/coplayers", "/mcp" },
                            tools = new[]
                            {
                                new { name = "get_playtime_summary", description = "Get aggregate playtime stats and top played experiences" },
                                new { name = "get_co_players", description = "Query local records of users played with" },
                                new { name = "get_live_status", description = "Check active Roblox in-game status and place details" }
                            }
                        };
                        break;
                }

                string json = JsonSerializer.Serialize(responseData, new JsonSerializerOptions { WriteIndented = true });
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"Error responding: {ex.Message}");
                res.StatusCode = 500;
            }
            finally
            {
                try { res.Close(); } catch { }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }
            _listener = null;
        }
    }
}
