using System;
using System.Collections.Generic;

namespace WPF_FitnessClub.Data.Services
{
    /// <summary>
    /// Базовый класс для сервисов данных, обеспечивающих взаимодействие ViewModels с UnitOfWork
    /// </summary>
    public abstract class DataService : IDisposable
    {
        protected readonly UnitOfWork _unitOfWork;
        private bool _disposed = false;

        public DataService()
        {
            _unitOfWork = new UnitOfWork();
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _unitOfWork.Dispose();
                }

                _disposed = true;
            }
        }
    }
} 