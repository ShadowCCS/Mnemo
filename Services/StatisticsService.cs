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
            return (T)Get(statisticName);
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
            
            // Create data directory if it doesn't exist
            string dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MnemoProject",
                "Data"
            );
            
            if (!Directory.Exists(dataDirectory))
            {
                LogService.Log.Info($"Creating data directory at {dataDirectory}");
                Directory.CreateDirectory(dataDirectory);
            }
            
            _filePath = Path.Combine(dataDirectory, "statistics.json");
            
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
            if (!File.Exists(_filePath))
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

            if (!File.Exists(_filePath))
            {
                LogService.Log.Info("Statistics file not found, creating default");
                var defaultStats = CreateDefaultStatistics();
                SaveStatistics(defaultStats);
                return defaultStats;
            }

            try
            {
                UserStatistics? loadedStats;

                if (_useEncryption)
                {
                    // Read and decrypt
                    byte[] encryptedData = File.ReadAllBytes(_filePath);
                    string json = DecryptData(encryptedData);

                    // Deserialize
                    loadedStats = JsonSerializer.Deserialize<UserStatistics>(json);
                }
                else
                {
                    // Read without decryption
                    string json = File.ReadAllText(_filePath);
                    loadedStats = JsonSerializer.Deserialize<UserStatistics>(json);
                }

                // Ensure we have valid statistics, use default if deserialization failed
                UserStatistics validStats = loadedStats ?? CreateDefaultStatistics();

                // Cache the loaded statistics
                _cachedStatistics = validStats;
                LogService.Log.Info("Statistics loaded successfully");
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
    }
}