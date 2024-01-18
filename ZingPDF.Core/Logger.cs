using ZingPdf.Core.Logging;

namespace ZingPdf.Core
{
    public static class Logger
    {
        private static readonly FileLogger _logger = new("debug-log", LogLevel.Trace);

        public static LogLevel LogLevel { get; set; } = LogLevel.Error;

        public static void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }
    }
}
