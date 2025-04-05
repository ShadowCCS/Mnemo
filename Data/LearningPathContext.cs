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
    public class LearningPathContext : DbContext
    {
        private static readonly string DefaultDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "learningpaths.db");
        public static string DatabasePath { get; set; } = DefaultDbPath;

        public DbSet<LearningPath> LearningPaths { get; set; }
        public DbSet<Unit> Units { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Ensure the directory exists
            string dbDirectory = Path.GetDirectoryName(DatabasePath);
            if (!Directory.Exists(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

            optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LearningPath>()
                .HasMany(lp => lp.Units)
                .WithOne(u => u.LearningPath)
                .HasForeignKey(u => u.LearningPathId)
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
        }
    }
}
