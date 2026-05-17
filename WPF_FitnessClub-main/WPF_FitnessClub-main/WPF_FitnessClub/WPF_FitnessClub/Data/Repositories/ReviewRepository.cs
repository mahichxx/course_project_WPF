using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using WPF_FitnessClub;

namespace WPF_FitnessClub.Data.Repositories
{
    public class ReviewRepository : BaseRepository<Review>
    {
        public ReviewRepository(AppDbContext context) : base(context)
        {
        }

        public override List<Review> GetAll()
        {
            try
            {
                var reviewsList = _dbSet.AsNoTracking().ToList();  

                return reviewsList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении всех отзывов: {ex.Message}");
                return new List<Review>();
            }
        }

        public override Review GetById(int id)
        {
            try
            {
                var review = _dbSet
                    .AsNoTracking()
                    .FirstOrDefault(r => r.Id == id);
                    
                System.Diagnostics.Debug.WriteLine($"GetById: получен отзыв с ID={id}");
                
                return review;      
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении отзыва по ID: {ex.Message}");
                return null;
            }
        }

        public List<Review> GetBySubscription(int subscriptionId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetBySubscription: начало получения отзывов для абонемента с ID={subscriptionId}");
                
                var query = _dbSet
                    .AsNoTracking()
                    .Where(r => r.SubscriptionId == subscriptionId);
                    
                var sql = query.ToString();
                System.Diagnostics.Debug.WriteLine($"GetBySubscription: SQL-запрос: {sql}");
                
                var reviewsList = query.ToList();
                
                System.Diagnostics.Debug.WriteLine($"GetBySubscription: получено {reviewsList.Count} отзывов для абонемента {subscriptionId}");
                
                foreach (var review in reviewsList)
                {
                    System.Diagnostics.Debug.WriteLine($"GetBySubscription: отзыв ID={review.Id}, пользователь={review.User}, оценка={review.Score}, subscriptionId={review.SubscriptionId}");
                }
                
                return reviewsList;      
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении отзывов для абонемента: {ex.Message}");
                return new List<Review>();
            }
        }

        public List<Review> GetByRating(int minRating, int maxRating)
        {
            try
            {
                var reviewsList = _dbSet
                    .AsNoTracking()
                    .Where(r => r.Score >= minRating && r.Score <= maxRating)
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"GetByRating: получено {reviewsList.Count} отзывов с рейтингом от {minRating} до {maxRating}");
                
                return reviewsList;      
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении отзывов по рейтингу: {ex.Message}");
                return new List<Review>();
            }
        }

        public List<Review> GetRecentReviews(int count = 10)
        {
            try
            {
                var reviewsList = _dbSet
                    .AsNoTracking()
                    .Take(count)      
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"GetRecentReviews: получено {reviewsList.Count} последних отзывов");
                
                return reviewsList;      
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении последних отзывов: {ex.Message}");
                return new List<Review>();
            }
        }

        public double GetAverageRating(int subscriptionId)
        {
            try
            {
                var scores = _dbSet
                    .Where(r => r.SubscriptionId == subscriptionId)
                    .Select(r => r.Score)
                    .ToList();
                    
                if (scores.Count == 0)
                    return 0;
                    
                return scores.Average();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при расчете среднего рейтинга: {ex.Message}");
                return 0;
            }
        }
        
    
        public bool HasUserReviewedSubscription(string userName, int subscriptionId)
        {
            try
            {
                var count = _dbSet
                    .AsNoTracking()
                    .Count(r => r.SubscriptionId == subscriptionId && r.User == userName);
                
                System.Diagnostics.Debug.WriteLine($"Найдено {count} отзывов от пользователя '{userName}' для абонемента {subscriptionId}");
                
                return count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при проверке наличия отзыва от пользователя: {ex.Message}");
                return false;
            }
        }
    }
} 