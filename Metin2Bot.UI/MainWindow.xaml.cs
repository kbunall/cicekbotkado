using Metin2Bot.Infrastructure.Services;
using Metin2Bot.UI.ViewModels;
using System.Windows;

namespace Metin2Bot.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Dependency Injection simülasyonu (Gerçek projede bir DI Container kullanılmalı)
            var visionService = new VisionService();
            var inputService = new InputService();
            var windowService = new WindowService();

            DataContext = new MainViewModel(visionService, inputService, windowService);
        }
    }
}