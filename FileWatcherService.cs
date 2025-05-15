using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace FileWatcherService
{
    public partial class FileWatcherService : ServiceBase
    {
        private FileSystemWatcher _watcher;
        private string _sourceFolder = @"C:\Folder1";
        private string _destinationFolder = @"C:\Folder2";
        private string _logFilePath = @"C:\FileWatcherService.log";
        private EventLog _eventLog;

        public FileWatcherService()
        {
            InitializeComponent();

            // Configure EventLog
            if (!EventLog.SourceExists("FileWatcherService"))
            {
                EventLog.CreateEventSource("FileWatcherService", "Application");
            }

            _eventLog = new EventLog();
            _eventLog.Source = "FileWatcherService";
            _eventLog.Log = "Application";
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                // Create directories if they don't exist
                if (!Directory.Exists(_sourceFolder))
                {
                    Directory.CreateDirectory(_sourceFolder);
                    LogEvent($"Created source directory: {_sourceFolder}", EventLogEntryType.Information);
                }

                if (!Directory.Exists(_destinationFolder))
                {
                    Directory.CreateDirectory(_destinationFolder);
                    LogEvent($"Created destination directory: {_destinationFolder}", EventLogEntryType.Information);
                }

                // Initialize FileSystemWatcher
                _watcher = new FileSystemWatcher
                {
                    Path = _sourceFolder,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                    Filter = "*.*",
                    EnableRaisingEvents = true
                };

                // Add event handlers
                _watcher.Created += OnFileCreated;

                LogEvent("File Watcher Service started successfully", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                LogEvent($"Error starting service: {ex.Message}", EventLogEntryType.Error);
                throw;
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Give the system some time to fully create/write the file
                System.Threading.Thread.Sleep(1000);

                string destinationFilePath = Path.Combine(_destinationFolder, Path.GetFileName(e.FullPath));

                // Ensure destination file doesn't exist or delete it if it does
                if (File.Exists(destinationFilePath))
                {
                    File.Delete(destinationFilePath);
                }

                // Move the file
                if (e.FullPath != null)
                {
                    File.Move(e.FullPath, destinationFilePath);

                    LogEvent($"Moved file from {e.FullPath} to {destinationFilePath}", EventLogEntryType.Information);
                }
            }
            catch (Exception ex)
            {
                LogEvent($"Error processing file {e.FullPath}: {ex.Message}", EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            // Cleanup
            if (_watcher != null)
            {
                _watcher.Created -= OnFileCreated;
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }

            LogEvent("File Watcher Service stopped", EventLogEntryType.Information);
        }

        private void LogEvent(string message, EventLogEntryType eventType)
        {
            // Log to Event Viewer
            _eventLog.WriteEntry(message, eventType);

            // Log to rolling file
            try
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {eventType}: {message}";

                // Create directory for log file if it doesn't exist
                string logDirectory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Implement simple rolling log file (roll when file reaches 5MB)
                if (File.Exists(_logFilePath) && new FileInfo(_logFilePath).Length > 5 * 1024 * 1024)
                {
                    string backupLogFile = $"{_logFilePath}_{DateTime.Now:yyyyMMddHHmmss}.bak";
                    File.Move(_logFilePath, backupLogFile);
                }

                // Append to log file
                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // If file logging fails, at least log to Event Viewer
                _eventLog.WriteEntry($"Failed to write to log file: {ex.Message}", EventLogEntryType.Error);
            }
        }
    }
}