using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Linq;
using WPF_FitnessClub.Data.Services;
using WPF_FitnessClub.Models;
using System.Collections.Generic;

namespace WPF_FitnessClub.ViewModels
{
    public class CoachClientsViewModel : ViewModelBase
    {
        private readonly UserService _userService;
        private CoachClientService _coachClientService;

        private User _currentCoach;
        private ObservableCollection<User> _clients;
        private ObservableCollection<User> _availableClients;
        private User _selectedClient;
        private User _selectedAvailableClient;
        private int _selectedTabIndex;
        
        private Dictionary<int, DateTime> _clientAssignmentDates;
        
        private SortType _currentSortType = SortType.ByName;
        
        public enum SortType
        {
            ByName,
            ByEmail,
            ByDate
        }

        public User CurrentCoach
        {
            get => _currentCoach;
            set
            {
                _currentCoach = value;
                OnPropertyChanged(nameof(CurrentCoach));
                
                if (_currentCoach != null)
                {
                    LoadClients();
                    LoadAvailableClients();
                }
            }
        }

        public ObservableCollection<User> Clients
        {
            get => _clients;
            set
            {
                _clients = value;
                OnPropertyChanged(nameof(Clients));
            }
        }
        
        public ObservableCollection<User> AvailableClients
        {
            get => _availableClients;
            set
            {
                _availableClients = value;
                OnPropertyChanged(nameof(AvailableClients));
            }
        }

        public User SelectedClient
        {
            get => _selectedClient;
            set
            {
                _selectedClient = value;
                OnPropertyChanged(nameof(SelectedClient));
            }
        }
        
        public User SelectedAvailableClient
        {
            get => _selectedAvailableClient;
            set
            {
                _selectedAvailableClient = value;
                OnPropertyChanged(nameof(SelectedAvailableClient));
            }
        }
        
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged(nameof(SelectedTabIndex));
                
                if (_selectedTabIndex == 1)
                {
                    LoadAvailableClients();
                }
            }
        }
        
        public DateTime GetClientAssignmentDate(int clientId)
        {
            if (_clientAssignmentDates != null && _clientAssignmentDates.ContainsKey(clientId))
            {
                return _clientAssignmentDates[clientId];
            }
            return DateTime.MinValue;
        }
        
        public CoachClientsViewModel(User coach)
        {
            _userService = new UserService();
            _coachClientService = new CoachClientService();
            
            Clients = new ObservableCollection<User>();
            AvailableClients = new ObservableCollection<User>();
            _clientAssignmentDates = new Dictionary<int, DateTime>();
            SelectedTabIndex = 0;      
            CurrentCoach = coach;
        }

        public void LoadClients()
        {
            try
            {
                var coachClients = _coachClientService.GetCoachClients(CurrentCoach.Id);
                
                _clientAssignmentDates = _coachClientService.GetClientAssignmentDates(CurrentCoach.Id);
                
                var sortedClients = SortClients(coachClients);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Clients.Clear();
                    foreach (var client in sortedClients)
                    {
                        Clients.Add(client);
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        string.Format((string)Application.Current.FindResource("ErrorRefreshingLists"), ex.Message),
                        (string)Application.Current.FindResource("ErrorTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }
        
        private List<User> SortClients(List<User> clients)
        {
            switch (_currentSortType)
            {
                case SortType.ByName:
                    return clients.OrderBy(c => c.FullName).ToList();
                case SortType.ByEmail:
                    return clients.OrderBy(c => c.Email).ToList();
                case SortType.ByDate:
                    return clients.OrderByDescending(c => GetClientAssignmentDate(c.Id)).ToList();
                default:
                    return clients;
            }
        }
        
        public void SortClientsList(SortType sortType)
        {
            _currentSortType = sortType;
            
            if (Clients != null && Clients.Count > 0)
            {
                var sortedClients = SortClients(Clients.ToList());
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Clients.Clear();
                    foreach (var client in sortedClients)
                    {
                        Clients.Add(client);
                    }
                });
            }
        }
        
        public void LoadAvailableClients()
        {
            try
            {
                if (CurrentCoach == null)
                {
                    return;
                }
                
                var clientsWithoutCoach = _coachClientService.GetClientsWithoutCoach();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableClients.Clear();
                    foreach (var client in clientsWithoutCoach)
                    {
                        AvailableClients.Add(client);
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        string.Format((string)Application.Current.FindResource("ErrorRefreshingLists"), ex.Message),
                        (string)Application.Current.FindResource("ErrorTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }
        
        public void AddClientToCoach(User client)
        {
            if (client == null || CurrentCoach == null)
            {
                MessageBox.Show(
                    (string)Application.Current.FindResource("ErrorGettingClientData"),
                    (string)Application.Current.FindResource("ErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            
            if (_coachClientService == null)
            {
                _coachClientService = new CoachClientService();
            }
            
            if (client.Id <= 0 || CurrentCoach.Id <= 0)
            {
                MessageBox.Show(
                    (string)Application.Current.FindResource("ErrorGettingClientData"),
                    (string)Application.Current.FindResource("ErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            
            try
            {
                int clientId = client.Id;
                int coachId = CurrentCoach.Id;
                
                bool result = _coachClientService.AssignClientToCoach(clientId, coachId);
                
                if (result)
                {
                    LoadClients();
                    LoadAvailableClients();
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            string.Format((string)Application.Current.FindResource("ClientAddedSuccessfully"), client.FullName),
                            (string)Application.Current.FindResource("SuccessTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            string.Format((string)Application.Current.FindResource("FailedToAddClient"), client.FullName),
                            (string)Application.Current.FindResource("ErrorTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        string.Format((string)Application.Current.FindResource("ErrorAddingClient"), ex.Message),
                        (string)Application.Current.FindResource("ErrorTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }

        public void RemoveClientFromCoach(User client)
        {
            if (client == null || CurrentCoach == null)
            {
                MessageBox.Show(
                    (string)Application.Current.FindResource("ErrorGettingClientData"),
                    (string)Application.Current.FindResource("ErrorTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            
            if (_coachClientService == null)
            {
                _coachClientService = new CoachClientService();
            }
            
            try
            {
                int clientId = client.Id;
                int coachId = CurrentCoach.Id;
                
                bool result = _coachClientService.RemoveClientFromCoach(clientId, coachId);
                
                if (result)
                {
                    LoadClients();
                    LoadAvailableClients();
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            string.Format((string)Application.Current.FindResource("ClientRemovedSuccessfully"), client.FullName),
                            (string)Application.Current.FindResource("SuccessTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            string.Format((string)Application.Current.FindResource("FailedToRemoveClient"), client.FullName),
                            (string)Application.Current.FindResource("ErrorTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        string.Format((string)Application.Current.FindResource("ErrorRemovingClient"), ex.Message),
                        (string)Application.Current.FindResource("ErrorTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }
    }
} 