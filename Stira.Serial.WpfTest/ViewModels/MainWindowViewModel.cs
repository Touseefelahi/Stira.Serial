using Prism.Mvvm;

namespace Stira.Serial.WpfTest.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";

        public MainWindowViewModel()
        {
            SerialControlViewModel = new Wpf.SerialControlViewModel();
            SerialControlViewModel.SerialCom.BaudRate = 4800;
        }

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public Wpf.SerialControlViewModel SerialControlViewModel { get; set; }
    }
}