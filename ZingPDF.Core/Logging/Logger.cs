namespace ZingPdf.Core.Logging
{
    // TODO: For performance reasons, all calls to this logger might need to be compiled out, maybe with a compiler directive
    public static class Logger
    {
        private static readonly FileLogger _logger = new("debug-log", LogLevel.Trace);

        public static LogLevel LogLevel { get; set; } = LogLevel.Error;

        public static void Log(LogLevel level, string message)
        {
#if !RELEASE
            //_logger.Log(level, message);
            //Console.WriteLine(message);
#endif
        }
    }
}
