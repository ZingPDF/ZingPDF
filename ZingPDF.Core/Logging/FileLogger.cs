namespace ZingPdf.Core.Logging
{
    public class FileLogger
    {
        private readonly LogLevel _logLevel;
        private readonly StreamWriter _logFileWriter;

        public FileLogger(string fileNamePrefix, LogLevel logLevel)
        {
            _logFileWriter = new StreamWriter($"{fileNamePrefix}-{DateTime.Now.Date:yyyy-dd-M}.log", append: true);

            _logLevel = logLevel;
        }

        public void Log(LogLevel level, string message)
        {
            if (level < _logLevel)
            {
                return;
            }

            //Write log messages to text file
            _logFileWriter.WriteLine($"[{_logLevel}] {message}");
            _logFileWriter.Flush();
        }
    }
}