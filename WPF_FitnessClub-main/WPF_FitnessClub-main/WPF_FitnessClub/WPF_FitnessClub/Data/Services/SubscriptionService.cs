using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Services
{
    /// <summary>
    /// Сервис для работы с абонементами
    /// </summary>
    public class SubscriptionService : DataService
    {
        /// <summary>
        /// Получить все абонементы
        /// </summary>
        public List<Subscription> GetAll()
        {
            return _unitOfWork.SubscriptionRepository.GetAll();
        }

        /// <summary>
        /// Получить абонемент по ID
        /// </summary>
        public Subscription GetById(int id)
        {
            return _unitOfWork.SubscriptionRepository.GetById(id);
        }

        /// <summary>
        /// Добавить новый абонемент
        /// </summary>
        public int Add(Subscription subscription)
        {
            _unitOfWork.SubscriptionRepository.Create(subscription);
            _unitOfWork.Save();
            return subscription.Id;
        }

        /// <summary>
        /// Обновить абонемент
        /// </summary>
        public bool Update(Subscription subscription)
        {
            try
            {
                _unitOfWork.SubscriptionRepository.Update(subscription);
                _unitOfWork.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Удалить абонемент
        /// </summary>
        public bool Delete(int id)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    // Удаляем все отзывы для этого абонемента напрямую через SQL-запрос
                    var reviews = context.Reviews.Where(r => r.SubscriptionId == id).ToList();
                    foreach (var review in reviews)
                    {
                        context.Reviews.Remove(review);
                    }
                    
                    // Сохраняем изменения после удаления отзывов
                    context.SaveChanges();
                    
                    // Теперь удаляем сам абонемент
                    var subscription = context.Subscriptions.Find(id);
                    if (subscription != null)
                    {
                        context.Subscriptions.Remove(subscription);
                        context.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при удалении абонемента: {ex.Message}");
                return false;
            }
        }
    }
} 