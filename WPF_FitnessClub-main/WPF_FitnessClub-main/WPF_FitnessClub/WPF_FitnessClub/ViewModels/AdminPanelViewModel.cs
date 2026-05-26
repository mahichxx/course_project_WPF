using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using static WPF_FitnessClub.Commands;
using WPF_FitnessClub.Models;
using WPF_FitnessClub.Data;
using WPF_FitnessClub.Data.Services;
using System.IO;
using System.Text;

namespace WPF_FitnessClub.ViewModels
{
    public class AdminPanelViewModel : ViewModelBase
    {
        private readonly AppDbContext _context;
        private readonly UserService _userService;
        private readonly DatabaseBackupService _backupService;
        private bool _isLoading;
        private ObservableCollection<User> _usersTable;
        private User _selectedUser;

        public AdminPanelViewModel()
        {
            try
            {
                _isLoading = true;
                OnPropertyChanged(nameof(IsLoading));

                _context = new AppDbContext();
                _userService = new UserService();
                _backupService = new DatabaseBackupService();

                RefreshCommand = new RelayCommand(ExecuteRefreshCommand);
                BlockUserCommand = new RelayCommand(ExecuteBlockUserCommand, (p) => p is User);
                DeleteUserCommand = new RelayCommand(ExecuteDeleteUserCommand, (p) => p is User);
                AddUserCommand = new RelayCommand(ExecuteAddUserCommand);

                LoadUsersData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(string)Application.Current.Resources["AdminPanelInitError"]}: {ex.Message}",
                    (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        #region Свойства

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

        public ObservableCollection<User> UsersTable
        {
            get => _usersTable;
            set
            {
                if (_usersTable != value)
                {
                    _usersTable = value;
                    OnPropertyChanged(nameof(UsersTable));
                }
            }
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (_selectedUser != value)
                {
                    _selectedUser = value;
                    OnPropertyChanged(nameof(SelectedUser));
                }
            }
        }

        #endregion

        #region Команды

        public ICommand RefreshCommand { get; private set; }
        public ICommand BlockUserCommand { get; private set; }
        public ICommand DeleteUserCommand { get; private set; }
        public ICommand AddUserCommand { get; private set; }
        public ICommand ExportToJsonCommand { get; private set; }

        #endregion

        #region Методы команд

        private void ExecuteRefreshCommand(object parameter)
        {
            try
            {
                IsLoading = true;
                LoadUsersData();
                MessageBox.Show((string)Application.Current.Resources["AdminPanelDataUpdated"], 
                    (string)Application.Current.Resources["AdminPanelSuccess"], MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(string)Application.Current.Resources["AdminPanelLoadUsersError"]}: {ex.Message}",
                    (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteBlockUserCommand(object parameter)
        {
            try
            {
                if (parameter is User user)
                {
                    if (user.Id == 1)
                    {
                        MessageBox.Show((string)Application.Current.Resources["AdminPanelBlockAdminError"],
                            (string)Application.Current.Resources["AdminPanelLimitationTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    User currentUser = _userService.GetCurrentUser();
                    if (currentUser != null && currentUser.Id == user.Id)
                    {
                        MessageBox.Show((string)Application.Current.Resources["AdminPanelBlockSelfError"],
                            (string)Application.Current.Resources["AdminPanelLimitationTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    var freshUser = _userService.GetById(user.Id);
                    if (freshUser == null)
                    {
                        MessageBox.Show(string.Format((string)Application.Current.Resources["AdminPanelUserNotFound"], user.Id),
                            (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    freshUser.IsBlocked = !freshUser.IsBlocked;

                    bool success = _userService.Update(freshUser);
                    
                    if (success)
                    {
                        user.IsBlocked = freshUser.IsBlocked;
                        
                        string message = freshUser.IsBlocked
                            ? (string)Application.Current.Resources["AdminPanelUserBlocked"]
                            : (string)Application.Current.Resources["AdminPanelUserUnblocked"];

                        MessageBox.Show(message, (string)Application.Current.Resources["AdminPanelSuccess"], MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        LoadUsersData();
                    }
                    else
                    {
                        MessageBox.Show((string)Application.Current.Resources["AdminPanelUpdateUserError"],
                            (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(string)Application.Current.Resources["AdminPanelBlockUserError"]}: {ex.Message}",
                    (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteUserCommand(object parameter)
        {
            try
            {
                if (parameter is User user)
                {
                    if (user.Id == 1)
                    {
                        MessageBox.Show((string)Application.Current.Resources["AdminPanelDeleteAdminError"],
                            (string)Application.Current.Resources["AdminPanelLimitationTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    User currentUser = _userService.GetCurrentUser();
                    if (currentUser != null && currentUser.Id == user.Id)
                    {
                        MessageBox.Show((string)Application.Current.Resources["AdminPanelBlockSelfError"],
                            (string)Application.Current.Resources["AdminPanelLimitationTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    var result = MessageBox.Show(string.Format((string)Application.Current.Resources["AdminPanelConfirmDeleteUser"], user.Login),
                        (string)Application.Current.Resources["DeleteConfirmTitle"], MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        IsLoading = true;
                        var freshUser = _userService.GetById(user.Id);
                        if (freshUser == null)
                        {
                            MessageBox.Show(string.Format((string)Application.Current.Resources["AdminPanelUserNotFound"], user.Id),
                                (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                            IsLoading = false;
                            return;
                        }
                        
                        bool success = _userService.Delete(freshUser.Id);
                        
                        if (success)
                        {
                            MessageBox.Show((string)Application.Current.Resources["AdminPanelUserDeleted"],
                                (string)Application.Current.Resources["AdminPanelSuccess"], MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            LoadUsersData();
                        }
                        else
                        {
                            MessageBox.Show((string)Application.Current.Resources["AdminPanelDeleteUserError"],
                                (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                            
                            LoadUsersData();
                        }
                        IsLoading = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(string)Application.Current.Resources["AdminPanelDeleteUserError"]}: {ex.Message}",
                    (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                
                LoadUsersData();
            }
        }

        private void ExecuteAddUserCommand(object parameter)
        {
            try
            {
                var addUserDialog = new View.AddUserDialog();
                addUserDialog.Owner = Application.Current.MainWindow;

                if (addUserDialog.ShowDialog() == true && addUserDialog.NewUser != null)
                {
                    IsLoading = true;
                    User u = addUserDialog.NewUser;

                    // 1. ПРОВЕРКА УНИКАЛЬНОСТИ ЛОГИНА В БАЗЕ (Твой код)
                    if (!_userService.IsLoginUnique(u.Login))
                    {
                        MessageBox.Show((string)Application.Current.Resources["LoginAlreadyTaken"], "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        IsLoading = false; return;
                    }

                    // === ВАЖНЫЙ ШАГ: ХЕШИРОВАНИЕ ===
                    // Хешируем пароль перед сохранением в БД
                    u.Password = WPF_FitnessClub.Data.PasswordHasher.HashPassword(u.Password);

                    // 2. ДОБАВЛЕНИЕ В БД
                    int userId = _userService.Add(u);
                    if (userId > 0)
                    {
                        // Очистка кэша EF, чтобы в таблице сразу появились верные данные
                        _context.ChangeTracker.Entries().ToList().ForEach(e => e.State = EntityState.Detached);

                        LoadUsersData(); // Обновляем таблицу
                        MessageBox.Show((string)Application.Current.Resources["AdminPanelUserAdded"], (string)Application.Current.Resources["AdminPanelSuccess"]);
                    }
                    IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении пользователя: " + ex.Message);
                IsLoading = false;
            }
        }

        #endregion

        #region Вспомогательные методы

        private void LoadUsersData()
        {
            try
            {
                var users = _userService.GetAll();
                
                if (users == null || users.Count == 0)
                {
                    MessageBox.Show((string)Application.Current.Resources["AdminPanelNoUsersFound"],
                        (string)Application.Current.Resources["AdminPanelWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                    UsersTable = new ObservableCollection<User>();
                    return;
                }
                
                foreach (var user in users)
                {
                    if (user == null)
                    {
                        continue;
                    }
                    
                    if (!Enum.IsDefined(typeof(UserRole), user.Role))
                    {
                        user.Role = UserRole.Client;
                    }
                }
                
                UsersTable = new ObservableCollection<User>(users);
                
                OnPropertyChanged(nameof(UsersTable));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(string)Application.Current.Resources["AdminPanelLoadUsersError"]}: {ex.Message}",
                    (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                UsersTable = new ObservableCollection<User>();
            }
        }


        public bool IsLoginUnique(string login)
        {
            return _userService.IsLoginUnique(login);
        }


        public bool IsEmailUnique(string email)
        {
            return _userService.IsEmailUnique(email);
        }

        public bool SaveUserChanges(User user)
        {
            try
            {
                _context.ChangeTracker.Entries()
                    .Where(e => e.State != EntityState.Detached)
                    .ToList()
                    .ForEach(e => e.State = EntityState.Detached);
                
                var freshUser = _userService.GetById(user.Id);
                if (freshUser == null)
                {
                    MessageBox.Show(string.Format((string)Application.Current.Resources["AdminPanelUserNotFound"], user.Id),
                        (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                
                if (user.Id == 1)
                {
                    if (freshUser.Login != user.Login)
                    {
                        MessageBox.Show((string)Application.Current.Resources["AdminPanelDeleteAdminError"],
                            (string)Application.Current.Resources["AdminPanelLimitationTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                        
                        user.Login = freshUser.Login;
                    }
                    
                    if (freshUser.Role != user.Role)
                    {
                        MessageBox.Show((string)Application.Current.Resources["AdminPanelDeleteAdminError"],
                            (string)Application.Current.Resources["AdminPanelLimitationTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                        user.Role = freshUser.Role;
                    }
                    
                    if (user.IsBlocked)
                    {
                        MessageBox.Show((string)Application.Current.Resources["AdminPanelBlockAdminError"],
                            (string)Application.Current.Resources["AdminPanelLimitationTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                        user.IsBlocked = false;
                    }
                }

                freshUser.FullName = user.FullName;
                freshUser.Email = user.Email;
                freshUser.Login = user.Login;
                freshUser.Role = user.Role;
                freshUser.IsBlocked = user.IsBlocked;

                bool result = _userService.Update(freshUser);
                
                if (result)
                {
                    int index = UsersTable.IndexOf(user);
                    if (index >= 0)
                    {
                        UsersTable[index] = freshUser;
                    }
                    
                    OnPropertyChanged(nameof(UsersTable));
                }
                else
                {
                    MessageBox.Show((string)Application.Current.Resources["AdminPanelUpdateUserError"],
                        (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(string)Application.Current.Resources["AdminPanelUpdateUserError"]}: {ex.Message}",
                    (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public User GetCurrentUser()
        {
            return _userService.GetCurrentUser();
        }

        #endregion
    }
} 