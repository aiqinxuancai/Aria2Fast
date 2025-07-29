using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aria2Fast.Services
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Aria2Fast.Services.GameAnalytics;

    /// <summary>
    /// 管理 GameAnalyticsClient 的生命周期并为其提供一个全局访问点。
    /// 该类以单例模式实现。
    /// </summary>
    public class GameAnalyticsManager : IDisposable
    {
        #region Singleton Implementation (单例实现)

        private static GameAnalyticsManager _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取 GameAnalyticsManager 的单例实例。
        /// </summary>
        public static GameAnalyticsManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new GameAnalyticsManager();
                        _instance.Configure(
                            "https://api.gameanalytics.com",
                            "aca53353ee41ad923daf4b2878c564d2",
                            "b4fcd2e562b0583b9d728efe7102c5b81d07e142"
                            );
                    }
                    return _instance;
                }
            }
        }

        /// <summary>
        /// 私有构造函数，以防止直接实例化。
        /// </summary>
        private GameAnalyticsManager() { }

        #endregion

        // 客户端和状态标志
        private GameAnalyticsClient _client;
        private bool _isInitialized = false;
        private bool _isConfigured = false;

        // 配置信息
        private string _baseUrl;
        private string _gameKey;
        private string _secretKey;

        /// <summary>
        /// 获取一个值，该值指示 GameAnalytics 服务是否已初始化。
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #region Lifecycle Management (生命周期管理)

        /// <summary>
        /// 配置 GameAnalyticsManager，设置必要的 API 密钥和 URL。
        /// 这个方法必须在整个应用程序生命周期中首先被调用，且只调用一次。
        /// </summary>
        /// <param name="baseUrl">GameAnalytics API 的基础 URL。</param>
        /// <param name="gameKey">您的游戏的 Game Key。</param>
        /// <param name="secretKey">您的游戏的 Secret Key。</param>
        public void Configure(string baseUrl, string gameKey, string secretKey)
        {
            if (_isConfigured)
            {
                Debug.WriteLine("[GA Manager] GameAnalytics 已被配置，请勿重复调用。");
                return;
            }
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(gameKey) || string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentException("Base URL, Game Key, 和 Secret Key 不能为空。");
            }

            _baseUrl = baseUrl;
            _gameKey = gameKey;
            _secretKey = secretKey;
            _isConfigured = true;

            Debug.WriteLine("[GA Manager] 配置完成。");
        }

        /// <summary>
        /// 使用指定的用户ID初始化 GameAnalytics 客户端并启动一个会话。
        /// 此方法必须在 Configure() 方法被调用后执行。
        /// </summary>
        /// <param name="userId">当前用户的唯一标识符。</param>
        /// <returns>代表异步初始化过程的任务。</returns>
        public async Task InitializeAsync(string userId)
        {
            if (_isInitialized)
            {
                Debug.WriteLine("[GA Manager] GameAnalytics 已初始化。");
                return;
            }

            if (!_isConfigured)
            {
                // 这是一个严重错误，直接抛出异常来提醒开发者正确的调用顺序。
                throw new InvalidOperationException("GameAnalyticsManager 尚未配置。请在调用 InitializeAsync 之前先调用 Configure() 方法。");
            }

            Debug.WriteLine("[GA Manager] 正在初始化...");

            // 使用存储在 Manager 内部的配置来创建客户端
            _client = new GameAnalyticsClient(_baseUrl, _gameKey, _secretKey, userId);

            var success = await _client.InitializeAsync();

            if (success)
            {
                _isInitialized = true;
                _client.StartSession();
                Debug.WriteLine("[GA Manager] 初始化完成并已启动会话。");
            }
            else
            {
                Debug.WriteLine("[GA Manager] 初始化失败。事件将不会被发送。");
                _client = null; // 如果初始化失败，则清空客户端
            }
        }

        /// <summary>
        /// 结束当前会话，发送所有剩余的事件，并清理资源。
        /// 该方法应该在应用程序关闭时调用。
        /// </summary>
        /// <returns>代表异步关闭过程的任务。</returns>
        public async Task ShutdownAsync()
        {
            if (!_isInitialized || _client == null) return;

            Debug.WriteLine("[GA Manager] 正在关闭...");
            await _client.EndSessionAsync();
            _client.Dispose();
            _isInitialized = false;
            Debug.WriteLine("[GA Manager] 关闭完成。");
        }

        /// <summary>
        /// 实现 IDisposable 接口以确保在关闭期间进行清理。
        /// </summary>
        public void Dispose()
        {
            ShutdownAsync().GetAwaiter().GetResult();
        }

        #endregion

        #region Event Tracking API (事件跟踪接口)

        /// <summary>
        /// 跟踪一个设计事件，用于追踪玩家的行为和选择。
        /// </summary>
        /// <param name="eventId">一个用于标识事件的字符串，例如："MainMenu:StartButtonPressed"。</param>
        public void TrackDesignEvent(string eventId)
        {
            if (!IsInitialized) return;
            _client.AddDesignEvent(eventId);
        }

        /// <summary>
        /// 跟踪一个带数值的设计事件。
        /// </summary>
        /// <param name="eventId">一个用于标识事件的字符串。</param>
        /// <param name="value">与事件关联的一个浮点数值。</param>
        public void TrackDesignEvent(string eventId, float value)
        {
            if (!IsInitialized) return;
            _client.AddDesignEvent(eventId, value);
        }

        /// <summary>
        /// 跟踪一个商业事件，通常用于真实货币交易。
        /// </summary>
        /// <param name="itemType">购买的物品类型（例如："GemsBundle"）。</param>
        /// <param name="itemId">物品的具体 ID（例如："GEM_PACK_100"）。</param>
        /// <param name="amount">物品的价格，以最小货币单位计算（例如：美分）。</param>
        /// <param name="currency">ISO 4217 标准的货币代码（例如："USD"）。</param>
        /// <param name="transactionNum">一个唯一的交易编号。</param>
        public void TrackBusinessEvent(string itemType, string itemId, int amount, string currency, int transactionNum)
        {
            if (!IsInitialized) return;
            _client.AddBusinessEvent(itemType, itemId, amount, currency, transactionNum);
        }

        /// <summary>
        /// 跟踪玩家在游戏中的进度。
        /// </summary>
        /// <param name="status">进度的状态（Start, Fail, Complete）。</param>
        /// <param name="p01">进度第一层级（例如："World1"）。</param>
        /// <param name="p02">进度第二层级（例如："Level5"）。可以为 null。</param>
        /// <param name="p03">进度第三层级（例如："BossFight"）。可以为 null。</param>
        /// <param name="score">玩家在此进度中的得分。可以为 null。</param>
        public void TrackProgressionEvent(ProgressionStatus status, string p01, string p02 = null, string p03 = null, int? score = null)
        {
            if (!IsInitialized) return;
            _client.AddProgressionEvent(status, p01, p02, p03, score);
        }

        /// <summary>
        /// 跟踪玩家资源余额的变化（例如：获得或花费金币）。
        /// </summary>
        /// <param name="flowType">流向类型：Source (获取) 或 Sink (消耗)。</param>
        /// <param name="virtualCurrency">虚拟货币的名称（例如："Coins"）。</param>
        /// <param name="itemType">购买的物品类型或货币来源的类型（例如："PowerUp", "DailyBonus"）。</param>
        /// <param name="itemId">物品或来源的具体 ID（例如："ExtraLife", "LoginReward_Day5"）。</param>
        /// <param name="amount">获得或花费的货币数量。</param>
        public void TrackResourceEvent(ResourceFlowType flowType, string virtualCurrency, string itemType, string itemId, float amount)
        {
            if (!IsInitialized) return;
            _client.AddResourceEvent(flowType, virtualCurrency, itemType, itemId, amount);
        }

        /// <summary>
        /// 从应用程序中跟踪一个错误事件。
        /// </summary>
        /// <param name="severity">错误的严重级别（例如：Error, Critical）。</param>
        /// <param name="message">错误的描述性信息。</param>
        public void TrackErrorEvent(ErrorType severity, string message)
        {
            if (!IsInitialized) return;
            _client.AddErrorEvent(severity, message);
        }

        #endregion
    }
}
