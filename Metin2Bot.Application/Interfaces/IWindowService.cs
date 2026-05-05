using Metin2Bot.Domain.Models;
using System.Collections.Generic;

namespace Metin2Bot.Application.Interfaces
{
    public interface IWindowService
    {
        IEnumerable<WindowInfo> GetActiveWindows();
        System.Drawing.Rectangle GetWindowRect(IntPtr handle);
    }
}
