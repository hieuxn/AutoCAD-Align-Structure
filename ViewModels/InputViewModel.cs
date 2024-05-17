using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCAD_Align_Structure.ViewModels
{
	using AutoCAD_Align_Structure.Models;
	using AutoCAD_Align_Structure.Views;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using System.Windows.Input;

	public class InputViewModel : INotifyPropertyChanged
	{
		private InputModel _model;
		public InputModel GetResult() => _model.Clone();

		public int SpanCount
		{
			get => _model.SpanCount;
			set
			{
				_model.SpanCount = value;
				OnPropertyChanged();
			}
		}

		public double ProtrusionLength
		{
			get => _model.ProtrusionLength;
			set
			{
				_model.ProtrusionLength = value;
				OnPropertyChanged();
			}
		}

		public double EmbedmentLength
		{
			get => _model.EmbedmentLength;
			set
			{
				_model.EmbedmentLength = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		public ICommand OKCommand { get; }
		public ICommand CancelCommand { get; }

		public InputViewModel()
		{
			_model = new();
			OKCommand = new RelayCommand(ExecuteOK);
			CancelCommand = new RelayCommand(ExecuteCancel);
		}

		private void ExecuteOK(object parameter)
		{
			if (parameter is InputWindow window)
			{
				window.DialogResult = true;
				window.Close();
			}
		}

		private void ExecuteCancel(object parameter)
		{
			if (parameter is InputWindow window)
			{
				window.DialogResult = false;
				window.Close();
			}
		}
	}

}
