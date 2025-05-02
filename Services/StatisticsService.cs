using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Collections.Generic;
using MnemoProject.Services;
using MnemoProject.Models;

namespace MnemoProject.Services
{
    public class StatisticsService
    {
        private static readonly StatisticsService _instance = new();
        public static StatisticsService Instance => _instance;

        // Event for notifying when statistics are updated
        public static event EventHandler StatisticsUpdated;

        // Cached statistics to avoid repeatedly loading from file
        private static UserStatistics _cachedStatistics = new()
        {
            WeeklyStudyTimeSeconds = 0,
            LongestStreak = 0,
            Retention = 0,
            StudyGoalMinutes = 15,
            CurrentStudyGoalMinutes = 0
        };

        // Static methods for easy access from anywhere in the application
        public static object Get(string statisticName)
        {
            var stats = _cachedStatistics ?? Instance.LoadStatistics();
            return statisticName switch
            {
                "WeeklyStudyTimeSeconds" => stats.WeeklyStudyTimeSeconds,
                "LongestStreak" => stats.LongestStreak,
                "Retention" => stats.Retention,
                "StudyGoalMinutes" => stats.StudyGoalMinutes,
                "CurrentStudyGoalMinutes" => stats.CurrentStudyGoalMinutes,
                _ => throw new ArgumentException($"Unknown statistic name: {statisticName}")
            };
        }

        // Generic type-safe getter
        public static T Get<T>(string statisticName)
        {
            var value = Get(statisticName);
            
            if (value is T typedValue)
            {
                return typedValue;
            }
            
            // Handle numeric type conversions
            if (typeof(T) == typeof(int) && value is double doubleValue)
            {
                return (T)(object)Convert.ToInt32(doubleValue);
            }
            
            if (typeof(T) == typeof(double) && value is int intValue)
            {
                return (T)(object)Convert.ToDouble(intValue);
            }
            
            if (typeof(T) == typeof(float) && (value is double doubleVal || value is int intVal))
            {
                return (T)(object)Convert.ToSingle(value);
            }
            
            // Try a general conversion for other types
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot convert value of type {value.GetType().Name} to {typeof(T).Name} for statistic '{statisticName}'", ex);
            }
        }

        // Writer class for fluent API
        public static StatisticsWriter Write => new StatisticsWriter();

        // StatisticsWriter class for updating statistics values
        public class StatisticsWriter
        {
            public StatisticsWriter LongestStreak(int value)
            {
                var stats = _cachedStatistics ?? Instance.LoadStatistics();
                stats.LongestStreak = value;
                Instance.SaveStatistics(stats);
                return this;
            }

            public StatisticsWriter WeeklyStudyTimeSeconds(int value)
            {
                var stats = _cachedStatistics ?? Instance.LoadStatistics();
                stats.WeeklyStudyTimeSeconds = value;
                Instance.SaveStatistics(stats);
                return this;
            }

            public StatisticsWriter Retention(double value)
            {
                var stats = _cachedStatistics ?? Instance.LoadStatistics();
                stats.Retention = value;
                Instance.SaveStatistics(stats);
                return this;
            }

            public StatisticsWriter StudyGoalMinutes(int value)
            {
                var stats = _cachedStatistics ?? Instance.LoadStatistics();
                stats.StudyGoalMinutes = value;
                Instance.SaveStatistics(stats);
                return this;
            }

            public StatisticsWriter CurrentStudyGoalMinutes(int value)
            {
                var stats = _cachedStatistics ?? Instance.LoadStatistics();
                stats.CurrentStudyGoalMinutes = value;
                Instance.SaveStatistics(stats);
                return this;
            }

            // Increment methods for easy updating
            public StatisticsWriter IncrementWeeklyStudyTimeSeconds(int seconds)
            {
                var stats = _cachedStatistics ?? Instance.LoadStatistics();
                stats.WeeklyStudyTimeSeconds += seconds;
                Instance.SaveStatistics(stats);
                return this;
            }
            
            public StatisticsWriter IncrementLongestStreak()
            {
                var stats = _cachedStatistics ?? Instance.LoadStatistics();
                stats.LongestStreak += 1;
                Instance.SaveStatistics(stats);
                return this;
            }
        }

        private readonly string _filePath;
        private readonly bool _useEncryption = false; // Temporarily disabled
        private readonly byte[] _encryptionKey;
        private readonly byte[] _iv;
        
        // Private constructor for singleton pattern
        private StatisticsService()
        {
            LogService.Log.Info("Initializing StatisticsService");
            
            // Primary location - Application directory
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string primaryDataDirectory = Path.Combine(appDirectory, "Data");
            string primaryFilePath = Path.Combine(primaryDataDirectory, "statistics.json");
            
            // Secondary location - AppData
            string secondaryDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MnemoProject",
                "Data"
            );
            string secondaryFilePath = Path.Combine(secondaryDataDirectory, "statistics.json");
            
            // Check if file exists in primary location
            bool fileExists = File.Exists(primaryFilePath);
            
            if (fileExists)
            {
                _filePath = primaryFilePath;
                LogService.Log.Info($"Using statistics file from application directory: {_filePath}");
            }
            else
            {
                // Check if file exists in secondary location
                if (File.Exists(secondaryFilePath))
                {
                    _filePath = secondaryFilePath;
                    LogService.Log.Info($"Using statistics file from AppData directory: {_filePath}");
                    fileExists = true;
                }
                else
                {
                    // No file found, create in primary location
                    if (!Directory.Exists(primaryDataDirectory))
                    {
                        LogService.Log.Info($"Creating data directory at {primaryDataDirectory}");
                        Directory.CreateDirectory(primaryDataDirectory);
                    }
                    
                    _filePath = primaryFilePath;
                    LogService.Log.Info($"Will create statistics file in application directory: {_filePath}");
                }
            }
            
            // Generate encryption keys (not used for now but keeping the code)
            string encryptionPassword = "DefaultPassword"; // Should be changed in production
            using (var deriveBytes = new Rfc2898DeriveBytes(
                password: encryptionPassword,
                salt: Encoding.UTF8.GetBytes("YourSaltHere"),
                iterations: 100000,
                hashAlgorithm: HashAlgorithmName.SHA256))
            {
                _encryptionKey = deriveBytes.GetBytes(32); // 256 bits
                _iv = deriveBytes.GetBytes(16); // 128 bits
            }
            
            // Create default statistics file if it doesn't exist
            if (!fileExists)
            {
                LogService.Log.Info("Creating default statistics file");
                SaveStatistics(CreateDefaultStatistics());
            }
        }
        
        private UserStatistics CreateDefaultStatistics()
        {
            LogService.Log.Debug("Creating default statistics");
            return new UserStatistics
            {
                WeeklyStudyTimeSeconds = 0,
                LongestStreak = 0,
                Retention = 0,
                StudyGoalMinutes = 15,
                CurrentStudyGoalMinutes = 0
            };
        }
        
        public void SaveStatistics(UserStatistics statistics)
        {
            LogService.Log.Debug("Saving statistics");
            // Update cache
            _cachedStatistics = statistics;
            
            try
            {
                // Serialize to JSON
                string json = JsonSerializer.Serialize(statistics, new JsonSerializerOptions { WriteIndented = true });
                
                if (_useEncryption)
                {
                    // Encrypt and save
                    File.WriteAllBytes(_filePath, EncryptData(json));
                }
                else
                {
                    // Save without encryption
                    File.WriteAllText(_filePath, json);
                }
                LogService.Log.Info("Statistics saved successfully");
                
                // Notify subscribers that statistics have been updated
                StatisticsUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogService.Log.Error($"Error saving statistics: {ex.Message}");
                throw;
            }
        }

        public UserStatistics LoadStatistics()
        {
            LogService.Log.Debug("Loading statistics");
            // Return cached statistics if available
            if (_cachedStatistics != null)
            {
                return _cachedStatistics;
            }

            // Primary path from constructor might not exist anymore, check if we need to find the file
            string fileToLoad = _filePath;
            if (!File.Exists(fileToLoad))
            {
                LogService.Log.Info("Statistics file not found at expected location");
                
                // Try to find the file in alternative locations
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string primaryPath = Path.Combine(appDirectory, "Data", "statistics.json");
                
                string secondaryPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MnemoProject",
                    "Data",
                    "statistics.json"
                );
                
                // Try primary (application directory) location first
                if (File.Exists(primaryPath) && primaryPath != fileToLoad)
                {
                    LogService.Log.Info($"Found statistics file in application directory: {primaryPath}");
                    fileToLoad = primaryPath;
                }
                // Try secondary (AppData) location as fallback
                else if (File.Exists(secondaryPath) && secondaryPath != fileToLoad)
                {
                    LogService.Log.Info($"Found statistics file in AppData directory: {secondaryPath}");
                    fileToLoad = secondaryPath;
                }
                else 
                {
                    // No file found at all, create default
                    LogService.Log.Info("Statistics file not found in any location, creating default");
                    var defaultStats = CreateDefaultStatistics();
                    SaveStatistics(defaultStats);
                    return defaultStats;
                }
            }

            try
            {
                // Load from the determined file path
                UserStatistics? loadedStats;

                if (_useEncryption)
                {
                    // Read and decrypt
                    byte[] encryptedData = File.ReadAllBytes(fileToLoad);
                    string json = DecryptData(encryptedData);

                    // Deserialize
                    loadedStats = JsonSerializer.Deserialize<UserStatistics>(json);
                }
                else
                {
                    // Read without decryption
                    string json = File.ReadAllText(fileToLoad);
                    loadedStats = JsonSerializer.Deserialize<UserStatistics>(json);
                }

                // Ensure we have valid statistics, use default if deserialization failed
                UserStatistics validStats = loadedStats ?? CreateDefaultStatistics();

                // Cache the loaded statistics
                _cachedStatistics = validStats;
                LogService.Log.Info($"Statistics loaded successfully from {fileToLoad}");
                return validStats;
            }
            catch (Exception ex)
            {
                LogService.Log.Error($"Error loading statistics: {ex.Message}");
                var defaultStats = CreateDefaultStatistics();
                SaveStatistics(defaultStats);
                return defaultStats;
            }
        }


        private byte[] EncryptData(string plainText)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _encryptionKey;
                    aes.IV = _iv;
                    
                    using (MemoryStream output = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            using (StreamWriter writer = new StreamWriter(cryptoStream))
                            {
                                writer.Write(plainText);
                            }
                        }
                        
                        return output.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log.Error($"Error encrypting data: {ex.Message}");
                throw;
            }
        }
        
        private string DecryptData(byte[] encryptedData)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _encryptionKey;
                    aes.IV = _iv;
                    
                    using (MemoryStream input = new MemoryStream(encryptedData))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            using (StreamReader reader = new StreamReader(cryptoStream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Log.Error($"Error decrypting data: {ex.Message}");
                throw;
            }
        }

        // Method to record a study session
        public static void RecordStudySession(int durationSeconds)
        {
            LogService.Log.Debug($"Recording study session of {durationSeconds} seconds");
            var stats = _cachedStatistics ?? Instance.LoadStatistics();
            
            // Check if we need to reset weekly stats
            stats.CheckWeeklyReset();
            
            // Update study time
            stats.WeeklyStudyTimeSeconds += durationSeconds;
            
            // Update streak
            stats.UpdateStreak(DateTime.Now);
            
            // Update daily goal progress (convert seconds to minutes)
            stats.CurrentStudyGoalMinutes += durationSeconds / 60;
            
            // Save changes
            Instance.SaveStatistics(stats);
            LogService.Log.Info($"Study session recorded: {durationSeconds} seconds");
        }
        
        // Method to record a card review result (correct/incorrect)
        public static void RecordCardReview(bool isCorrect)
        {
            LogService.Log.Debug($"Recording card review: {(isCorrect ? "correct" : "incorrect")}");
            var stats = _cachedStatistics ?? Instance.LoadStatistics();
            
            // Simple exponential moving average for retention calculation
            // Weight new results at 5% to smooth out the retention rate
            const double alpha = 0.05;
            double newDataPoint = isCorrect ? 100.0 : 0.0;
            
            stats.Retention = (alpha * newDataPoint) + ((1 - alpha) * stats.Retention);
            
            // Save changes
            Instance.SaveStatistics(stats);
            LogService.Log.Info($"Card review recorded: {(isCorrect ? "correct" : "incorrect")}");
        }

        // Additional static methods for managing statistics

        // Clear the cached statistics to force loading from file next time
        public static void ClearCache()
        {
            LogService.Log.Info("Clearing statistics cache");
            _cachedStatistics = null;
        }

        // Force reload from file
        public static UserStatistics ForceReload()
        {
            ClearCache();
            return Instance.LoadStatistics();
        }
    }
}