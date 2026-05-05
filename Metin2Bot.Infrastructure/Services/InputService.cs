using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using Metin2Bot.Application.Interfaces;

namespace Metin2Bot.Infrastructure.Services
{
    public class InputService : IInputService
    {
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;

        private readonly Random _random = new Random();
        
        // Çoklu ekranda (multi-client) farklı oyunların fareyi aynı anda kapışmasını engellemek için kilit
        private static readonly object _mouseLock = new object();

        public IntPtr FindWindow(string windowTitle) => IntPtr.Zero;

        public void BackgroundClick(IntPtr handle, int x, int y)
        {
            if (handle == IntPtr.Zero) return;

            // Lock kullanarak işlemleri sıraya sokuyoruz (Senkronizasyon)
            lock (_mouseLock)
            {
                // 1. Kullanıcının gerçek farenin o anki konumunu kaydet
                GetCursorPos(out Point originalPos);

                // 2. Hedefe ufak bir sapma ekle (VisionService doğrudan ekran koordinatı veriyor)
                int targetX = x + _random.Next(-2, 3);
                int targetY = y + _random.Next(-2, 3);

                // 3. PostMessage için Client koordinatlarına çevir
                Point clientPoint = new Point(targetX, targetY);
                ScreenToClient(handle, ref clientPoint);
                int lParam = (clientPoint.Y << 16) | (clientPoint.X & 0xFFFF);

                // 4. Oyun gerçek fareyi kontrol ettiği için, fareyi anlık olarak çiçeğin üstüne ışınla
                SetCursorPos(targetX, targetY);
                
                // Oyunun motorunun farenin yeni yerine geldiğini algılaması için çok kısa bekle (1 Frame)
                Thread.Sleep(20);

                // 5. Tıklamayı gönder
                PostMessage(handle, WM_LBUTTONDOWN, 1, lParam);
                Thread.Sleep(_random.Next(30, 60)); // Basılı tutma süresi
                PostMessage(handle, WM_LBUTTONUP, 0, lParam);

                // 6. Fareyi, kullanıcıyı rahatsız etmemek için anında eski yerine geri koy
                SetCursorPos(originalPos.X, originalPos.Y);
                
                // Kilit açılmadan önce sistemin toparlanması için minik bir nefes
                Thread.Sleep(10);
            }
        }

        public void BackgroundKeyPress(IntPtr handle, int keyCode)
        {
            // İleride eklenecek
        }
    }
}