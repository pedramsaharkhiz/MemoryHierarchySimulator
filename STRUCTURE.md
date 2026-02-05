## نمای کلی ساختار پروژه

- **`src/Program.cs`**  
  نقطه ورود برنامه. برنامه Avalonia (`App`) را پیکربندی و اجرا می‌کند (تشخیص پلتفرم، لاگ‌گیری) و چرخه عمر دسکتاپ کلاسیک را شروع می‌کند.

- **`src/App.axaml` / `src/App.axaml.cs`**  
  کلاس `Application` در Avalonia، استایل‌های سراسری، تم و استایل‌های DataGrid را تعریف می‌کند. مسئول راه‌اندازی درخت視و و تنظیم منابع مشترک بین تمام پنجره‌ها است.

- **`src/Views/MainWindow.axaml` / `src/Views/MainWindow.axaml.cs`**  
  پنجره اصلی رابط کاربری شبیه‌ساز.  
  - XAML پنل تنظیمات (پارامترهای کش‌های L1/L2/L3، گزینه‌های شبیه‌سازی، دکمه‌های اجرا/مقایسه/ریست)، داشبورد نتایج (کارت‌ها + نمودار اصلی)، جدول جزئیات (DataGrid) و تب گزارش/مقایسه (نمودار مقایسه، لاگ‌ها و توضیح سیاست) را می‌چیند.  
  - کد پشت‌صحنه فقط ویو را مقداردهی اولیه می‌کند؛ تمام منطق در ViewModel و سرویس‌ها قرار دارد.

- **`src/ViewModels/MainViewModel.cs`**  
  منطق ارائه (Presentation) برای UI که با CommunityToolkit.Mvvm پیاده‌سازی شده است.  
  - پراپرتی‌های بایندشدنی برای تمام پارامترهای کش، گزینه‌های شبیه‌سازی، آمار، سری‌ها و محورهای نمودارها، داده نمودار مقایسه، لاگ‌ها و نتایج را در اختیار ویو می‌گذارد.  
  - دستورات **Run**، **Compare** و **Reset** را پیاده‌سازی می‌کند؛ شبیه‌سازی را از طریق `SimulatorService` اجرا می‌کند، `Results` و `Logs` را پر می‌کند، آمار تجمیعی را محاسبه می‌کند و داده مناسب برای LiveCharts تولید می‌کند.

- **`src/Models/Models.cs`**  
  مدل‌های دامنه و داده برای سلسله‌مراتب حافظه.  
  - ساختارهای کش (`CacheBlock`، `CacheSet`)، پیکربندی (`CacheLevelConfig`) و enum‌ها (`ReplacementPolicy`، `AccessType`، `AccessPattern`).  
  - انواع داده شبیه‌سازی (`MemoryAccess`، `AccessResult`) و کانتینرهای آمار (`SimulationStatistics`، `CacheLevelStatistics`) که توسط سرویس‌ها و ViewModel استفاده می‌شوند.

- **`src/Services/Services.cs`**  
  موتور اصلی شبیه‌ساز.  
  - `CacheLevel` رفتار یک سطح کش را پیاده‌سازی می‌کند (ست‌ها، بلوک‌ها، استراتژی جایگزینی، شمارش Hit/Miss).  
  - `MainMemory` و `SecondaryStorage` دسترسی‌ها و تأخیرها در سطوح عمیق‌تر حافظه را ردیابی می‌کنند.  
  - `MemoryAccessGenerator` الگوهای مختلف دسترسی (Sequential، Random، Locality، Stride، Loop، Mixed) را تولید می‌کند.  
  - `SimulatorService` همه چیز را به هم وصل می‌کند: دسترسی‌ها را از میان سطوح کش عبور می‌دهد، آمار را به‌روز می‌کند، لیست `AccessResult` برمی‌گرداند و یک خلاصه متنی قابل خواندن تولید می‌کند.

- **`src/Services/ReplacementStrategies.cs`**  
  تمام پیاده‌سازی‌های سیاست‌های جایگزینی کش و کارخانه ساخت آن‌ها.  
  - استراتژی‌های مشخص (`LruStrategy`، `FifoStrategy`، `RandomStrategy`، `LfuStrategy`، `MruStrategy`، `RoundRobinStrategy`، `SecondChanceStrategy`، `LfruStrategy`) واسط `IReplacementStrategy` را پیاده‌سازی کرده و منطق انتخاب Victim و به‌روزرسانی وضعیت در هر دسترسی را کپسوله می‌کنند.  
  - `ReplacementStrategyFactory` مقادیر enum نوع `ReplacementPolicy` را به نمونه استراتژی متناظر نگاشت می‌کند و توضیح‌های کوتاهی فراهم می‌کند که در UI نمایش داده می‌شود.

- **`src/MemoryHierarchySimulator.csproj`**  
  فایل پروژه .NET. فریم‌ورک هدف، وابستگی‌های Avalonia و LiveCharts، تنظیمات بیلد (از جمله غیرفعال کردن پیش‌فرض compiled bindings) و رفرنس پکیج DataGrid را تعریف می‌کند.

- **`src/app.manifest`**  
  مانيفست برنامه برای پیکربندی‌های مخصوص پلتفرم (پنجره، متادیتای زمان اجرا و غیره).
