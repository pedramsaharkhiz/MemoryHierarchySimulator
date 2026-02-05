# شبیه‌ساز سلسله مراتب حافظه (Memory Hierarchy Simulator)

## پروژه درس معماری و سازمان کامپیوتر - دانشگاه صنعتی اصفهان

این پروژه با **Avalonia UI** نوشته شده و روی **مک، لینوکس و ویندوز** اجرا می‌شود.

## قابلیت‌ها

- شبیه‌سازی کش L1, L2, L3
- ۸ سیاست جایگزینی: LRU, FIFO, Random, LFU, MRU, RoundRobin, SecondChance, LFRU
- ۶ الگوی دسترسی: Sequential, Random, Locality, Stride, Loop, Mixed
- نمودار Hit/Miss
- مقایسه همه سیاست‌ها

## نیازمندی‌ها

فقط **.NET 8 SDK**:
```
https://dotnet.microsoft.com/download/dotnet/8.0
```

## اجرا

```bash
cd MemoryHierarchySimulator/src
dotnet restore
dotnet run
```

## ساختار

```
src/
├── Models/Models.cs           # مدل‌های داده
├── Services/
│   ├── ReplacementStrategies.cs  # سیاست‌های جایگزینی
│   └── Services.cs            # سرویس‌های شبیه‌سازی
├── ViewModels/MainViewModel.cs
├── Views/
│   ├── MainWindow.axaml
│   └── MainWindow.axaml.cs
├── App.axaml
├── App.axaml.cs
└── Program.cs
```
# MemoryHierarchySimulator
