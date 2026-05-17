using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Repositories
{
    public class NutritionPlanRepository : BaseRepository<NutritionPlan>
    {
        public NutritionPlanRepository(AppDbContext context) : base(context)
        {
        }

        public override List<NutritionPlan> GetAll()
        {
            try
            {
                var context = _context as AppDbContext;
                var nutritionPlans = new List<NutritionPlan>();
                
                if (context == null)
                {
                    System.Diagnostics.Debug.WriteLine("GetAll: Ошибка приведения контекста к AppDbContext");
                    return new List<NutritionPlan>();
                }
                
                var plansData = _dbSet.AsNoTracking().ToList();
                
                System.Diagnostics.Debug.WriteLine($"GetAll: загружено {plansData.Count} планов питания");
                
                foreach (var plan in plansData)
                {
                    var client = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.ClientId);
                    var coach = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.CoachId);
                    
                    var nutritionPlan = new NutritionPlan
                    {
                        Id = plan.Id,
                        ClientId = plan.ClientId,
                        CoachId = plan.CoachId,
                        Title = plan.Title,
                        Description = plan.Description,
                        CreatedDate = plan.CreatedDate,
                        UpdatedDate = plan.UpdatedDate,
                        StartDate = plan.StartDate,
                        EndDate = plan.EndDate,
                        Client = client,        
                        Coach = coach           
                    };
                    
                    nutritionPlans.Add(nutritionPlan);
                }
                
                return nutritionPlans;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении планов питания: {ex.Message}");
                return new List<NutritionPlan>();
            }
        }

        public override NutritionPlan GetById(int id)
        {
            try
            {
                var context = _context as AppDbContext;
                
                if (context == null)
                {
                    System.Diagnostics.Debug.WriteLine("GetById: Ошибка приведения контекста к AppDbContext");
                    return null;
                }
                
                var plan = _dbSet.AsNoTracking().FirstOrDefault(np => np.Id == id);
                
                if (plan == null)
                {
                    System.Diagnostics.Debug.WriteLine($"GetById: План с ID={id} не найден");
                    return null;
                }
                
                var client = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.ClientId);
                var coach = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.CoachId);
                
                var nutritionPlan = new NutritionPlan
                {
                    Id = plan.Id,
                    ClientId = plan.ClientId,
                    CoachId = plan.CoachId,
                    Title = plan.Title,
                    Description = plan.Description,
                    CreatedDate = plan.CreatedDate,
                    UpdatedDate = plan.UpdatedDate,
                    StartDate = plan.StartDate,
                    EndDate = plan.EndDate,
                    Client = client,        
                    Coach = coach           
                };
                
                return nutritionPlan;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении плана питания по ID: {ex.Message}");
                return null;
            }
        }

        public List<NutritionPlan> GetByUser(int userId)
        {
            try
            {
                var context = _context as AppDbContext;
                var nutritionPlans = new List<NutritionPlan>();
                
                if (context == null)
                {
                    System.Diagnostics.Debug.WriteLine("GetByUser: Ошибка приведения контекста к AppDbContext");
                    return new List<NutritionPlan>();
                }
                
                var plansData = _dbSet.AsNoTracking().Where(np => np.ClientId == userId).ToList();
                
                System.Diagnostics.Debug.WriteLine($"GetByUser: загружено {plansData.Count} планов питания для клиента ID={userId}");
                
                foreach (var plan in plansData)
                {
                    var client = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.ClientId);
                    var coach = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.CoachId);
                    
                    var nutritionPlan = new NutritionPlan
                    {
                        Id = plan.Id,
                        ClientId = plan.ClientId,
                        CoachId = plan.CoachId,
                        Title = plan.Title,
                        Description = plan.Description,
                        CreatedDate = plan.CreatedDate,
                        UpdatedDate = plan.UpdatedDate,
                        StartDate = plan.StartDate,
                        EndDate = plan.EndDate,
                        Client = client,
                        Coach = coach
                    };
                    
                    nutritionPlans.Add(nutritionPlan);
                }
                
                return nutritionPlans;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении планов питания для клиента: {ex.Message}");
                return new List<NutritionPlan>();
            }
        }

        public List<NutritionPlan> GetByTrainer(int trainerId)
        {
            try
            {
                var context = _context as AppDbContext;
                var nutritionPlans = new List<NutritionPlan>();
                
                if (context == null)
                {
                    System.Diagnostics.Debug.WriteLine("GetByTrainer: Ошибка приведения контекста к AppDbContext");
                    return new List<NutritionPlan>();
                }
                
                var plansData = _dbSet.AsNoTracking().Where(np => np.CoachId == trainerId).ToList();
                
                System.Diagnostics.Debug.WriteLine($"GetByTrainer: загружено {plansData.Count} планов питания для тренера ID={trainerId}");
                
                foreach (var plan in plansData)
                {
                    var client = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.ClientId);
                    var coach = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.CoachId);
                    
                    var nutritionPlan = new NutritionPlan
                    {
                        Id = plan.Id,
                        ClientId = plan.ClientId,
                        CoachId = plan.CoachId,
                        Title = plan.Title,
                        Description = plan.Description,
                        CreatedDate = plan.CreatedDate,
                        UpdatedDate = plan.UpdatedDate,
                        StartDate = plan.StartDate,
                        EndDate = plan.EndDate,
                        Client = client,
                        Coach = coach
                    };
                    
                    nutritionPlans.Add(nutritionPlan);
                }
                
                return nutritionPlans;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении планов питания для тренера: {ex.Message}");
                return new List<NutritionPlan>();
            }
        }

        public List<NutritionPlan> GetActivePlans()
        {
            try
            {
                var context = _context as AppDbContext;
                var nutritionPlans = new List<NutritionPlan>();
                
                if (context == null)
                {
                    System.Diagnostics.Debug.WriteLine("GetActivePlans: Ошибка приведения контекста к AppDbContext");
                    return new List<NutritionPlan>();
                }
                
                var currentDate = DateTime.Now;
                
                var plansData = _dbSet.AsNoTracking()
                    .Where(np => np.StartDate <= currentDate && np.EndDate >= currentDate)
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"GetActivePlans: загружено {plansData.Count} активных планов питания");
                
                foreach (var plan in plansData)
                {
                    var client = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.ClientId);
                    var coach = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.CoachId);
                    
                    var nutritionPlan = new NutritionPlan
                    {
                        Id = plan.Id,
                        ClientId = plan.ClientId,
                        CoachId = plan.CoachId,
                        Title = plan.Title,
                        Description = plan.Description,
                        CreatedDate = plan.CreatedDate,
                        UpdatedDate = plan.UpdatedDate,
                        StartDate = plan.StartDate,
                        EndDate = plan.EndDate,
                        Client = client,
                        Coach = coach
                    };
                    
                    nutritionPlans.Add(nutritionPlan);
                }
                
                return nutritionPlans;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении активных планов питания: {ex.Message}");
                return new List<NutritionPlan>();
            }
        }

        public List<NutritionPlan> GetPlansByDateRange(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var context = _context as AppDbContext;
                var nutritionPlans = new List<NutritionPlan>();
                
                if (context == null)
                {
                    System.Diagnostics.Debug.WriteLine("GetPlansByDateRange: Ошибка приведения контекста к AppDbContext");
                    return new List<NutritionPlan>();
                }
                
                var plansData = _dbSet.AsNoTracking().Where(np => 
                    (np.StartDate >= fromDate && np.StartDate <= toDate) || 
                    (np.EndDate >= fromDate && np.EndDate <= toDate) || 
                    (np.StartDate <= fromDate && np.EndDate >= toDate))
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"GetPlansByDateRange: загружено {plansData.Count} планов питания в диапазоне дат");
                
                foreach (var plan in plansData)
                {
                    var client = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.ClientId);
                    var coach = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == plan.CoachId);
                    
                    var nutritionPlan = new NutritionPlan
                    {
                        Id = plan.Id,
                        ClientId = plan.ClientId,
                        CoachId = plan.CoachId,
                        Title = plan.Title,
                        Description = plan.Description,
                        CreatedDate = plan.CreatedDate,
                        UpdatedDate = plan.UpdatedDate,
                        StartDate = plan.StartDate,
                        EndDate = plan.EndDate,
                        Client = client,
                        Coach = coach
                    };
                    
                    nutritionPlans.Add(nutritionPlan);
                }
                
                return nutritionPlans;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении планов питания по диапазону дат: {ex.Message}");
                return new List<NutritionPlan>();
            }
        }
    }
} 