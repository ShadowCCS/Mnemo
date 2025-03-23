using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MnemoProject.Models;

namespace MnemoProject.Data
{
    public class DatabaseService
    {
        public async Task AddLearningPath(LearningPath learningPath)
        {
            using var db = new LearningPathContext();
            db.LearningPaths.Add(learningPath);
            await db.SaveChangesAsync();
        }


        public async Task<List<LearningPath>> GetAllLearningPaths()
        {
            using var db = new LearningPathContext();
            return await db.LearningPaths.Include(lp => lp.Units).ToListAsync();
        }

        public async Task<LearningPath?> GetLearningPathById(Guid id)
        {
            using var db = new LearningPathContext();
            return await db.LearningPaths.Include(lp => lp.Units).FirstOrDefaultAsync(lp => lp.Id == id);
        }

        public async Task DeleteLearningPath(Guid id)
        {
            using var db = new LearningPathContext();
            var learningPath = await db.LearningPaths.FindAsync(id);
            if (learningPath != null)
            {
                db.LearningPaths.Remove(learningPath);
                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteAllLearningPaths()
        {
            using var db = new LearningPathContext();
            var learningPaths = await db.LearningPaths.ToListAsync();

            foreach (var learningPath in learningPaths)
            {
                await DeleteLearningPath(learningPath.Id);
            }
        }


        public async Task UpdateLearningPath(LearningPath learningPath)
        {
            using var db = new LearningPathContext();
            db.LearningPaths.Update(learningPath);
            await db.SaveChangesAsync();
        }
    }
}
