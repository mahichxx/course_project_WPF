using System.Collections.Generic;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Services.Interfaces
{
    public interface IWorkoutPlanService
    {
        WorkoutPlan GetById(int id);
        List<WorkoutPlan> GetAll();
        List<WorkoutPlan> GetWorkoutPlansByClientId(int clientId);
        List<WorkoutPlan> GetWorkoutPlansByCoachId(int coachId);
        WorkoutPlan Create(WorkoutPlan workoutPlan);
        WorkoutPlan Update(WorkoutPlan workoutPlan);
        void Delete(int id);
    }
} 