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

            // Ensure the learning path has a unique ID
            if (learningPath.Id == Guid.Empty)
            {
                learningPath.Id = Guid.NewGuid();
            }

            // Ensure all units have a valid learning path ID
            foreach (var unit in learningPath.Units)
            {
                unit.LearningPathId = learningPath.Id;
                if (unit.Id == Guid.Empty)
                {
                    unit.Id = Guid.NewGuid();
                }
            }

            db.LearningPaths.Add(learningPath);
            await db.SaveChangesAsync();
        }

        public async Task AddUnit(Unit unit)
        {
            using var db = new LearningPathContext();

            // Ensure the unit has a unique ID
            if (unit.Id == Guid.Empty)
            {
                unit.Id = Guid.NewGuid();
            }

            db.Units.Add(unit);
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
            var learningPath = await db.LearningPaths.Include(lp => lp.Units).FirstOrDefaultAsync(lp => lp.Id == id);
            if (learningPath != null)
            {
                db.LearningPaths.Remove(learningPath);
                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteAllLearningPaths()
        {
            using var db = new LearningPathContext();
            await db.Database.ExecuteSqlRawAsync("DELETE FROM LearningPaths");
        }

        public async Task UpdateLearningPath(LearningPath learningPath)
        {
            using var db = new LearningPathContext();
            db.LearningPaths.Update(learningPath);
            await db.SaveChangesAsync();
        }

        public async Task<Unit?> GetUnitById(Guid unitId)
        {
            using var db = new LearningPathContext();
            return await db.Units.FirstOrDefaultAsync(u => u.Id == unitId);
        }

        public async Task<List<Unit>> GetUnitsByLearningPathId(Guid learningPathId)
        {
            using var db = new LearningPathContext();
            return await db.Units.Where(u => u.LearningPathId == learningPathId).OrderBy(u => u.UnitNumber).ToListAsync();
        }

        public async Task<Unit?> GetUnitByNumber(Guid learningPathId, int unitNumber)
        {
            using var db = new LearningPathContext();
            return await db.Units.FirstOrDefaultAsync(u => u.LearningPathId == learningPathId && u.UnitNumber == unitNumber);
        }

        public async Task UpdateUnit(Unit unit)
        {
            using var db = new LearningPathContext();
            db.Units.Update(unit);
            await db.SaveChangesAsync();
        }
    }
}
