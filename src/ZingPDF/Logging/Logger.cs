namespace ZingPDF.Logging
{
    // TODO: For performance reasons, all calls to this logger might need to be compiled out, maybe with a compiler directive
    public static class Logger
    {
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
