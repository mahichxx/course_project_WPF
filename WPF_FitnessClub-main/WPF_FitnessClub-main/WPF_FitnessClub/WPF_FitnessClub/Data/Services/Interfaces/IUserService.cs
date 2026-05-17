using System.Collections.Generic;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Services.Interfaces
{
    public interface IUserService
    {
        User GetById(int id);
        List<User> GetAll();
        List<User> GetAllClients();
        List<User> GetAllCoaches();
        List<User> GetCoachClients(int coachId);
        User Create(User user);
        User Update(User user);
        void Delete(int id);
        User GetByUsername(string username);
        bool Exists(string username);
    }
} 