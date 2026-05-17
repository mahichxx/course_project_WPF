using System;
using WPF_FitnessClub.Data.Repositories;

namespace WPF_FitnessClub.Data
{
    public class UnitOfWork : IDisposable
    {
        private readonly AppDbContext _context;
        private bool _disposed = false;

        private UserRepository _userRepository;
        private SubscriptionRepository _subscriptionRepository;
        private ReviewRepository _reviewRepository;
        private NutritionPlanRepository _nutritionPlanRepository;

        public UnitOfWork()
        {
            _context = new AppDbContext();
        }

        public AppDbContext Context
        {
            get { return _context; }
        }

        public UserRepository UserRepository
        {
            get
            {
                if (_userRepository == null)
                {
                    _userRepository = new UserRepository(_context);
                }
                return _userRepository;
            }
        }

        public SubscriptionRepository SubscriptionRepository
        {
            get
            {
                if (_subscriptionRepository == null)
                {
                    _subscriptionRepository = new SubscriptionRepository(_context);
                }
                return _subscriptionRepository;
            }
        }

        public ReviewRepository ReviewRepository
        {
            get
            {
                if (_reviewRepository == null)
                {
                    _reviewRepository = new ReviewRepository(_context);
                }
                return _reviewRepository;
            }
        }

        public NutritionPlanRepository NutritionPlanRepository
        {
            get
            {
                if (_nutritionPlanRepository == null)
                {
                    _nutritionPlanRepository = new NutritionPlanRepository(_context);
                }
                return _nutritionPlanRepository;
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
} 