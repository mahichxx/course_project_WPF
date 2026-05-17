using System;
using System.Collections.Generic;
using System.Linq;
using WPF_FitnessClub.Data.Repositories;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Services
{
    public class CoachClientService : DataService
    {
        private readonly CoachClientRepository _coachClientRepository;

        public CoachClientService()
        {
            _coachClientRepository = new CoachClientRepository(_unitOfWork.Context);
        }

        /// <summary>
        /// Получить всех клиентов тренера
        /// </summary>
        public List<User> GetCoachClients(int coachId)
        {
            return _coachClientRepository.GetCoachClients(coachId);
        }

        public Dictionary<int, DateTime> GetClientAssignmentDates(int coachId)
        {
            try
            {
                return _coachClientRepository.GetClientAssignmentDates(coachId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении дат добавления клиентов: {ex.Message}");
                return new Dictionary<int, DateTime>();
            }
        }

        public bool IsClientAssignedToCoach(int clientId, int coachId)
        {
            return _coachClientRepository.IsClientAssignedToCoach(clientId, coachId);
        }

        public bool AssignClientToCoach(int clientId, int coachId)
        {
            System.Diagnostics.Debug.WriteLine($"CoachClientService.AssignClientToCoach начало: clientId={clientId}, coachId={coachId}");
            
            try
            {
                if (clientId <= 0 || coachId <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"CoachClientService: Неверные ID: clientId={clientId}, coachId={coachId}");
                    return false;
                }
                
                // Проверяем, не является ли клиент уже клиентом этого тренера
                if (IsClientAssignedToCoach(clientId, coachId))
                {
                    System.Diagnostics.Debug.WriteLine($"CoachClientService: Клиент уже назначен тренеру: clientId={clientId}, coachId={coachId}");
                    return true;
                }
                
                System.Diagnostics.Debug.WriteLine($"CoachClientService: Вызов репозитория для добавления связи");
                bool result = _coachClientRepository.AssignClientToCoach(clientId, coachId);
                
                System.Diagnostics.Debug.WriteLine($"CoachClientService: Результат добавления: {result}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CoachClientService: Исключение в AssignClientToCoach: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public bool RemoveClientFromCoach(int clientId, int coachId)
        {
            return _coachClientRepository.RemoveClientFromCoach(clientId, coachId);
        }


        public List<User> GetClientsWithoutCoach()
        {
            return _coachClientRepository.GetClientsWithoutCoach();
        }
    }
} 