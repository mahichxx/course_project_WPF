using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using WPF_FitnessClub.Data.Repositories;
using WPF_FitnessClub.Data.Services.Interfaces;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Services
{
    /// <summary>
    /// Сервис для работы с планами питания
    /// </summary>
    public class NutritionPlanService : INutritionPlanService
    {
        private readonly NutritionPlanRepository _repository;

        public NutritionPlanService()
        {
            _repository = new NutritionPlanRepository(new AppDbContext());
        }

        /// <summary>
        /// Получить все планы питания
        /// </summary>
        public List<NutritionPlan> GetAll()
        {
            return _repository.GetAll();
        }

        /// <summary>
        /// Получить план питания по ID
        /// </summary>
        public NutritionPlan GetById(int id)
        {
            return _repository.GetById(id);
        }

        public NutritionPlan Create(NutritionPlan nutritionPlan)
        {
            _repository.Create(nutritionPlan);
            _repository.Save();
            return nutritionPlan;
        }

        public NutritionPlan Update(NutritionPlan nutritionPlan)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    // Находим реальный объект в базе по ID
                    var planToUpdate = context.NutritionPlans.Find(nutritionPlan.Id);

                    if (planToUpdate == null)
                    {
                        throw new Exception($"План питания с ID {nutritionPlan.Id} не найден в базе данных.");
                    }

                    // ПЕРЕНОСИМ ВСЕ ИЗМЕНЕННЫЕ ДАННЫЕ (Этого у вас не было!)
                    planToUpdate.Title = nutritionPlan.Title;
                    planToUpdate.Description = nutritionPlan.Description;
                    planToUpdate.StartDate = nutritionPlan.StartDate;
                    planToUpdate.EndDate = nutritionPlan.EndDate;
                    planToUpdate.IsCompleted = nutritionPlan.IsCompleted;
                    planToUpdate.UpdatedDate = DateTime.Now;

                    // Сохраняем физически в SQL Server
                    context.SaveChanges();

                    System.Diagnostics.Debug.WriteLine($"План питания ID {planToUpdate.Id} успешно обновлен.");
                    return planToUpdate;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в NutritionPlanService.Update: {ex.Message}");
                throw;
            }
        }

        public void Delete(int id)
        {
            try
            {
                // Используем новый контекст для этой операции
                using (var context = new AppDbContext())
                {
                    // Находим план по ID с отслеживанием
                    var planToDelete = context.NutritionPlans.Find(id);
                    
                    if (planToDelete != null)
                    {
                        // Удаляем сам план
                        context.NutritionPlans.Remove(planToDelete);
                        
                        // Сохраняем изменения
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                System.Diagnostics.Debug.WriteLine($"Ошибка при удалении плана питания: {ex.Message}");
                throw; // Передаем исключение дальше для обработки в ViewModel
            }
        }

        public List<NutritionPlan> GetNutritionPlansByClientId(int clientId)
        {
            return _repository.GetByUser(clientId);
        }

        public List<NutritionPlan> GetNutritionPlansByCoachId(int coachId)
        {
            return _repository.GetByTrainer(coachId);
        }
    }
} 