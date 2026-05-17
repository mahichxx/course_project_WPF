using System;
using System.Collections.Generic;

namespace WPF_FitnessClub.Data.Repositories
{
    public interface IRepository<T> where T : class
    {
        List<T> GetAll();
        
        T GetById(int id);
        
        void Create(T entity);
        
        void Update(T entity);
        
        void Delete(T entity);
        
        void DeleteById(int id);
        
        void Save();
    }
} 