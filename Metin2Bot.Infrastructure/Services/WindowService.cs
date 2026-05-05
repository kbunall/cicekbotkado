using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;

namespace Metin2Bot.Infrastructure.Services
{
    public class WindowService : IWindowService
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public IEnumerable<WindowInfo> GetActiveWindows()
        {
            var windows = new List<WindowInfo>();

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder sb = new StringBuilder(256);
                    GetWindowText(hWnd, sb, 256);
                    string title = sb.ToString();

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        windows.Add(new WindowInfo { Handle = hWnd, Title = title });
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public Rectangle GetWindowRect(IntPtr handle)
        {
            if (GetWindowRect(handle, out RECT rect))
            {
                return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            return Rectangle.Empty;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    }
}
