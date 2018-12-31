using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogBox
{
    /// <summary>
    /// Log event warning
    /// </summary>
    public class LogEventWarning : LogEvent
    {
        /// <summary>
        /// Constructor of LogEventWarning
        /// </summary>
        /// <param name="logTime">Time of log entry</param>
        /// <param name="logMessage">Message of log entry</param>
        public LogEventWarning(DateTime logTime, string logMessage) : base(LogTypes.WARNING, logTime, logMessage)
        {
        }
    }
}
