using Microsoft.Extensions.Logging;
using System;
using ZLogger;

namespace Aria2Fast.Utils
{
    public class SimpleLogger : IDisposable
    {
        private readonly ILogger logger;
        private readonly ILoggerFactory loggerFactory;

        public SimpleLogger()
        {
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);



                // 添加控制台输出
                builder.AddZLoggerConsole(options =>
                {
                    options.UsePlainTextFormatter(formatter =>
                    {
                        // 使用 Format 方法格式化时间和日志级别
                        formatter.SetPrefixFormatter($"{0}|{1}|", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));
                    });
                });

                // 添加文件输出
                var logFilename = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".log";
                builder.AddZLoggerFile(logFilename, options =>
                {
                    options.UsePlainTextFormatter(formatter =>
                    {
                        formatter.SetPrefixFormatter($"{0}|{1}|", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));
                    });
                });


                //builder.AddDebug();
            });

            logger = loggerFactory.CreateLogger("SimpleLogger");
        }

        public void Debug(string text)
        {
            logger.ZLogDebug($"{text}");
        }

        public void Debug(object text)
        {
            logger.ZLogDebug($"{text?.ToString() ?? "null"}");
        }

        public void Error(string text)
        {
            logger.ZLogError($"{text}");
        }

        public void Error(object text)
        {
            logger.ZLogError($"{text?.ToString() ?? "null"}");
        }

        public void Fatal(string text)
        {
            logger.ZLogCritical($"{text}");
        }

        public void Info(string text)
        {
            logger.ZLogInformation($"{text}");
        }

        public void Info(object text)
        {
            logger.ZLogInformation($"{text?.ToString() ?? "null"}");
        }

        public void Info(string text, params object[] args)
        {
            logger.ZLogInformation($"{string.Format(text, args)}");
        }

        public void Trace(string text)
        {
            logger.ZLogTrace($"{text}");
        }

        public void Warning(string text)
        {
            logger.ZLogWarning($"{text}");
        }

        public void Dispose()
        {
            loggerFactory?.Dispose();
        }
    }
}
