using Metin2Bot.Infrastructure.Services;
using Metin2Bot.Domain.Models;
using System.Drawing;
using System.Drawing.Imaging;

namespace Metin2Bot.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("   Metin2Bot Vision Service Demo");
            Console.WriteLine("========================================");

            string templatePath = "cicek.png";

            // Test için bir cicek.png yoksa geçici bir tane oluşturalım
            if (!File.Exists(templatePath))
            {
                Console.WriteLine("[!] 'cicek.png' bulunamadı. Test için kırmızı bir kare oluşturuluyor...");
                using (Bitmap bmp = new Bitmap(50, 50))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Red);
                    }
                    bmp.Save(templatePath, ImageFormat.Png);
                }
                Console.WriteLine("[+] 'cicek.png' (kırmızı kare) oluşturuldu.");
            }

            var visionService = new VisionService();
            // Tüm ekranı kapsayan bir bölge tanımlayalım (Genelde 1920x1080 varsayıyoruz)
            var screenRegion = new Metin2Bot.Domain.Models.Region(0, 0, 1920, 1080);

            Console.WriteLine("\n[.] Ekran taranıyor... Lütfen ekranda kırmızı bir alan bulunduğundan emin olun.");
            Console.WriteLine("[.] Not: Kırmızı bir görseli (örneğin Paint'te) açıp ekranda gösterirseniz bot bulacaktır.");
            Console.WriteLine("[.] 10 saniye boyunca deneme yapılacak...\n");

            for (int i = 1; i <= 10; i++)
            {
                try
                {
                    var result = visionService.FindTemplate(templatePath, screenRegion, 0.85);

                    if (result.HasValue)
                    {
                        Console.WriteLine($"\a[MATCH] BULDUM! Koordinatlar: X={result.Value.Location.X}, Y={result.Value.Location.Y}");
                        Console.WriteLine("Başarıyla tamamlandı.");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"[-] Deneme {i}: Bulunamadı...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Bir hata oluştu: {ex.Message}");
                }

                Thread.Sleep(1000);
            }

            Console.WriteLine("\n[!] Süre doldu, şablon bulunamadı.");
        }
    }
}
