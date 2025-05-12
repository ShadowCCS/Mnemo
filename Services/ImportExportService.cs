using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using MnemoProject.Data;
using MnemoProject.Models;

namespace MnemoProject.Services
{
    public enum ExportType
    {
        FlashcardDeck,
        LearningPath
    }

    public class MnemoExportData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("meta")]
        public MnemoMetaData Meta { get; set; }

        [JsonPropertyName("content")]
        public JsonElement Content { get; set; }
    }

    public class MnemoMetaData
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }
    }

    public class FlashcardExportContent
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("cards")]
        public List<CardExport> Cards { get; set; }
    }

    public class CardExport
    {
        [JsonPropertyName("front")]
        public string Front { get; set; }

        [JsonPropertyName("back")]
        public string Back { get; set; }
    }

    public class LearningPathExportContent
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("units")]
        public List<UnitExport> Units { get; set; }
    }

    public class UnitExport
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("unit_number")]
        public int UnitNumber { get; set; }

        [JsonPropertyName("theory_content")]
        public string TheoryContent { get; set; }

        [JsonPropertyName("unit_content")]
        public string UnitContent { get; set; }
    }

    public class ImportExportService
    {
        private readonly MnemoContext _dbContext;
        
        public ImportExportService()
        {
            _dbContext = new MnemoContext();
        }
        
        public async Task<string> ExportFlashcardDeckAsync(Guid deckId, string outputPath)
        {
            try
            {
                var flashcard = await _dbContext.Flashcards
                    .Include(f => f.Decks)
                    .FirstOrDefaultAsync(f => f.Id == deckId);
                
                if (flashcard == null)
                {
                    NotificationService.Error("Failed to export: Flashcard deck not found");
                    return null;
                }
                
                var content = new FlashcardExportContent
                {
                    Title = flashcard.Title,
                    Cards = flashcard.Decks.Select(d => new CardExport
                    {
                        Front = d.Front,
                        Back = d.Back
                    }).ToList()
                };
                
                return await ExportDataAsync(ExportType.FlashcardDeck, flashcard.Title, content, outputPath);
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error exporting flashcard deck: {ex.Message}");
                return null;
            }
        }
        
        public async Task<string> ExportLearningPathAsync(Guid learningPathId, string outputPath)
        {
            try
            {
                var learningPath = await _dbContext.LearningPaths
                    .Include(lp => lp.Units)
                    .FirstOrDefaultAsync(lp => lp.Id == learningPathId);
                
                if (learningPath == null)
                {
                    NotificationService.Error("Failed to export: Learning path not found");
                    return null;
                }
                
                var content = new LearningPathExportContent
                {
                    Title = learningPath.Title,
                    Units = learningPath.Units.Select(u => new UnitExport
                    {
                        Title = u.Title,
                        UnitNumber = u.UnitNumber,
                        TheoryContent = u.TheoryContent,
                        UnitContent = u.UnitContent
                    }).ToList()
                };
                
                return await ExportDataAsync(ExportType.LearningPath, learningPath.Title, content, outputPath);
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error exporting learning path: {ex.Message}");
                return null;
            }
        }
        
        private async Task<string> ExportDataAsync<T>(ExportType exportType, string title, T content, string outputPath)
        {
            try
            {
                string type = exportType.ToString().ToLower();
                
                // Convert the strongly-typed content to JsonElement for the final structure
                var contentElement = JsonSerializer.SerializeToElement(content);
                
                var exportData = new
                {
                    type = type,
                    version = VersionInfo.MnemoFileVersion,
                    meta = new
                    {
                        title = title,
                        created_at = DateTime.UtcNow.ToString("o") // ISO 8601 format
                    },
                    content = contentElement
                };
                
                string filename = $"{title.Replace(" ", "_")}.mnemo";
                string fullPath = Path.Combine(outputPath, filename);
                
                // Make sure directory exists
                Directory.CreateDirectory(outputPath);
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                
                string jsonData = JsonSerializer.Serialize(exportData, options);
                await File.WriteAllTextAsync(fullPath, jsonData);
                
                NotificationService.Success($"Successfully exported to {fullPath}");
                return fullPath;
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error during export: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ImportFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    NotificationService.Error($"File does not exist: {filePath}");
                    return false;
                }

                if (!filePath.EndsWith(".mnemo", StringComparison.OrdinalIgnoreCase))
                {
                    NotificationService.Error("Only .mnemo files can be imported");
                    return false;
                }

                string jsonContent = await File.ReadAllTextAsync(filePath);
                var importData = JsonSerializer.Deserialize<MnemoExportData>(jsonContent);

                if (importData == null)
                {
                    NotificationService.Error("Invalid import file format");
                    return false;
                }

                switch (importData.Type.ToLower())
                {
                    case "flashcarddeck":
                        return await ImportFlashcardDeckAsync(importData);
                    case "learningpath":
                        return await ImportLearningPathAsync(importData);
                    default:
                        NotificationService.Error($"Unknown import type: {importData.Type}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error importing file: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ImportFlashcardDeckAsync(MnemoExportData importData)
        {
            try
            {
                var flashcardContent = importData.Content.Deserialize<FlashcardExportContent>();
                
                if (flashcardContent == null || flashcardContent.Cards == null || !flashcardContent.Cards.Any())
                {
                    NotificationService.Error("Invalid flashcard data in import file");
                    return false;
                }

                // Create new flashcard deck
                var flashcard = new Flashcard
                {
                    Id = Guid.NewGuid(),
                    Title = flashcardContent.Title,
                    CardCount = flashcardContent.Cards.Count,
                    Decks = new List<Deck>()
                };

                // Add cards to deck
                foreach (var card in flashcardContent.Cards)
                {
                    flashcard.Decks.Add(new Deck
                    {
                        Id = Guid.NewGuid(),
                        Front = card.Front,
                        Back = card.Back,
                        FlashcardId = flashcard.Id
                    });
                }

                // Save to database
                await _dbContext.Flashcards.AddAsync(flashcard);
                await _dbContext.SaveChangesAsync();

                NotificationService.Success($"Successfully imported flashcard deck: {flashcardContent.Title}");
                return true;
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error importing flashcard deck: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ImportLearningPathAsync(MnemoExportData importData)
        {
            try
            {
                var pathContent = importData.Content.Deserialize<LearningPathExportContent>();
                
                if (pathContent == null || pathContent.Units == null || !pathContent.Units.Any())
                {
                    NotificationService.Error("Invalid learning path data in import file");
                    return false;
                }

                // Create new learning path
                var learningPath = new LearningPath
                {
                    Id = Guid.NewGuid(),
                    Title = pathContent.Title,
                    Units = new List<Unit>()
                };

                // Add units to learning path
                foreach (var unit in pathContent.Units)
                {
                    learningPath.Units.Add(new Unit
                    {
                        Id = Guid.NewGuid(),
                        Title = unit.Title,
                        UnitNumber = unit.UnitNumber,
                        TheoryContent = unit.TheoryContent,
                        UnitContent = unit.UnitContent,
                        LearningPathId = learningPath.Id
                    });
                }

                // Save to database
                await _dbContext.LearningPaths.AddAsync(learningPath);
                await _dbContext.SaveChangesAsync();

                NotificationService.Success($"Successfully imported learning path: {pathContent.Title}");
                return true;
            }
            catch (Exception ex)
            {
                NotificationService.Error($"Error importing learning path: {ex.Message}");
                return false;
            }
        }
    }
} 