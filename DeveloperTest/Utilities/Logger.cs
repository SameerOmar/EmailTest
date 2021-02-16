using System;

namespace DeveloperTest.Utilities
{
    internal class Logger
    {
        private static Logger _logger;

        public static Logger Instance => _logger ?? (_logger = new Logger());

        public void LogError(Exception e, string message)
        {
            // a logic for writing errors to log file
        }

        public void LogInfo(Exception e, string message)
        {
            // a logic for writing information to log file
        }
    }
}