using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using WPF_FitnessClub.Data.Services;
using WPF_FitnessClub.Models;
using static WPF_FitnessClub.Commands;
using System.Collections.Generic;
using WPF_FitnessClub.View;
using System.Configuration;
using System.Windows.Controls;

namespace WPF_FitnessClub.ViewModels
{
    public class ClientPlansDetailsViewModel : ViewModelBase
    {
        private readonly WorkoutPlanService _workoutPlanService;
        private readonly NutritionPlanService _nutritionPlanService;
        private readonly UserService _userService;
        
        private User _client;
        private ObservableCollection<WorkoutPlan> _workoutPlans;
        private ObservableCollection<NutritionPlan> _nutritionPlans;
        private WorkoutPlan _selectedWorkoutPlan;
        private NutritionPlan _selectedNutritionPlan;
        private bool _isLoading;
        private int _selectedTabIndex;
        
        private string _newNutritionPlanTitle;
        private string _newNutritionPlanDescription;
        private DateTime _newNutritionPlanStartDate = DateTime.Today;
        private DateTime _newNutritionPlanEndDate = DateTime.Today.AddMonths(1);
        
        private string _newWorkoutPlanTitle;
        private string _newWorkoutPlanDescription;
        private DateTime _newWorkoutPlanStartDate = DateTime.Today;
        private DateTime _newWorkoutPlanEndDate = DateTime.Today.AddMonths(1);
        
        private DateTime _currentDate = DateTime.Today;
        
        public ICommand AddNutritionPlanCommand { get; private set; }
        public ICommand EditNutritionPlanCommand { get; private set; }
        public ICommand DeleteNutritionPlanCommand { get; private set; }
        public ICommand SaveNutritionPlanCommand { get; private set; }
        public ICommand CancelNutritionPlanEditCommand { get; private set; }
        
        public ICommand AddWorkoutPlanCommand { get; private set; }
        public ICommand EditWorkoutPlanCommand { get; private set; }
        public ICommand DeleteWorkoutPlanCommand { get; private set; }
        public ICommand SaveWorkoutPlanCommand { get; private set; }
        public ICommand CancelWorkoutPlanEditCommand { get; private set; }

        public User Client
        {
            get => _client;
            set
            {
                _client = value;
                OnPropertyChanged("Client");
                
                if (_client != null)
                {
                    LoadClientPlans();
                }
            }
        }

        public ObservableCollection<WorkoutPlan> WorkoutPlans
        {
            get => _workoutPlans;
            set
            {
                _workoutPlans = value;
                OnPropertyChanged("WorkoutPlans");
            }
        }

        public ObservableCollection<NutritionPlan> NutritionPlans
        {
            get => _nutritionPlans;
            set
            {
                _nutritionPlans = value;
                OnPropertyChanged("NutritionPlans");
            }
        }

        public WorkoutPlan SelectedWorkoutPlan
        {
            get => _selectedWorkoutPlan;
            set
            {
                _selectedWorkoutPlan = value;
                OnPropertyChanged("SelectedWorkoutPlan");
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        public NutritionPlan SelectedNutritionPlan
        {
            get => _selectedNutritionPlan;
            set
            {
                _selectedNutritionPlan = value;
                OnPropertyChanged("SelectedNutritionPlan");
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
     
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                OnPropertyChanged("SelectedTabIndex");
            }
        }

        public string NewNutritionPlanTitle
        {
            get => _newNutritionPlanTitle;
            set
            {
                _newNutritionPlanTitle = value;
                OnPropertyChanged("NewNutritionPlanTitle");
            }
        }
        
        public string NewNutritionPlanDescription
        {
            get => _newNutritionPlanDescription;
            set
            {
                _newNutritionPlanDescription = value;
                OnPropertyChanged("NewNutritionPlanDescription");
            }
        }
        
        public DateTime NewNutritionPlanStartDate
        {
            get => _newNutritionPlanStartDate;
            set
            {
                _newNutritionPlanStartDate = value;
                
                if (_newNutritionPlanStartDate > _newNutritionPlanEndDate)
                {
                    NewNutritionPlanEndDate = _newNutritionPlanStartDate.AddMonths(1);
                }
                
                OnPropertyChanged("NewNutritionPlanStartDate");
            }
        }
        
        public DateTime NewNutritionPlanEndDate
        {
            get => _newNutritionPlanEndDate;
            set
            {
                _newNutritionPlanEndDate = value;
                OnPropertyChanged("NewNutritionPlanEndDate");
            }
        }
        
        private bool _isNutritionPlanEditMode;
        public bool IsNutritionPlanEditMode
        {
            get => _isNutritionPlanEditMode;
            set
            {
                _isNutritionPlanEditMode = value;
                OnPropertyChanged("IsNutritionPlanEditMode");
                OnPropertyChanged("IsNutritionPlanViewMode");
            }
        }
        
        public bool IsNutritionPlanViewMode => !_isNutritionPlanEditMode;
        
        private bool _isEditingExistingNutritionPlan;
        public bool IsEditingExistingNutritionPlan
        {
            get => _isEditingExistingNutritionPlan;
            set
            {
                _isEditingExistingNutritionPlan = value;
                OnPropertyChanged("IsEditingExistingNutritionPlan");
            }
        }

        public string NewWorkoutPlanTitle
        {
            get => _newWorkoutPlanTitle;
            set
            {
                _newWorkoutPlanTitle = value;
                OnPropertyChanged("NewWorkoutPlanTitle");
            }
        }
        
        public string NewWorkoutPlanDescription
        {
            get => _newWorkoutPlanDescription;
            set
            {
                _newWorkoutPlanDescription = value;
                OnPropertyChanged("NewWorkoutPlanDescription");
            }
        }
        
        public DateTime NewWorkoutPlanStartDate
        {
            get => _newWorkoutPlanStartDate;
            set
            {
                _newWorkoutPlanStartDate = value;
                
                if (_newWorkoutPlanStartDate > _newWorkoutPlanEndDate)
                {
                    NewWorkoutPlanEndDate = _newWorkoutPlanStartDate.AddMonths(1);
                }
                
                OnPropertyChanged("NewWorkoutPlanStartDate");
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }
        public DateTime NewWorkoutPlanEndDate
        {
            get => _newWorkoutPlanEndDate;
            set
            {
                _newWorkoutPlanEndDate = value;
                OnPropertyChanged("NewWorkoutPlanEndDate");
            }
        }
        
        private bool _isWorkoutPlanEditMode;
        public bool IsWorkoutPlanEditMode
        {
            get => _isWorkoutPlanEditMode;
            set
            {
                _isWorkoutPlanEditMode = value;
                OnPropertyChanged(nameof(IsWorkoutPlanEditMode));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public bool IsWorkoutPlanViewMode => !_isWorkoutPlanEditMode;
        
        private bool _isEditingExistingWorkoutPlan;
        public bool IsEditingExistingWorkoutPlan
        {
            get => _isEditingExistingWorkoutPlan;
            set
            {
                _isEditingExistingWorkoutPlan = value;
                OnPropertyChanged("IsEditingExistingWorkoutPlan");
            }
        }

        public DateTime CurrentDate
        {
            get => _currentDate;
            set
            {
                _currentDate = value;
                OnPropertyChanged("CurrentDate");
            }
        }

        public ClientPlansDetailsViewModel(User client)
        {
            _workoutPlanService = new WorkoutPlanService();
            _nutritionPlanService = new NutritionPlanService();
            _userService = new UserService();
            
            WorkoutPlans = new ObservableCollection<WorkoutPlan>();
            NutritionPlans = new ObservableCollection<NutritionPlan>();
            
            SelectedTabIndex = 0;      
            
            AddNutritionPlanCommand = new RelayCommand(ExecuteAddNutritionPlan);
            EditNutritionPlanCommand = new RelayCommand(ExecuteEditNutritionPlan, CanExecuteEditNutritionPlan);
            DeleteNutritionPlanCommand = new RelayCommand(ExecuteDeleteNutritionPlan, CanExecuteDeleteNutritionPlan);
            SaveNutritionPlanCommand = new RelayCommand(ExecuteSaveNutritionPlan, CanExecuteSaveNutritionPlan);
            CancelNutritionPlanEditCommand = new RelayCommand(ExecuteCancelNutritionPlanEdit);
            
            AddWorkoutPlanCommand = new RelayCommand(ExecuteAddWorkoutPlan);
            EditWorkoutPlanCommand = new RelayCommand(ExecuteEditWorkoutPlan, CanExecuteEditWorkoutPlan);
            DeleteWorkoutPlanCommand = new RelayCommand(ExecuteDeleteWorkoutPlan, CanExecuteDeleteWorkoutPlan);
            SaveWorkoutPlanCommand = new RelayCommand(ExecuteSaveWorkoutPlan, CanExecuteSaveWorkoutPlan);
            CancelWorkoutPlanEditCommand = new RelayCommand(ExecuteCancelWorkoutPlanEdit);
            
            ResetNewNutritionPlanFields();
            ResetNewWorkoutPlanFields();
            
            Client = client;
        }

        private void ResetNewNutritionPlanFields()
        {
            NewNutritionPlanTitle = string.Empty;
            NewNutritionPlanDescription = string.Empty;
            NewNutritionPlanStartDate = DateTime.Today;
            NewNutritionPlanEndDate = DateTime.Today.AddMonths(1);
        }

        private void ExecuteAddNutritionPlan(object parameter)
        {
            IsEditingExistingNutritionPlan = false;
            ResetNewNutritionPlanFields();
            IsNutritionPlanEditMode = true;
        }
        
        private void ExecuteEditNutritionPlan(object parameter)
        {
            if (SelectedNutritionPlan != null)
            {
                IsEditingExistingNutritionPlan = true;
                
                NewNutritionPlanTitle = SelectedNutritionPlan.Title;
                NewNutritionPlanDescription = SelectedNutritionPlan.Description;
                NewNutritionPlanStartDate = SelectedNutritionPlan.StartDate;
                NewNutritionPlanEndDate = SelectedNutritionPlan.EndDate;
                
                IsNutritionPlanEditMode = true;
            }
        }
        
        private void ExecuteDeleteNutritionPlan(object parameter)
        {
            if (SelectedNutritionPlan != null)
            {
                var result = MessageBox.Show(
                    string.Format(Application.Current.FindResource("ConfirmDeleteNutritionPlan") as string, SelectedNutritionPlan.Title),
                    Application.Current.FindResource("ConfirmationDelete") as string,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        
                        int planId = SelectedNutritionPlan.Id;
                        var planToRemove = SelectedNutritionPlan;
                        
                        SelectedNutritionPlan = null;
                        
                        _nutritionPlanService.Delete(planId);
                        
                        NutritionPlans.Remove(planToRemove);
                        
                        MessageBox.Show(
                            Application.Current.FindResource("NutritionPlanDeleteSuccess") as string,
                            Application.Current.FindResource("Success") as string,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            string.Format(Application.Current.FindResource("NutritionPlanDeleteError") as string, 
                                ex.Message, ex.InnerException?.Message),
                            Application.Current.FindResource("Error") as string,
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        
                        LoadClientPlans();
                    }
                }
            }
        }

        private void ExecuteSaveNutritionPlan(object parameter)
        {
            // Валидация (сохраняем твою логику)
            if (string.IsNullOrWhiteSpace(NewNutritionPlanTitle) || NewNutritionPlanTitle.Length < 3)
            {
                MessageBox.Show("Введите корректное название (мин. 3 символа)");
                return;
            }

            try
            {
                IsLoading = true;

                if (IsEditingExistingNutritionPlan) // РЕДАКТИРОВАНИЕ
                {
                    if (SelectedNutritionPlan != null)
                    {
                        // Заполняем объект данными из текстовых полей
                        SelectedNutritionPlan.Title = NewNutritionPlanTitle.Trim();
                        SelectedNutritionPlan.Description = NewNutritionPlanDescription.Trim();
                        SelectedNutritionPlan.StartDate = NewNutritionPlanStartDate;
                        SelectedNutritionPlan.EndDate = NewNutritionPlanEndDate;

                        // Отправляем в сервис
                        _nutritionPlanService.Update(SelectedNutritionPlan);
                        MessageBox.Show("Изменения сохранены!", "Успех");
                    }
                }
                else // СОЗДАНИЕ
                {
                    var coach = _userService.GetCurrentUser();
                    var newPlan = new NutritionPlan
                    {
                        Title = NewNutritionPlanTitle.Trim(),
                        Description = NewNutritionPlanDescription.Trim(),
                        ClientId = Client.Id,
                        CoachId = coach.Id,
                        StartDate = NewNutritionPlanStartDate,
                        EndDate = NewNutritionPlanEndDate,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                        IsCompleted = false
                    };

                    _nutritionPlanService.Create(newPlan);
                    MessageBox.Show("План создан!", "Успех");
                }

                IsNutritionPlanEditMode = false;
                LoadClientPlans(); // Принудительно обновляем список на экране
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
            finally { IsLoading = false; }
        }

        private void ExecuteCancelNutritionPlanEdit(object parameter)
        {
            IsNutritionPlanEditMode = false;
            ResetNewNutritionPlanFields();
        }
        
        private bool CanExecuteEditNutritionPlan(object parameter)
        {
            return SelectedNutritionPlan != null && !IsNutritionPlanEditMode;
        }
        
        private bool CanExecuteDeleteNutritionPlan(object parameter)
        {
            return SelectedNutritionPlan != null && !IsNutritionPlanEditMode;
        }

        private bool CanExecuteSaveNutritionPlan(object parameter)
        {
            return IsNutritionPlanEditMode;
        }


        public void LoadClientPlans()
        {
            try
            {
                
                var workoutPlans = _workoutPlanService.GetWorkoutPlansByClientId(Client.Id);
                var nutritionPlans = _nutritionPlanService.GetNutritionPlansByClientId(Client.Id);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WorkoutPlans.Clear();
                    foreach (var plan in workoutPlans)
                    {
                        WorkoutPlans.Add(plan);
                    }
                    
                    NutritionPlans.Clear();
                    foreach (var plan in nutritionPlans)
                    {
                        NutritionPlans.Add(plan);
                    }
                });
                
                System.Diagnostics.Debug.WriteLine($"Загружено {WorkoutPlans.Count} планов тренировок и {NutritionPlans.Count} планов питания для клиента {Client.FullName} (ID: {Client.Id})");
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        string.Format(Application.Current.FindResource("LoadClientPlansError") as string, ex.Message),
                        Application.Current.FindResource("Error") as string,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
                
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке планов клиента: {ex.Message}");
            }
        }

        private void ExecuteAddWorkoutPlan(object parameter)
        {
            IsEditingExistingWorkoutPlan = false;
            ResetNewWorkoutPlanFields();
            IsWorkoutPlanEditMode = true;
        }
        
        private void ExecuteEditWorkoutPlan(object parameter)
        {
            if (SelectedWorkoutPlan != null)
            {
                IsEditingExistingWorkoutPlan = true;
                
                NewWorkoutPlanTitle = SelectedWorkoutPlan.Title;
                NewWorkoutPlanDescription = SelectedWorkoutPlan.Description;
                NewWorkoutPlanStartDate = SelectedWorkoutPlan.StartDate;
                NewWorkoutPlanEndDate = SelectedWorkoutPlan.EndDate;
                
                IsWorkoutPlanEditMode = true;
            }
        }
        
        private void ExecuteDeleteWorkoutPlan(object parameter)
        {
            if (SelectedWorkoutPlan != null)
            {
                var result = MessageBox.Show(
                    string.Format(Application.Current.FindResource("ConfirmDeleteWorkoutPlan") as string, SelectedWorkoutPlan.Title),
                    Application.Current.FindResource("ConfirmationDelete") as string,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        
                        int planId = SelectedWorkoutPlan.Id;
                        var planToRemove = SelectedWorkoutPlan;
                        
                        SelectedWorkoutPlan = null;
                        
                        _workoutPlanService.Delete(planId);
                        
                        WorkoutPlans.Remove(planToRemove);
                        
                        MessageBox.Show(
                            Application.Current.FindResource("WorkoutPlanDeleteSuccess") as string,
                            Application.Current.FindResource("Success") as string,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            string.Format(Application.Current.FindResource("WorkoutPlanDeleteError") as string, 
                                ex.Message, ex.InnerException?.Message),
                            Application.Current.FindResource("Error") as string,
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        
                        LoadClientPlans();
                    }
                }
            }
        }

        private void ExecuteSaveWorkoutPlan(object parameter)
        {
            // 1. Валидация на пустые поля и длину (минимум 3 символа)
            if (string.IsNullOrWhiteSpace(NewWorkoutPlanTitle) || NewWorkoutPlanTitle.Length < 3 ||
                string.IsNullOrWhiteSpace(NewWorkoutPlanDescription) || NewWorkoutPlanDescription.Length < 3)
            {
                MessageBox.Show("Пожалуйста, заполните все поля! Название и описание должны быть не менее 3 символов.",
                                "Ошибка заполнения", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewWorkoutPlanEndDate < NewWorkoutPlanStartDate)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала!", "Ошибка даты");
                return;
            }

            try
            {
                IsLoading = true;

                if (IsEditingExistingWorkoutPlan) // РЕДАКТИРОВАНИЕ
                {
                    if (SelectedWorkoutPlan != null)
                    {
                        SelectedWorkoutPlan.Title = NewWorkoutPlanTitle;
                        SelectedWorkoutPlan.Description = NewWorkoutPlanDescription;
                        SelectedWorkoutPlan.StartDate = NewWorkoutPlanStartDate;
                        SelectedWorkoutPlan.EndDate = NewWorkoutPlanEndDate;
                        SelectedWorkoutPlan.UpdatedDate = DateTime.Now;

                        _workoutPlanService.Update(SelectedWorkoutPlan);
                        MessageBox.Show("План тренировок успешно обновлен!", "Успех");
                    }
                }
                else // СОЗДАНИЕ НОВОГО
                {
                    var coach = _userService.GetCurrentUser();
                    WorkoutPlan newPlan = new WorkoutPlan
                    {
                        Title = NewWorkoutPlanTitle,
                        Description = NewWorkoutPlanDescription,
                        ClientId = Client.Id,
                        CoachId = coach.Id,
                        StartDate = NewWorkoutPlanStartDate,
                        EndDate = NewWorkoutPlanEndDate,
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                        IsCompleted = false
                    };

                    _workoutPlanService.Create(newPlan);
                    MessageBox.Show("Новый план тренировок создан!", "Успех");
                }

                IsWorkoutPlanEditMode = false;
                LoadClientPlans(); // Принудительно обновляем список на экране
            }
            catch (Exception ex) { MessageBox.Show("Ошибка сохранения: " + ex.Message); }
            finally { IsLoading = false; }
        }

        private void ExecuteCancelWorkoutPlanEdit(object parameter)
        {
            IsWorkoutPlanEditMode = false;
            ResetNewWorkoutPlanFields();
        }
        
        private bool CanExecuteEditWorkoutPlan(object parameter)
        {
            return SelectedWorkoutPlan != null && !IsWorkoutPlanEditMode;
        }
        
        private bool CanExecuteDeleteWorkoutPlan(object parameter)
        {
            return SelectedWorkoutPlan != null && !IsWorkoutPlanEditMode;
        }

        private bool CanExecuteSaveWorkoutPlan(object parameter)
        {
            // Разрешаем нажать всегда в режиме редактирования, чтобы сработал наш MessageBox с ошибкой
            return IsWorkoutPlanEditMode;
        }

        private void ResetNewWorkoutPlanFields()
        {
            NewWorkoutPlanTitle = string.Empty;
            NewWorkoutPlanDescription = string.Empty;
            NewWorkoutPlanStartDate = DateTime.Today;
            NewWorkoutPlanEndDate = DateTime.Today.AddMonths(1);
        }
    }
} 