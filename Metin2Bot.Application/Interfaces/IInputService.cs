using System;

namespace Metin2Bot.Application.Interfaces
{
    public interface IInputService
    {
        /// <summary>
        /// Belirtilen pencere başlığına sahip pencerenin handle (IntPtr) değerini bulur.
        /// </summary>
        /// <param name="windowTitle">Pencere başlığı.</param>
        /// <returns>Pencere handle değeri.</returns>
        IntPtr FindWindow(string windowTitle);

        /// <summary>
        /// Belirtilen koordinatlara arka planda sol tıklama yapar.
        /// </summary>
        /// <param name="handle">Pencere handle değeri.</param>
        /// <param name="x">X koordinatı.</param>
        /// <param name="y">Y koordinatı.</param>
        void BackgroundClick(IntPtr handle, int x, int y);

        /// <summary>
        /// Belirtilen tuşu arka planda gönderir.
        /// </summary>
        /// <param name="handle">Pencere handle değeri.</param>
        /// <param name="keyCode">Tuş kodu (Virtual Key Code).</param>
        void BackgroundKeyPress(IntPtr handle, int keyCode);
    }
}
