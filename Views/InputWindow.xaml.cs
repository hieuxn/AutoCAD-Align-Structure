using AutoCAD_Align_Structure.ViewModels;
using System.Windows;

namespace AutoCAD_Align_Structure.Views
{
	public partial class InputWindow : Window
	{

		public InputViewModel ViewModel { get; set; }
		public InputWindow()
		{
			InitializeComponent();
			DataContext = ViewModel = new InputViewModel();
		}
	}
}
