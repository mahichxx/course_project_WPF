using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub
{
	public class Commands : INotifyPropertyChanged
	{

		public static readonly RoutedUICommand LogoutCommand = new RoutedUICommand(
			"Выйти из системы",
			"Logout",
			typeof(Commands)
		);

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public class RelayCommand : ICommand
		{
			private readonly Action<object> _execute;
			private readonly Func<object, bool> _canExecute;

			public event EventHandler CanExecuteChanged
			{
				add { CommandManager.RequerySuggested += value; }
				remove { CommandManager.RequerySuggested -= value; }
			}

			public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
			{
				_execute = execute ?? throw new ArgumentNullException(nameof(execute));
				_canExecute = canExecute;
			}

			public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
			public void Execute(object parameter) => _execute(parameter);
			public void RaiseCanExecuteChanged()
			{
				CommandManager.InvalidateRequerySuggested();
			}
		}

		public class RelayCommand<T> : ICommand
		{
			private readonly Action<T> _execute;
			private readonly Predicate<T> _canExecute;

			public event EventHandler CanExecuteChanged
			{
				add { CommandManager.RequerySuggested += value; }
				remove { CommandManager.RequerySuggested -= value; }
			}

			public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
			{
				_execute = execute ?? throw new ArgumentNullException(nameof(execute));
				_canExecute = canExecute;
			}

			public bool CanExecute(object parameter)
			{
				return _canExecute == null || _canExecute((T)parameter);
			}

			public void Execute(object parameter)
			{
				_execute((T)parameter);
			}
		}
	}
}