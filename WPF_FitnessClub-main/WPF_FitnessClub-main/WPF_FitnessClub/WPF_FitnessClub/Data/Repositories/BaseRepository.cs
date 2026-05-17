using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace WPF_FitnessClub.Data.Repositories
{
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        protected BaseRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual List<T> GetAll()
        {
            return _dbSet.ToList();
        }

        public virtual T GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public virtual void Create(T entity)
        {
            _dbSet.Add(entity);
        }

        public virtual void Update(T entity)
        {
            try
            {
                var entry = _context.Entry(entity);
                if (entry.State == EntityState.Detached)
                {
                    _dbSet.Attach(entity);
                }
                
                entry.State = EntityState.Modified;
                
                System.Diagnostics.Debug.WriteLine($"Сущность {typeof(T).Name} помечена как измененная");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении сущности {typeof(T).Name}: {ex.Message}");
                throw;        
            }
        }

        public virtual void Delete(T entity)
        {
            try
            {
                if (_context.Entry(entity).State == EntityState.Detached)
                {
                    _dbSet.Attach(entity);
                }
                _dbSet.Remove(entity);
                System.Diagnostics.Debug.WriteLine($"Сущность {typeof(T).Name} помечена для удаления");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при удалении сущности {typeof(T).Name}: {ex.Message}");
                throw;        
            }
        }

        public virtual void DeleteById(int id)
        {
            try
            {
                T entity = GetById(id);
                if (entity != null)
                {
                    Delete(entity);
                    System.Diagnostics.Debug.WriteLine($"Сущность {typeof(T).Name} с ID {id} помечена для удаления");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Сущность {typeof(T).Name} с ID {id} не найдена для удаления");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при удалении сущности {typeof(T).Name} с ID {id}: {ex.Message}");
                throw;        
            }
        }

        public void Save()
        {
            try
            {
                _context.ChangeTracker.DetectChanges();
                
                int changes = _context.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine($"Сохранено {changes} изменений в базе данных для сущностей {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении изменений для сущности {typeof(T).Name}: {ex.Message}");
                throw;        
            }
        }
    }
} 