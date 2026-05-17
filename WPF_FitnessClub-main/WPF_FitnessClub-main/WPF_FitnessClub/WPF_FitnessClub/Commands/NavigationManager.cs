using System;
using System.Windows;
using System.Windows.Controls;

namespace WPF_FitnessClub
{
    public class NavigationManager
    {
        private static NavigationManager _instance;
        private ContentControl _contentControl;
        private UserControl _currentView;

        public static NavigationManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new NavigationManager();
                return _instance;
            }
        }

        public UserControl CurrentView => _currentView;

        public void Initialize(ContentControl contentControl)
        {
            _contentControl = contentControl;
        }

        public void NavigateTo(UserControl content)
        {
            if (_contentControl == null)
                throw new InvalidOperationException("Менеджер навигации не инициализирован. Вызовите метод Initialize.");

            _contentControl.Content = content;
            _currentView = content;
            OnContentChanged(content);
        }

        public event EventHandler<object> ContentChanged;

        protected virtual void OnContentChanged(object content)
        {
            ContentChanged?.Invoke(this, content);
        }
    }
} 