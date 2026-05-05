using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;

namespace Metin2Bot.UI.ViewModels
{
    // Kuyrukta (Channel) taşınacak verilerin modelleri (Record)
    public record ScanCommand(WindowInfo Window);
    public record ClickCommand(WindowInfo Window, int X, int Y, string FileName);

    public partial class MainViewModel : ObservableObject
    {
        private readonly IVisionService _visionService;
        private readonly IInputService _inputService;
        private readonly IWindowService _windowService;
        private CancellationTokenSource? _cts;

        // --- KUYRUKLAR (CHANNELS) ---
        // Sadece 1 adet tarama ve 1 adet tıklama kuyruğu oluşturuyoruz. Kapasitelerini 10 ile sınırlıyoruz ki RAM şişmesin.
        private Channel<ScanCommand> _scanChannel;
        private Channel<ClickCommand> _clickChannel;

        [ObservableProperty]
        private ObservableCollection<WindowInfo> _windows = new();

        [ObservableProperty]
        private ObservableCollection<string> _logs = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartBotCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopBotCommand))]
        private bool _isRunning;

        public MainViewModel(IVisionService visionService, IInputService inputService, IWindowService windowService)
        {
            _visionService = visionService;
            _inputService = inputService;
            _windowService = windowService;
            RefreshWindows();
        }

        [RelayCommand]
        private void RefreshWindows()
        {
            Windows.Clear();
            foreach (var win in _windowService.GetActiveWindows())
            {
                Windows.Add(win);
            }
            AddLog($"{Windows.Count} adet Metin2 penceresi bulundu ve listeye eklendi.");
        }

        [RelayCommand(CanExecute = nameof(CanStart))]
        private async Task StartBotAsync()
        {
            if (Windows.Count == 0)
            {
                AddLog("Açık oyun penceresi yok!");
                return;
            }

            IsRunning = true;
            _cts = new CancellationTokenSource();
            
            // Kuyrukları başlat
            _scanChannel = Channel.CreateBounded<ScanCommand>(10);
            _clickChannel = Channel.CreateBounded<ClickCommand>(10);

            AddLog($"Çoklu Bot Başlatıldı. Toplam {Windows.Count} pencere taranacak.");

            try
            {
                // Fabrika Bant Sistemini (Görevleri) Paralel Olarak Başlatıyoruz
                var producerTask = Task.Run(() => ProducerLoop(_cts.Token), _cts.Token);
                var visionTask = Task.Run(() => VisionConsumerLoop(_cts.Token), _cts.Token);
                var clickTask = Task.Run(() => ClickConsumerLoop(_cts.Token), _cts.Token);

                // Üç görevden biri iptal edilene kadar bekle
                await Task.WhenAll(producerTask, visionTask, clickTask);
            }
            catch (OperationCanceledException)
            {
                AddLog("Bot döngüsü güvenli bir şekilde sonlandırıldı.");
            }
            catch (Exception ex)
            {
                AddLog($"Kritik Hata: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private bool CanStart() => !IsRunning;

        [RelayCommand(CanExecute = nameof(IsRunning))]
        private void StopBot()
        {
            AddLog("Durdurma isteği gönderildi...");
            _cts?.Cancel();
        }

        // 1. ÜRETİCİ (PRODUCER): Sürekli açık pencereleri dolaşıp "Bunu Tara" emrini kuyruğa atar
        private async Task ProducerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var win in Windows)
                {
                    if (token.IsCancellationRequested) break;
                    
                    // Tarama kuyruğuna pencereyi at
                    await _scanChannel.Writer.WriteAsync(new ScanCommand(win), token);
                }
                
                // CPU'yu yormamak için tüm pencereleri turladıktan sonra 1 saniye dinlen
                await Task.Delay(1000, token);
            }
        }

        // 2. TÜKETİCİ (BEYİN): Tarama kuyruğundan pencereyi alır, çiçekleri arar.
        private async Task VisionConsumerLoop(CancellationToken token)
        {
            // Şablonları (fotoğrafları) döngü dışında bir kez yükleyerek performansı artırıyoruz
            string[] templates = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.png")
                                          .Where(f => !f.EndsWith("last_capture.png"))
                                          .ToArray();

            await foreach (var command in _scanChannel.Reader.ReadAllAsync(token))
            {
                var winRect = _windowService.GetWindowRect(command.Window.Handle);
                if (winRect.Width <= 0 || winRect.Height <= 0) continue;

                var region = new Domain.Models.Region(winRect.X, winRect.Y, winRect.Width, winRect.Height);

                foreach (var templatePath in templates)
                {
                    if (token.IsCancellationRequested) break;

                    string fileName = Path.GetFileName(templatePath);
                    double threshold = 0.55; // Gerekirse bunu 0.45 yapabilirsin
                    
                    var result = _visionService.FindTemplate(templatePath, region, threshold);

                    if (result.HasValue && result.Value.Confidence >= threshold)
                    {
                        // Ekranda çiçek bulundu! Tıklama görevini (koordinatları) Tıklama Kuyruğuna at ve diğer şablonlara bakmayı bırak.
                        await _clickChannel.Writer.WriteAsync(new ClickCommand(command.Window, result.Value.Location.X, result.Value.Location.Y, fileName), token);
                        break; 
                    }
                }
            }
        }

        // 3. TÜKETİCİ (EL): Tıklama kuyruğundan emirleri alır ve sırayla güvenli şekilde tıklar
        private async Task ClickConsumerLoop(CancellationToken token)
        {
            await foreach (var command in _clickChannel.Reader.ReadAllAsync(token))
            {
                AddLog($"[EŞLEŞME - {command.Window.Title}] {command.FileName} bulundu! Tıklanıyor...");

                // Kilit (lock) mimarisiyle yazdığımız Ghost Mouse metodu çalışıyor
                _inputService.BackgroundClick(command.Window.Handle, command.X, command.Y);

                // Karaktere koşması ve çiçeği toplaması için diğer tıklamaları 2 saniye beklet
                await Task.Delay(2000, token);
            }
        }

        // UI thread güvenliğini burada tek merkezden sağlıyoruz
        private void AddLog(string message)
        {
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                Logs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
                if (Logs.Count > 100) Logs.RemoveAt(100);
            });
        }
    }
}