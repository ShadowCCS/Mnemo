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
        private readonly MnemoContext _context;
        private static bool _isInitialized = false;
        private static readonly object _lockObject = new object();
        private static Task _initializationTask = null;

        public DatabaseService()
        {
            _context = new MnemoContext();
        }

        /// <summary>
        /// Initialize the database asynchronously
        /// </summary>
        public Task InitializeAsync()
        {
            if (!_isInitialized)
            {
                lock (_lockObject)
                {
                    if (_initializationTask == null)
                    {
                        _initializationTask = Task.Run(async () =>
                        {
                            try
                            {
                                await _context.Database.EnsureCreatedAsync().ConfigureAwait(false);
                                _isInitialized = true;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
                                throw;
                            }
                        });
                    }
                }
                return _initializationTask;
            }
            return Task.CompletedTask;
        }

        public async Task AddLearningPath(LearningPath learningPath)
        {
            await InitializeAsync().ConfigureAwait(false);

            if (learningPath.Id == Guid.Empty)
            {
                learningPath.Id = Guid.NewGuid();
            }

            foreach (var unit in learningPath.Units)
            {
                unit.LearningPathId = learningPath.Id;
                if (unit.Id == Guid.Empty)
                {
                    unit.Id = Guid.NewGuid();
                }
            }

            _context.LearningPaths.Add(learningPath);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task AddUnit(Unit unit)
        {
            if (unit.Id == Guid.Empty)
            {
                unit.Id = Guid.NewGuid();
            }

            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LearningPath>> GetAllLearningPaths()
        {
            await InitializeAsync().ConfigureAwait(false);
            return await _context.LearningPaths.Include(lp => lp.Units).ToListAsync().ConfigureAwait(false);
        }

        public async Task<LearningPath?> GetLearningPathById(Guid id)
        {
            return await _context.LearningPaths.Include(lp => lp.Units).FirstOrDefaultAsync(lp => lp.Id == id);
        }

        public async Task DeleteLearningPath(Guid id)
        {
            var learningPath = await _context.LearningPaths.Include(lp => lp.Units).FirstOrDefaultAsync(lp => lp.Id == id);
            if (learningPath != null)
            {
                _context.LearningPaths.Remove(learningPath);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAllLearningPaths()
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM LearningPaths");
        }

        public async Task UpdateLearningPath(LearningPath learningPath)
        {
            _context.LearningPaths.Update(learningPath);
            await _context.SaveChangesAsync();
        }

        public async Task<Unit?> GetUnitById(Guid unitId)
        {
            return await _context.Units.FirstOrDefaultAsync(u => u.Id == unitId);
        }

        public async Task<List<Unit>> GetUnitsByLearningPathId(Guid learningPathId)
        {
            return await _context.Units.Where(u => u.LearningPathId == learningPathId).OrderBy(u => u.UnitNumber).ToListAsync();
        }

        public async Task<Unit?> GetUnitByNumber(Guid learningPathId, int unitNumber)
        {
            return await _context.Units.FirstOrDefaultAsync(u => u.LearningPathId == learningPathId && u.UnitNumber == unitNumber);
        }

        public async Task UpdateUnit(Unit unit)
        {
            _context.Units.Update(unit);
            await _context.SaveChangesAsync();
        }
    }
}
