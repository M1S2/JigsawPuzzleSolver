using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Drawing;

namespace LogBox
{
    /// <summary>
    /// Interaktionslogik für LogBox.xaml
    /// </summary>
    public partial class LogBoxControl : UserControl
    {
        /// <summary>
        /// List with all log events
        /// </summary>
        public ObservableCollection<LogEvent> LogEvents { get; private set; }

        /// <summary>
        /// List with all log events that are shown (not filtered)
        /// </summary>
        public ObservableCollection<LogEvent> ShownLogEvents { get; private set; }

        private bool _showInfos;
        /// <summary>
        /// Show all INFO events
        /// </summary>
        public bool ShowInfos
        {
            get { return _showInfos; }
            set
            {
                _showInfos = value;
                RefreshShownLogEvents();
            }
        }

        private bool _showWarnings;
        /// <summary>
        /// Show all WARNING events
        /// </summary>
        public bool ShowWarnings
        {
            get { return _showWarnings; }
            set
            {
                _showWarnings = value;
                RefreshShownLogEvents();
            }
        }

        private bool _showErrors;
        /// <summary>
        /// Show all ERROR events
        /// </summary>
        public bool ShowErrors
        {
            get { return _showErrors; }
            set
            {
                _showErrors = value;
                RefreshShownLogEvents();
            }
        }

        private bool _showImageLogs;
        /// <summary>
        /// Show all IMAGE events
        /// </summary>
        public bool ShowImageLogs
        {
            get { return _showImageLogs; }
            set
            {
                _showImageLogs = value;
                RefreshShownLogEvents();
            }
        }

        //***********************************************************************************************************************************************************************************************************

        public LogBoxControl()
        {
            InitializeComponent();
            DataContext = this;
            LogEvents = new ObservableCollection<LogEvent>();
            ShownLogEvents = new ObservableCollection<LogEvent>();
            listView_Log.ItemsSource = ShownLogEvents;
            ShowInfos = true;
            ShowWarnings = true;
            ShowErrors = true;
            ShowImageLogs = true;
        }

        //***********************************************************************************************************************************************************************************************************

        private void btn_clearLog_Click(object sender, RoutedEventArgs e)
        {
            ClearLog();
        }

        //***********************************************************************************************************************************************************************************************************

        /// <summary>
        /// Create a new error log entry with time and message
        /// </summary>
        /// <param name="errorMessage">error message</param>
        public void LogError(string errorMessage)
        {
            LogEventError logEvent = new LogEventError(DateTime.Now, errorMessage);
            LogEvent(logEvent);
        }

        /// <summary>
        /// Create a new warning log entry with time and message
        /// </summary>
        /// <param name="warningMessage">warning message</param>
        public void LogWarning(string warningMessage)
        {
            LogEventWarning logEvent = new LogEventWarning(DateTime.Now, warningMessage);
            LogEvent(logEvent);
        }

        /// <summary>
        /// Create a new info log entry with time and message
        /// </summary>
        /// <param name="infoMessage">info message</param>
        public void LogInfo(string infoMessage)
        {
            LogEventInfo logEvent = new LogEventInfo(DateTime.Now, infoMessage);
            LogEvent(logEvent);
        }

        /// <summary>
        /// Create a new image log entry with time and message
        /// </summary>
        /// <param name="imageMessage">image message</param>
        /// <param name="image">image of log entry</param>
        public void LogImage(string imageMessage, Bitmap image)
        {
            LogEventImage logEvent = new LogEventImage(DateTime.Now, imageMessage, image);
            LogEvent(logEvent);
        }

        //***********************************************************************************************************************************************************************************************************

        /// <summary>
        /// Create a new log entry with type, time and message
        /// </summary>
        /// <param name="logEvent">log event</param>
        private void LogEvent(LogEvent logEvent)
        {
            LogEvents.Add(logEvent);
            RefreshShownLogEvents();
            listView_Log.ScrollIntoView(logEvent);
        }

        /// <summary>
        /// Clear all log entries
        /// </summary>
        public void ClearLog()
        {
            LogEvents.Clear();
            ShownLogEvents.Clear();
        }

        //***********************************************************************************************************************************************************************************************************

        /// <summary>
        /// Refresh the list of shown log entries
        /// </summary>
        private void RefreshShownLogEvents()
        {
            ShownLogEvents = new ObservableCollection<LogEvent>(LogEvents.Where(log => (ShowInfos == true && log.LogType == LogTypes.INFO) || (ShowWarnings == true && log.LogType == LogTypes.WARNING) || (ShowErrors == true && log.LogType == LogTypes.ERROR) || (ShowImageLogs == true && log.LogType == LogTypes.IMAGE)));
            listView_Log.ItemsSource = ShownLogEvents;
            resizeListViewColumns(listView_Log);
        }

        //***********************************************************************************************************************************************************************************************************

        /// <summary>
        /// Autosize the columns of the listView
        /// </summary>
        /// see: https://dotnet-snippets.de/snippet/automatische-anpassung-der-breite-von-gridviewcolumns/1286
        private void resizeListViewColumns(ListView listView)
        {
            GridView gridView = listView.View as GridView;
            if (gridView == null) { return; }

            foreach (GridViewColumn column in gridView.Columns)
            {
                if(column.Header.ToString() == "Message") { continue; }
                column.Width = column.ActualWidth;
                column.Width = double.NaN;
            }
        }

    }
}
