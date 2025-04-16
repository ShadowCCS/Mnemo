using Microsoft.EntityFrameworkCore;
using MnemoProject.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MnemoProject.Data
{
    public class MnemoContext : DbContext
    {
        private static string _databasePath;
        public static string DatabasePath 
        { 
            get => _databasePath ?? GetDefaultDbPath();
            set => _databasePath = value;
        }

        private static string GetDefaultDbPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mnemo.db");
        }

        public DbSet<LearningPath> LearningPaths { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Flashcard> Flashcards { get; set; }
        public DbSet<Deck> Decks { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }

        public MnemoContext()
        {
            // Ensure the directory exists
            string dbDirectory = Path.GetDirectoryName(DatabasePath);
            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // LearningPath and Unit configuration
            modelBuilder.Entity<LearningPath>()
                .HasMany(lp => lp.Units)
                .WithOne(u => u.LearningPath)
                .HasForeignKey(u => u.LearningPathId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Flashcard and Deck configuration
            modelBuilder.Entity<Flashcard>()
                .HasMany(f => f.Decks)
                .WithOne(d => d.Flashcard)
                .HasForeignKey(d => d.FlashcardId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quiz and Question configuration
            modelBuilder.Entity<Quiz>()
                .HasMany(q => q.Questions)
                .WithOne(q => q.Quiz)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Ensure non-nullable string properties have default values
            modelBuilder.Entity<LearningPath>()
                .Property(lp => lp.Title)
                .HasDefaultValue("");
                
            modelBuilder.Entity<Unit>()
                .Property(u => u.Title)
                .HasDefaultValue("");
                
            modelBuilder.Entity<Unit>()
                .Property(u => u.TheoryContent)
                .HasDefaultValue("");
                
            modelBuilder.Entity<Unit>()
                .Property(u => u.UnitContent)
                .HasDefaultValue("");

            modelBuilder.Entity<Flashcard>()
                .Property(f => f.Title)
                .HasDefaultValue("");

            modelBuilder.Entity<Deck>()
                .Property(d => d.Front)
                .HasDefaultValue("");

            modelBuilder.Entity<Deck>()
                .Property(d => d.Back)
                .HasDefaultValue("");

            modelBuilder.Entity<Quiz>()
                .Property(q => q.Title)
                .HasDefaultValue("");

            modelBuilder.Entity<Question>()
                .Property(q => q.QuestionText)
                .HasDefaultValue("");

            modelBuilder.Entity<Question>()
                .Property(q => q.CorrectAnswer)
                .HasDefaultValue("");
        }
    }
}
