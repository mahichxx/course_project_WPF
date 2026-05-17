using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using WPF_FitnessClub.Data.Repositories;
using WPF_FitnessClub.Data.Services.Interfaces;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Services
{
    public class WorkoutPlanService : IWorkoutPlanService
    {
        private readonly WorkoutPlanRepository _repository;

        public WorkoutPlanService()
        {
            _repository = new WorkoutPlanRepository(new AppDbContext());
        }

        public WorkoutPlan GetById(int id)
        {
            return _repository.GetById(id);
        }

        public List<WorkoutPlan> GetAll()
        {
            return _repository.GetAll();
        }

        public List<WorkoutPlan> GetWorkoutPlansByClientId(int clientId)
        {
            return _repository.GetByUser(clientId);
        }

        public List<WorkoutPlan> GetWorkoutPlansByCoachId(int coachId)
        {
            return _repository.GetByTrainer(coachId);
        }

        public WorkoutPlan Create(WorkoutPlan workoutPlan)
        {
            _repository.Create(workoutPlan);
            _repository.Save();
            return workoutPlan;
        }

        public WorkoutPlan Update(WorkoutPlan workoutPlan)
        {
            try
            {
                // Используем новый контекст для операции обновления
                using (var context = new AppDbContext())
                {
                    // Находим план в базе данных
                    var planToUpdate = context.WorkoutPlans.Find(workoutPlan.Id);
                    
                    if (planToUpdate == null)
                    {
                        throw new Exception($"План тренировок с ID {workoutPlan.Id} не найден");
                    }
                    
                    // Обновляем только необходимые поля
                    planToUpdate.IsCompleted = workoutPlan.IsCompleted;
                    planToUpdate.UpdatedDate = DateTime.Now;
                    
                    // Сохраняем изменения непосредственно через контекст
                    context.SaveChanges();
                    
                    System.Diagnostics.Debug.WriteLine($"План тренировок с ID {workoutPlan.Id} успешно обновлен. IsCompleted = {planToUpdate.IsCompleted}");
                    
                    // Возвращаем обновленный план
                    return planToUpdate;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении плана тренировок: {ex.Message}");
                throw; // Передаем исключение дальше для обработки в ViewModel
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
                    var planToDelete = context.WorkoutPlans.Find(id);
                    
                    if (planToDelete != null)
                    {
                        // Удаляем сам план
                        context.WorkoutPlans.Remove(planToDelete);
                        
                        // Сохраняем изменения
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                System.Diagnostics.Debug.WriteLine($"Ошибка при удалении плана тренировок: {ex.Message}");
                throw; // Передаем исключение дальше для обработки в ViewModel
            }
        }
    }
} 