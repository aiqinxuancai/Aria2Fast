using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Services.GameAnalytics
{
    using Flurl.Http;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class GameAnalyticsClient : IDisposable
    {
        // --- Configuration Constants ---
        private const int GA_BATCH_SIZE_LIMIT = 20;      // Flush queue when event count reaches 20
        private const double GA_FLUSH_INTERVAL_SECONDS = 30.0; // Flush queue every 30 seconds
        private const string SESSION_RECOVERY_KEY = "gameanalytics_session_recovery_data";

        // --- Properties ---
        private readonly string _baseUrl;
        private readonly string _gameKey;
        private readonly string _secretKey;
        private readonly string _userId;
        private bool _canSendEvents;
        private string _sessionId;
        private DateTime _sessionStartDate;
        private readonly List<Dictionary<string, object>> _eventQueue;
        private readonly FileBasedKeyValueStore _mmkv;

        // --- Default Annotations ---
        private readonly string _platform;
        private readonly string _osVersion;
        private readonly string _sdkVersion;
        private readonly string _device;
        private readonly string _manufacturer;

        // --- Sending Strategy Properties ---
        private Timer _sendTimer;
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);

        public GameAnalyticsClient(string baseUrl, string gameKey, string secretKey, string userId)
        {
            Version v = Environment.OSVersion.Version;
            _platform = "windows"; // Or determine dynamically, e.g., using System.Runtime.InteropServices.RuntimeInformation
            _osVersion = $"{_platform}{string.Format(" {0}.{1}.{2}", v.Major, v.Minor, v.Build)}"; ; ; //AppUtils.DeviceSystemVersion();
            _sdkVersion = "rest api v2";
            _device = AppUtils.DeviceModelName();
            _manufacturer = "unknown"; // Or determine dynamically

            _baseUrl = baseUrl;
            _gameKey = gameKey;
            _secretKey = secretKey;
            _userId = userId;
            _eventQueue = new List<Dictionary<string, object>>();

            // Initialize the key-value store
            _mmkv = new FileBasedKeyValueStore("GameAnalyticsSession");
        }

        public async Task<bool> InitializeAsync()
        {
            var initPayload = new Dictionary<string, object>
        {
            { "platform", _platform },
            { "os_version", _osVersion },
            { "sdk_version", _sdkVersion },
            { "user_id", _userId },
            { "build", AppUtils.BundleFullVersion() }
        };

            var url = $"{_baseUrl}/v2/{_gameKey}/init";
            var response = await SendRequestAsync(url, initPayload);

            if (response.IsSuccess && IsInitEnabled(response.ResponseBody))
            {
                _canSendEvents = true;
                Debug.WriteLine("[GA] GameAnalytics initialized successfully.");
                return true;
            }

            _canSendEvents = false;
            Debug.WriteLine("[GA] GameAnalytics is disabled or init failed.");
            return false;
        }

        public void StartSession()
        {
            if (!_canSendEvents) return;

            // 1. Check for and resend a session_end event for a previous, unclosed session
            _ = CheckForAndSendMissingSessionEndAsync();

            // 2. Start new session
            _sessionId = Guid.NewGuid().ToString().ToLower();
            _sessionStartDate = DateTime.UtcNow;
            IncrementSessionCount();

            // 3. Start the flush timer
            SetupSendTimer();

            Debug.WriteLine($"[GA] Session started with ID: {_sessionId}");

            // 4. Send 'user' event
            AddUserEvent();

            _ = SendQueueAsync();
        }

        public async Task EndSessionAsync()
        {
            if (!_canSendEvents || string.IsNullOrEmpty(_sessionId))
            {
                return;
            }

            // 1. Stop the flush timer
            InvalidateSendTimer();

            var sessionLength = (DateTime.UtcNow - _sessionStartDate).TotalSeconds;
            var sessionEndEvent = CreateDefaultAnnotations();
            sessionEndEvent["category"] = "session_end";
            sessionEndEvent["length"] = Math.Max(0, (int)sessionLength);

            // 2. Add session_end event to the queue and force send everything
            AddEventToQueue(sessionEndEvent);
            await SendQueueAsync();

            Debug.WriteLine("[GA] Session ended.");

            // 3. Clear session state
            _sessionId = null;
            _sessionStartDate = default;
            ClearSessionStateForRecovery();
        }

        // --- Event Adding Methods ---
        public void AddUserEvent()
        {
            if (!_canSendEvents || string.IsNullOrEmpty(_sessionId)) return;

            var userEvent = CreateDefaultAnnotations();
            userEvent["category"] = "user";

            AddEventToQueue(userEvent);
        }

        public void AddDesignEvent(string eventId)
        {
            if (!_canSendEvents || string.IsNullOrEmpty(_sessionId)) return;

            var designEvent = CreateDefaultAnnotations();
            designEvent["category"] = "design";
            designEvent["event_id"] = eventId;

            AddEventToQueue(designEvent);
        }

        public void AddDesignEvent(string eventId, float amount)
        {
            if (!_canSendEvents || string.IsNullOrEmpty(_sessionId)) return;

            var designEvent = CreateDefaultAnnotations();
            designEvent["category"] = "design";
            designEvent["event_id"] = eventId;
            designEvent["value"] = amount;

            AddEventToQueue(designEvent);
        }

        public void AddBusinessEvent(string itemType, string itemId, int amount, string currency, int transactionNum)
        {
            if (!_canSendEvents || string.IsNullOrEmpty(_sessionId)) return;

            if (string.IsNullOrEmpty(itemType) || string.IsNullOrEmpty(itemId))
            {
                Debug.WriteLine("[GA] Business event requires a valid itemType and itemId.");
                return;
            }

            var eventId = $"{itemType}:{itemId}";
            var businessEvent = CreateDefaultAnnotations();
            businessEvent["category"] = "business";
            businessEvent["event_id"] = eventId;
            businessEvent["amount"] = amount;
            businessEvent["currency"] = currency;
            businessEvent["transaction_num"] = transactionNum;

            AddEventToQueue(businessEvent);
        }

        public void AddProgressionEvent(ProgressionStatus status, string p01, string p02 = null, string p03 = null, int? score = null)
        {
            if (!_canSendEvents || string.IsNullOrEmpty(_sessionId)) return;

            var eventData = CreateDefaultAnnotations();
            eventData["category"] = "progression";
            eventData["event_id"] = FormatProgressionEventId(status, p01, p02, p03);

            if (status == ProgressionStatus.Fail || status == ProgressionStatus.Complete)
            {
                // Note: comprehensive attempt_num tracking would require its own persistent storage.
                eventData["attempt_num"] = 1;
            }
            if (score.HasValue)
            {
                eventData["score"] = score.Value;
            }

            AddEventToQueue(eventData);
        }

        public void AddResourceEvent(ResourceFlowType flowType, string virtualCurrency, string itemType, string itemId, float amount)
        {
            if (!_canSendEvents || string.IsNullOrEmpty(_sessionId)) return;

            var eventData = CreateDefaultAnnotations();
            eventData["category"] = "resource";
            var flowTypeString = flowType == ResourceFlowType.Source ? "Source" : "Sink";
            eventData["event_id"] = $"{flowTypeString}:{virtualCurrency}:{itemType}:{itemId}";
            eventData["amount"] = amount;

            AddEventToQueue(eventData);
        }

        public void AddErrorEvent(ErrorType severity, string message)
        {
            if (!_canSendEvents || string.IsNullOrEmpty(_sessionId)) return;

            var eventData = CreateDefaultAnnotations();
            eventData["category"] = "error";
            eventData["severity"] = severity.ToString().ToLower();
            eventData["message"] = message ?? "";

            AddEventToQueue(eventData);
        }

        #region Core Logic

        private void AddEventToQueue(Dictionary<string, object> eventPayload)
        {
            int queueCount;
            lock (_eventQueue)
            {
                _eventQueue.Add(eventPayload);
                queueCount = _eventQueue.Count;
            }

            // Save state for session recovery, unless it's the session_end event itself
            if ((string)eventPayload["category"] != "session_end")
            {
                SaveSessionStateForRecovery(eventPayload);
            }

            // Check if batch size limit is reached and trigger a send
            if (queueCount >= GA_BATCH_SIZE_LIMIT)
            {
                Debug.WriteLine("[GA] Batch size limit reached, flushing queue.");
                _ = SendQueueAsync();
            }
        }

        private async Task SendQueueAsync()
        {
            if (!_canSendEvents)
            {
                return;
            }

            if (!await _sendSemaphore.WaitAsync(0))
            {
                return;
            }

            try
            {
                var url = $"{_baseUrl}/v2/{_gameKey}/events";

                while (true)
                {
                    List<Dictionary<string, object>> eventsToSend;
                    lock (_eventQueue)
                    {
                        if (_eventQueue.Count == 0)
                        {
                            return;
                        }

                        eventsToSend = _eventQueue
                            .Take(GA_BATCH_SIZE_LIMIT)
                            .ToList();
                    }

                    var response = await SendRequestAsync(url, eventsToSend);

                    if (response.IsSuccess)
                    {
                        lock (_eventQueue)
                        {
                            _eventQueue.RemoveAll(e => eventsToSend.Contains(e));
                            Debug.WriteLine($"[GA] Successfully sent {eventsToSend.Count} events. {_eventQueue.Count} remaining.");
                        }
                        continue;
                    }

                    if (response.ShouldDropEvents)
                    {
                        lock (_eventQueue)
                        {
                            _eventQueue.RemoveAll(e => eventsToSend.Contains(e));
                            Debug.WriteLine($"[GA] Dropped {eventsToSend.Count} invalid events (HTTP 400/401). {_eventQueue.Count} remaining.");
                        }
                        continue;
                    }

                    Debug.WriteLine($"[GA] Failed to send {eventsToSend.Count} events. They will be retried later.");
                    return;
                }
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        private async Task<GaHttpResponse> SendRequestAsync(string url, object payload)
        {
            // 1. 手动将 C# 对象序列化为 JSON 字符串。
            // 使用紧凑的格式，不进行任何美化，以减少数据量并确保一致性。
            var payloadJson = JsonConvert.SerializeObject(payload, Formatting.None);

            // 2. 使用这个确定的字符串来生成 HMAC 签名。
            var signature = GenerateHmacSha256(payloadJson, _secretKey);

            try
            {
                // 3. 使用 PostStringAsync 方法发送我们刚刚创建的字符串。
                //    这样可以保证发送的请求体与用于签名的字符串完全一致。
                var response = await url
                    .WithHeader("Authorization", signature)
                    .WithHeader("Content-Type", "application/json") // 明确指定内容类型
                    .PostStringAsync(payloadJson)
                    .ReceiveString();

                // 使用 Debug.WriteLine 代替 FLLogDebug
                Debug.WriteLine($"[GA] 成功响应: {response}");
                return new GaHttpResponse(response, isSuccess: true);
            }
            catch (FlurlHttpException ex)
            {
                var statusCode = ex.Call?.Response?.StatusCode;
                string errorResponse = string.Empty;
                try
                {
                    errorResponse = await ex.GetResponseStringAsync();
                }
                catch
                {
                    // ignored - we still log the exception message below
                }

                // 详细记录错误，包括请求的 JSON 内容，这对于调试至关重要！
                Debug.WriteLine($"[GA] 请求失败: {ex.Message}");
                Debug.WriteLine($"[GA] 失败的负载: {payloadJson}");
                Debug.WriteLine($"[GA] 服务器响应: {errorResponse}");
                return new GaHttpResponse(
                    errorResponse,
                    isSuccess: false,
                    shouldDropEvents: statusCode == 400 || statusCode == 401);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GA] 请求期间发生意外错误: {ex.Message}");
                Debug.WriteLine($"[GA] 发生意外错误的负载: {payloadJson}");
                return new GaHttpResponse(string.Empty, isSuccess: false);
            }
        }

        private Dictionary<string, object> CreateDefaultAnnotations()
        {
            return new Dictionary<string, object>
        {
            { "v", 2 },
            { "user_id", _userId },
            { "client_ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            { "sdk_version", _sdkVersion },
            { "os_version", _osVersion },
            { "manufacturer", _manufacturer },
            { "device", _device },
            { "platform", _platform },
            { "session_id", _sessionId ?? "" },
            { "session_num", GetSessionCount() },
            { "build", AppUtils.BundleFullVersion() },
        };
        }

        #endregion

        #region Timer Management
        private void SetupSendTimer()
        {
            InvalidateSendTimer();
            Debug.WriteLine($"[GA] Setting up flush timer with interval: {GA_FLUSH_INTERVAL_SECONDS} seconds.");
            _sendTimer = new Timer(
                callback: _ => FlushQueueByTimer(),
                state: null,
                dueTime: TimeSpan.FromSeconds(GA_FLUSH_INTERVAL_SECONDS),
                period: TimeSpan.FromSeconds(GA_FLUSH_INTERVAL_SECONDS)
            );
        }

        private void InvalidateSendTimer()
        {
            if (_sendTimer != null)
            {
                Debug.WriteLine("[GA] Invalidating flush timer.");
                _sendTimer.Dispose();
                _sendTimer = null;
            }
        }

        private void FlushQueueByTimer()
        {
            Debug.WriteLine("[GA] Flushing queue by timer.");
            _ = SendQueueAsync();
        }
        #endregion

        #region Session Recovery
        private void SaveSessionStateForRecovery(Dictionary<string, object> eventPayload)
        {
            if (string.IsNullOrEmpty(_sessionId)) return;

            var recoveryData = new Dictionary<string, object>
        {
            {"session_id", _sessionId},
            {"session_start_ts", new DateTimeOffset(_sessionStartDate).ToUnixTimeSeconds()},
            {"last_event_annotations", eventPayload }
        };

            _mmkv.SetValue(SESSION_RECOVERY_KEY, recoveryData);
        }

        private void ClearSessionStateForRecovery()
        {
            _mmkv.RemoveValue(SESSION_RECOVERY_KEY);
        }

        private async Task CheckForAndSendMissingSessionEndAsync()
        {
            var recoveryData = _mmkv.GetObject<JObject>(SESSION_RECOVERY_KEY);
            if (recoveryData == null) return;

            Debug.WriteLine("[GA] Detected an unclosed session. Recovering...");

            var lastSessionId = recoveryData["session_id"]?.ToString();
            var sessionStartTs = recoveryData["session_start_ts"]?.Value<long>() ?? 0;
            var lastAnnotations = recoveryData["last_event_annotations"]?.ToObject<Dictionary<string, object>>();
            if (string.IsNullOrWhiteSpace(lastSessionId) ||
                lastAnnotations == null ||
                !lastAnnotations.TryGetValue("client_ts", out var lastEventTsValue) ||
                !long.TryParse(lastEventTsValue?.ToString(), out var lastEventTs))
            {
                Debug.WriteLine("[GA] Recovery data is incomplete. Clearing stale session recovery state.");
                ClearSessionStateForRecovery();
                return;
            }

            var sessionLength = (int)(lastEventTs - sessionStartTs);

            var recoveredEvent = new Dictionary<string, object>(lastAnnotations)
            {
                ["category"] = "session_end",
                ["length"] = Math.Max(0, sessionLength),
                ["session_id"] = lastSessionId
            };

            lock (_eventQueue)
            {
                _eventQueue.Insert(0, recoveredEvent);
            }

            Debug.WriteLine($"[GA] Recovered session_end event added to queue for session: {lastSessionId}");

            // Immediately try to send the recovered event along with any others.
            await SendQueueAsync();

            ClearSessionStateForRecovery();
        }
        #endregion

        #region Session Counting
        private void IncrementSessionCount()
        {
            var count = GetSessionCount();
            _mmkv.SetValue(_userId, count + 1);
        }

        private int GetSessionCount()
        {
            // GetValue returns 0 (default for int) if the key doesn't exist, which is correct for the first session.
            return _mmkv.GetValue<int>(_userId, 0);
        }
        #endregion

        #region Helpers
        private string GenerateHmacSha256(string data, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(dataBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private string FormatProgressionEventId(ProgressionStatus status, string p01, string p02, string p03)
        {
            var statusString = status.ToString(); // "Start", "Fail", "Complete"
            var eventIdBuilder = new StringBuilder(statusString);

            if (!string.IsNullOrEmpty(p01)) eventIdBuilder.Append($":{p01}");
            if (!string.IsNullOrEmpty(p02)) eventIdBuilder.Append($":{p02}");
            if (!string.IsNullOrEmpty(p03)) eventIdBuilder.Append($":{p03}");

            return eventIdBuilder.ToString();
        }

        private bool IsInitEnabled(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return false;
            }

            try
            {
                var responseJson = JObject.Parse(responseBody);
                var enabledToken = responseJson["enabled"];
                if (enabledToken != null && enabledToken.Type == JTokenType.Boolean)
                {
                    return enabledToken.Value<bool>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GA] Failed to parse init response. Fallback to legacy disabled check. {ex.Message}");
            }

            return !responseBody.Contains("disabled", StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            // Ensure the timer is stopped and disposed of when the client is no longer needed.
            InvalidateSendTimer();
            _sendSemaphore.Dispose();
        }

        #endregion
    }

    internal class GaHttpResponse
    {
        public GaHttpResponse(string responseBody, bool isSuccess, bool shouldDropEvents = false)
        {
            ResponseBody = responseBody;
            IsSuccess = isSuccess;
            ShouldDropEvents = shouldDropEvents;
        }

        public string ResponseBody { get; }
        public bool IsSuccess { get; }
        public bool ShouldDropEvents { get; }
    }
}
