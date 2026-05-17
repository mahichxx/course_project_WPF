using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Repositories
{
    public class WorkoutPlanRepository : BaseRepository<WorkoutPlan>
    {
        public WorkoutPlanRepository(AppDbContext context) : base(context)
        {
        }

        public override List<WorkoutPlan> GetAll()
        {
            return _dbSet.Include(wp => wp.Client)
                         .Include(wp => wp.Coach)
                         .ToList();
        }

        public override WorkoutPlan GetById(int id)
        {
            return _dbSet.Include(wp => wp.Client)
                         .Include(wp => wp.Coach)
                         .FirstOrDefault(wp => wp.Id == id);
        }

        public List<WorkoutPlan> GetByUser(int userId)
        {
            return _dbSet.Where(wp => wp.ClientId == userId)
                         .ToList();
        }

        public List<WorkoutPlan> GetByTrainer(int trainerId)
        {
            return _dbSet.Where(wp => wp.CoachId == trainerId)
                         .ToList();
        }
    }
} 