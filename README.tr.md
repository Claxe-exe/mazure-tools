<div align="center">

<img src="Resources/mazure-logo.png" width="112" alt="Mazure Tools logosu" />

# Mazure Tools

**Windows sistem ve ağ araçları, tek bir küçük ve hızlı uygulamada.**
*Yapımcı: Claxe*

[English](README.md) · Türkçe

</div>

Mazure Tools, Windows'ta normalde farklı yerlere dağılmış araçları — görev yöneticisi, `ipconfig`, `ping`, disk kullanımı, hash hesaplama, port kontrolü — tek bir modern, koyu temalı pencerede toplar. Taşınabilirdir (tek `.exe`, kurulum yok), **üçüncü parti paket kullanmaz**, **telemetri göndermez** ve sistemde bir şeyi değiştirmeden önce ne yapacağını gösterip her zaman sorar.

C# · .NET 10 (LTS) · WPF · MVVM · Windows 10 (1809+) / 11 · English ve Türkçe

<p align="center">
  <img src="docs/screenshots/dashboard-dark-tr.png" width="46%" alt="Gösterge paneli" />
  <img src="docs/screenshots/dashboard-light-tr.png" width="46%" alt="Açık tema" />
</p>
<p align="center">
  <img src="docs/screenshots/dashboard-dark-en.png" width="46%" alt="Dashboard (İngilizce)" />
  <img src="docs/screenshots/processes-dark-en.png" width="46%" alt="İşlem yöneticisi (İngilizce)" />
</p>

## Özellikler

| Sayfa | Ne yapar |
|---|---|
| **Gösterge Paneli** | Canlı CPU / RAM / GPU kullanımı, disk kullanımı, Windows sürümü, CPU/GPU modeli, ağ durumu |
| **Sistem** | CPU modeli ve yükü, RAM toplam/kullanılan/boş, GPU adı ve yükü, diskler, Windows sürümü, çalışma süresi |
| **Ağ** | Ping (en düşük/ortalama/en yüksek gecikme, paket kaybı), DNS, ağ geçidi, yerel IP, genel IP, bağdaştırıcı bilgileri, DNS önbelleğini temizle, IP yenile |
| **İşlemler** | İşlem listesi (ad, PID, CPU, RAM), arama, sonlandır, yeniden başlat; kritik Windows işlemleri için koruma |
| **Depolama** | Disk adı, kapasite, kullanılan, boş, yüzde; seçilen klasörün boyutu |
| **Araçlar** | SHA-256 / SHA-1 / MD5 (dosya ve metin, beklenen hash ile karşılaştırma), port denetleyici, CMD / PowerShell / Gezgin aç |
| **Ayarlar** | **Dil (English / Türkçe, anında uygulanır)**, koyu/açık tema, tepsi davranışı, Windows ile başlat, maskotu göster/gizle, yönetici olarak yeniden başlat |

**Sistem tepsisi:** Mazure Tools'u Aç · Gösterge Paneli · Ağ · Çıkış. Pencereyi kapatınca tepsiye küçülür (kapatılabilir).

## Mazu ile tanış

Mazu, ekranın sağ altında her zaman üstte duran küçük mavi bir slime. Bilgisayarının durumuna tepki verir:

<p align="center"><img src="docs/screenshots/mazu-moods.png" alt="Mazu: mutlu, yorgun, endişeli, uykulu, şaşkın" /></p>

| Hal | Ne zaman |
|---|---|
| Mutlu | her şey sakin |
| Yorgun | CPU yaklaşık 10 saniyedir %90 üstünde |
| Endişeli | bellek %92 üstünde ya da bir sabit diskte %10'dan az yer kaldı |
| Şaşkın | ağ bağlantısı gitti |
| Uykulu | 3 dakikadır klavye/fare yok (döndüğünde uyanır) |

Mazu kısa, biraz salak konuşma balonlarıyla konuşur (Türkçe ve İngilizce, her hal için birkaç replik). **Tıkla** sohbet etsin (5 kez hızlı dürtersen gıdıklanır), istediğin yere **sürükle** (konum hatırlanır), **sağ tıkla** *Mazure Tools'u Aç* / *Mazu'yu gizle*. **Ayarlar → Mazu'yu göster** ile kapatabilirsin.

Maliyet: görünürken bir CPU çekirdeğinin yaklaşık %0,2'si ve ~8 MB RAM (CPU ve RAM saniyede bir örneklenir, GPU sayaçları kapalı kalır). Mazu kapalıyken tepsideki uygulama hiç CPU kullanmaz.

## İndirme ve kurulum

Kurulacak hiçbir şey yok, .NET bile.

1. **[Releases](../../releases)** sayfasından `MazureTools-win-x64.zip` dosyasını indirin (ARM64 bilgisayarlar için `win-arm64`).
2. Bir klasöre çıkarın (örn. `C:\Tools\MazureTools`).
3. `MazureTools.exe`'yi çalıştırın.

Notlar
- Exe imzalı değildir; Windows SmartScreen ilk açılışta uyarabilir → **Diğer bilgiler → Yine de çalıştır**. Zip'i internetten indirdiyseniz çıkarmadan önce zip'e sağ tık → Özellikler → **Engellemeyi kaldır**'ı işaretleyin. Emin olmak için zip'i sürümdeki `SHA256SUMS.txt` ile karşılaştırabilirsiniz.
- İlk açılışta WPF'nin yerel kütüphaneleri bir kez geçici klasöre açılır; birkaç saniye daha uzun sürer.
- Ayarlar `%AppData%\MazureTools\settings.json` içinde saklanır. Kaldırmak için exe'yi ve bu klasörü silin (kullandıysanız önce *Windows ile başlat*'ı kapatın).
- ARM64 sürümü derleniyor ancak ARM donanımda denenmedi.

İlk açılışta Türkçe Windows'ta **Türkçe**, diğerlerinde **English** seçilir; **Ayarlar → Dil**'den istediğiniz zaman değiştirebilirsiniz.

## Kaynaktan derleme

Yalnızca derleme yapan bilgisayarda [.NET 10 SDK](https://dotnet.microsoft.com/download) gerekir:

```powershell
winget install Microsoft.DotNet.SDK.10

dotnet build            # derle
dotnet run              # çalıştır
```

Kendi taşınabilir paketinizi üretin (çıktı `dist\` içine):

```powershell
.\publish.ps1                                  # tek dosya, .NET dahil (~170 MB, zip ~66 MB)
.\publish.ps1 -Mode FrameworkDependent         # birkaç MB, hedefte ".NET Desktop Runtime 10" gerekir
.\publish.ps1 -Runtime win-arm64               # ARM64
```

`Build-Portable.cmd` varsayılan derlemeyi çift tıkla yapar. İsterseniz `MazureTools.sln`'yi Visual Studio'da açın.

Pull request açmadan önce sözlük denetimini çalıştırın:

```powershell
.\tools\Test-Localization.ps1
```

## Güvenlik

Mazure Tools normal kullanıcı olarak başlar ve **hiçbir zaman sessizce yetki yükseltmez**.

- **IP yenileme** (`ipconfig /renew`): ne yapacağını, bağlantının kısa süre kopabileceğini gösterir ve onay ister. Yönetici gerekir; değilseniz *Yönetici olarak yeniden başlat* önerilir (Windows'un kendi UAC penceresi çıkar; reddederseniz hiçbir şey değişmez).
- **DNS önbelleğini temizle**: önce sorar; yetki yükseltmeyi yalnızca Windows isterse önerir.
- **İşlem sonlandırma / yeniden başlatma** her zaman onay ister.
  - Kritik işlemler (`System`, `csrss`, `wininit`, `winlogon`, `lsass`, `smss`, `services`, `Registry`, …) **engellenir**; sonlandırmak Windows'u çökertir veya oturumu kapatır.
  - Windows bileşenleri (`svchost`, `explorer`, `dwm`, `audiodg`, `spoolsv`, Defender, …) için ek kırmızı uyarı gösterilir ve buradan yeniden başlatılamazlar.
  - Uygulama kendini bu ekrandan kapatamaz; işlem yalnızca seçilen PID hâlâ aynı programa aitse yapılır (PID yeniden kullanımı koruması).
  - Koruma, yanlış tıklamaya karşı **ad tabanlı bir güvenlik ağıdır**; bir zararlı yazılımın adı taklit etmesine karşı güvenlik sınırı değildir.
- **Genel IP** yalnızca *Sorgula*'ya bastığınızda öğrenilir (`api.ipify.org`, yedek `icanhazip.com`). Bunun dışında uygulama internete bağlanmaz.
- Sistem araçları kabuk kullanılmadan doğrudan `System32`'den başlatılır; kullanıcı girdisi komut satırına birleştirilmez.
- CMD / PowerShell / Gezgin, Mazure Tools ile **aynı yetkiyle** açılır.

## Tasarım gereği hafif

- CPU/RAM/GPU örneklemesi (1 sn) **yalnızca** Gösterge Paneli veya Sistem sayfası görünürken çalışır; işlem listesi (2 sn) yalnızca İşlemler sayfası açıkken yenilenir.
- Pencere tepsiye küçüldüğünde veya simge durumuna alındığında tüm sayfa zamanlayıcıları durur (ölçüm: maskot kapalıyken 20 sn'de 0 ms CPU; Mazu görünürken bir çekirdeğin ~%0,2'si).
- Ağ değişiklikleri Windows olaylarıyla yakalanır, sorgulanmaz.
- Arayüz yazılım render ile çizilir (GPU sürücüsünün Direct3D yığınını yüklemez; ~60 MB daha az RAM, aynı CPU). Donanım render için `MAZURE_HW_RENDER=1`.
- Yazarın bilgisayarında ölçülen: WPF ve .NET dahil ~100 MB RAM; pencere yaklaşık 1 sn'de açılır.

## Mimari

```
Core/         MVVM altyapısı (ObservableObject, komutlar, PageViewModel), Loc (yerelleştirme), yardımcılar, Native/ (P/Invoke)
Services/     Uygulama geneli servisler: dialog, ayarlar, tema, gezinme, yükseltme, tepsi, pencere, tek örnek
ViewModels/   Shell ViewModel'leri: Main, Dashboard, Settings
Views/        Ana pencere, Dashboard/Settings görünümleri, mesaj penceresi, ortak kontroller ve şablonlar
Modules/      Her araç bağımsız modül: servis arayüzü + uygulama + modeller + ViewModel + View
  SystemInfo/   ("System" değil: C# ad alanı `System` ile çakışırdı)
  Network/  Processes/  Storage/  Utilities/
Languages/    Strings.English.xaml, Strings.Turkish.xaml
Themes/       Colors.Dark/Light.xaml (canlı değişir), Styles.xaml
Resources/    İkon, logo ve maskot görselleri
tools/        İkon üretici, yerelleştirme denetimi
```

Kurallar:
- **UI sistem işi yapmaz.** View → ViewModel → servis arayüzü (`INetworkService`, `IProcessService`, …). Ağ sayfasındaki düğme komut çalıştırmaz; `NetworkService` çalıştırır.
- Sorular/mesajlar `IDialogService` ile; ViewModel'ler pencere bilmez.
- Her şey `App.xaml.cs` içindeki tek bir *composition root*'ta bağlanır (DI kapsayıcı paketi gerekmedi).
- İstisnalar anlaşılır mesaja çevrilir (`ErrorMessages`); komutlarda yakalanmayan hata uygulamayı düşürmez.
- **Yeni araç eklemek:** `Modules/YeniArac/` altında servis + `PageViewModel` + `UserControl` yazın, `PageId`'ye ekleyin, `Views/Templates.xaml`'de ViewModel→View eşlemesini yapın, `App.xaml.cs`'e kaydedin ve metinlerini iki dil dosyasına da ekleyin.

### Çeviri / yeni dil ekleme

Tüm metinler `Languages/Strings.*.xaml` içindedir ve anahtarla okunur (XAML'de `{DynamicResource Anahtar}`, kodda `Loc.T("Anahtar")`), bu yüzden dil anında değişir. Yeni dil için `Strings.English.xaml`'i kopyalayıp değerleri çevirin, `Core/Loc.cs` içindeki `AppLanguage`'a dili ekleyin ve `Views/SettingsView.xaml`'e bir düğme koyun. `tools/Test-Localization.ps1` anahtarların ve `{0}` yer tutucularının eşleştiğini denetler.

## Bilinen sınırlar ve yapılacaklar

Bunlar **henüz yok** ve hazırmış gibi gösterilmez. [Yol haritasına](ROADMAP.md) da bakın.

- [ ] DNS sunucusunu *değiştirme* (yalnızca DNS bilgisi gösterilir ve önbellek temizlenir)
- [ ] Windows hizmetleri yönetimi
- [ ] İşlem yeniden başlatılırken komut satırı argümanları korunmaz (onay penceresinde belirtilir)
- [ ] İşlem ayrıntıları (yol, kullanıcı, iş parçacığı, komut satırı)
- GPU yüzdesi Görev Yöneticisi'nin kullandığı "GPU Engine" sayaçlarından hesaplanır ve en yoğun motoru gösterir; Windows sayaç sunmuyorsa (eski sürücü, bazı VM'ler) **N/A** yazar. Uydurma değer gösterilmez.
- "Bağlı" durumu Windows'un bağlantı bilgisidir; internete erişimi ayrıca sınamaz.
- Klasör boyutu dosya boyutlarını toplar (diskte ayrılan alan değil), sembolik bağlantıları izlemez, Windows'un reddettiklerini atlar.
- Tepsi bağlam menüsü Windows'un varsayılan (açık) görünümündedir.

## Katkı

Sorunlar ve pull request'ler memnuniyetle karşılanır — bkz. [CONTRIBUTING.md](CONTRIBUTING.md).

## Lisans

[MIT](LICENSE) © 2026 Claxe. Mazu ve logo bu proje için yapılmış özgün çizimlerdir ve aynı lisans kapsamındadır.
