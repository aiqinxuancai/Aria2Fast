using System;
using System.IO;
using LiteDB;

namespace Aria2Fast.Service
{
    internal class PushDeerPushHistoryStore
    {
        private const string kCollectionName = "pushdeer_completed";

        private static readonly Lazy<PushDeerPushHistoryStore> _instance =
            new Lazy<PushDeerPushHistoryStore>(() => new PushDeerPushHistoryStore());

        public static PushDeerPushHistoryStore Instance => _instance.Value;

        private readonly string _dbPath;
        private readonly object _lock = new object();

        private PushDeerPushHistoryStore()
        {
            _dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Aria2FastPushHistory.db");
            EnsureCollection();
        }

        public bool TryMarkTaskPushed(string taskIdentity, string taskName)
        {
            if (string.IsNullOrWhiteSpace(taskIdentity))
            {
                return false;
            }

            var normalizedTaskIdentity = taskIdentity.Trim();

            lock (_lock)
            {
                try
                {
                    using var db = new LiteDatabase(_dbPath);
                    var col = db.GetCollection<PushDeerPushRecord>(kCollectionName);

                    if (col.Exists(a => a.TaskIdentity == normalizedTaskIdentity))
                    {
                        return false;
                    }

                    col.Insert(new PushDeerPushRecord()
                    {
                        TaskIdentity = normalizedTaskIdentity,
                        TaskName = taskName ?? string.Empty,
                        CreatedAtUtc = DateTime.UtcNow
                    });

                    return true;
                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error($"PushDeer dedup record failed: {normalizedTaskIdentity} {ex}");
                    // Strict once behavior: if dedup state cannot be confirmed, skip sending.
                    return false;
                }
            }
        }

        private void EnsureCollection()
        {
            lock (_lock)
            {
                try
                {
                    using var db = new LiteDatabase(_dbPath);
                    var col = db.GetCollection<PushDeerPushRecord>(kCollectionName);
                    col.EnsureIndex(a => a.TaskIdentity, true);
                }
                catch (Exception ex)
                {
                    EasyLogManager.Logger.Error($"PushDeer dedup db init failed: {ex}");
                }
            }
        }

        private class PushDeerPushRecord
        {
            [BsonId]
            public string TaskIdentity { get; set; } = string.Empty;

            public string TaskName { get; set; } = string.Empty;

            public DateTime CreatedAtUtc { get; set; }
        }
    }
}
