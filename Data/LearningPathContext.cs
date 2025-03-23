using Microsoft.EntityFrameworkCore;
using MnemoProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MnemoProject.Data
{
    public class LearningPathContext : DbContext
{
    public DbSet<LearningPath> LearningPaths { get; set; }
    public DbSet<Unit> Units { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=./Data/learningpaths.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LearningPath>()
            .HasMany(lp => lp.Units)
            .WithOne(u => u.LearningPath)
            .HasForeignKey(u => u.LearningPathId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

}
