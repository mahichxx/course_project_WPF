using System.Collections.Generic;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Services.Interfaces
{
    public interface INutritionPlanService
    {
        NutritionPlan GetById(int id);
        List<NutritionPlan> GetAll();
        List<NutritionPlan> GetNutritionPlansByClientId(int clientId);
        List<NutritionPlan> GetNutritionPlansByCoachId(int coachId);
        NutritionPlan Create(NutritionPlan nutritionPlan);
        NutritionPlan Update(NutritionPlan nutritionPlan);
        void Delete(int id);
    }
} 