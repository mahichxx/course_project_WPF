using System.Threading.Tasks;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Services.Interfaces
{
    /// <summary>
    /// Интерфейс для сервиса экспорта в PDF
    /// </summary>
    public interface IPdfExportService
    {
        /// <summary>
        /// Экспортирует план тренировок в PDF файл
        /// </summary>
        /// <param name="workoutPlan">План тренировок для экспорта</param>
        /// <param name="filePath">Путь к файлу для сохранения</param>
        /// <returns>True, если экспорт выполнен успешно</returns>
        Task<bool> ExportWorkoutPlanToPdf(WorkoutPlan workoutPlan, string filePath);
        
        /// <summary>
        /// Экспортирует план питания в PDF файл
        /// </summary>
        /// <param name="nutritionPlan">План питания для экспорта</param>
        /// <param name="filePath">Путь к файлу для сохранения</param>
        /// <returns>True, если экспорт выполнен успешно</returns>
        Task<bool> ExportNutritionPlanToPdf(NutritionPlan nutritionPlan, string filePath);
        
        /// <summary>
        /// Экспортирует отчет о прогрессе клиента в PDF файл
        /// </summary>
        /// <param name="clientId">ID клиента</param>
        /// <param name="startDate">Дата начала периода</param>
        /// <param name="endDate">Дата окончания периода</param>
        /// <param name="filePath">Путь к файлу для сохранения</param>
        /// <returns>True, если экспорт выполнен успешно</returns>
        Task<bool> ExportClientProgressReport(int clientId, System.DateTime startDate, System.DateTime endDate, string filePath);
    }
} 