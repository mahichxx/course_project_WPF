using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_FitnessClub.Data;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Repositories
{
    public class UserSubscriptionRepository
    {
        private readonly AppDbContext _context;

        public UserSubscriptionRepository()
        {
            _context = new AppDbContext();
        }

        public UserSubscriptionRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public List<UserSubscription> GetAll()
        {
            return _context.UserSubscriptions
                .Include(us => us.User)
                .Include(us => us.Subscription)
                .ToList();
        }

        public List<UserSubscription> GetByUserId(int userId)
        {
            return _context.UserSubscriptions
                .Where(us => us.UserId == userId)
                .Include(us => us.Subscription)
                .ToList();
        }

        public List<UserSubscription> GetActiveByUserId(int userId)
        {
            DateTime now = DateTime.Now;
            return _context.UserSubscriptions
                .Where(us => us.UserId == userId && us.ExpiryDate >= now)
                .Include(us => us.Subscription)
                .ToList();
        }

        public UserSubscription GetById(int id)
        {
            return _context.UserSubscriptions
                .Include(us => us.User)
                .Include(us => us.Subscription)
                .FirstOrDefault(us => us.Id == id);
        }

        public UserSubscription Add(UserSubscription userSubscription)
        {
            if (userSubscription == null)
                throw new ArgumentNullException(nameof(userSubscription));

            _context.UserSubscriptions.Add(userSubscription);
            _context.SaveChanges();
            return userSubscription;
        }

        public bool Update(UserSubscription userSubscription)
        {
            if (userSubscription == null)
                throw new ArgumentNullException(nameof(userSubscription));

            var existingEntity = _context.UserSubscriptions.Find(userSubscription.Id);
            if (existingEntity == null)
                return false;

            _context.Entry(existingEntity).CurrentValues.SetValues(userSubscription);
            _context.SaveChanges();
            return true;
        }

        public bool Delete(int id)
        {
            var userSubscription = _context.UserSubscriptions.Find(id);
            if (userSubscription == null)
                return false;

            _context.UserSubscriptions.Remove(userSubscription);
            _context.SaveChanges();
            return true;
        }

        public bool HasActiveSubscription(int userId, int subscriptionId)
        {
            DateTime now = DateTime.Now;
            return _context.UserSubscriptions
                .Any(us => us.UserId == userId && 
                           us.SubscriptionId == subscriptionId && 
                           us.ExpiryDate >= now);
        }

        public List<UserSubscription> GetUserSubscriptionsByUserId(int userId)
        {
            return GetByUserId(userId);
        }

        public bool CancelUserSubscription(int id)
        {
            var subscription = GetById(id);
            if (subscription == null)
                return false;
                
            subscription.ExpiryDate = DateTime.Now;
            return Update(subscription);
        }

        public bool HasEverPurchasedSubscription(int userId, int subscriptionId)
        {
            return _context.UserSubscriptions
                .Any(us => us.UserId == userId && us.SubscriptionId == subscriptionId);
        }
    }
} 