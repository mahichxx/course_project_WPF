using System;
using System.Collections.Generic;
using WPF_FitnessClub;

namespace WPF_FitnessClub.Data.Services
{
    /// <summary>
    /// Сервис для работы с отзывами
    /// </summary>
    public class ReviewService : DataService
    {
        /// <summary>
        /// Получить все отзывы
        /// </summary>
        public List<Review> GetAll()
        {
            return _unitOfWork.ReviewRepository.GetAll();
        }

        /// <summary>
        /// Получить отзыв по ID
        /// </summary>
        public Review GetById(int id)
        {
            return _unitOfWork.ReviewRepository.GetById(id);
        }

        /// <summary>
        /// Добавить новый отзыв
        /// </summary>
        public int Add(Review review)
        {
            _unitOfWork.ReviewRepository.Create(review);
            _unitOfWork.Save();
            return review.Id;
        }

        /// <summary>
        /// Обновить отзыв
        /// </summary>
        public bool Update(Review review)
        {
            try
            {
                _unitOfWork.ReviewRepository.Update(review);
                _unitOfWork.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Удалить отзыв
        /// </summary>
        public bool Delete(int id)
        {
            try
            {
                _unitOfWork.ReviewRepository.DeleteById(id);
                _unitOfWork.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Получить отзывы по абонементу
        /// </summary>
        public List<Review> GetBySubscription(int subscriptionId)
        {
            return _unitOfWork.ReviewRepository.GetBySubscription(subscriptionId);
        }

        /// <summary>
        /// Получить отзывы по рейтингу
        /// </summary>
        public List<Review> GetByRating(int minRating, int maxRating)
        {
            return _unitOfWork.ReviewRepository.GetByRating(minRating, maxRating);
        }

        /// <summary>
        /// Получить последние отзывы
        /// </summary>
        public List<Review> GetRecentReviews(int count = 10)
        {
            return _unitOfWork.ReviewRepository.GetRecentReviews(count);
        }

        /// <summary>
        /// Получить средний рейтинг абонемента
        /// </summary>
        public double GetAverageRating(int subscriptionId)
        {
            return _unitOfWork.ReviewRepository.GetAverageRating(subscriptionId);
        }

        /// <summary>
        /// Проверяет, оставлял ли пользователь уже отзыв на данный абонемент
        /// </summary>
        /// <param name="userName">Имя пользователя</param>
        /// <param name="subscriptionId">ID абонемента</param>
        /// <returns>true если пользователь уже оставлял отзыв, иначе false</returns>
        public bool HasUserReviewedSubscription(string userName, int subscriptionId)
        {
            return _unitOfWork.ReviewRepository.HasUserReviewedSubscription(userName, subscriptionId);
        }

        /// <summary>
        /// Проверяет, может ли пользователь оставить отзыв на абонемент
        /// (пользователь должен был когда-либо приобретать этот абонемент)
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="subscriptionId">ID абонемента</param>
        /// <returns>true если пользователь может оставить отзыв, иначе false</returns>
        public bool CanUserReviewSubscription(int userId, int subscriptionId)
        {
            // Создаем экземпляр UserSubscriptionRepository для проверки покупок
            var userSubscriptionRepository = new WPF_FitnessClub.Repositories.UserSubscriptionRepository();
            
            // Проверяем, приобретал ли пользователь когда-либо данный абонемент
            return userSubscriptionRepository.HasEverPurchasedSubscription(userId, subscriptionId);
        }
    }
} 