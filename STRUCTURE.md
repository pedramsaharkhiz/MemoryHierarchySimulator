## Project structure overview

- **`src/Program.cs`**  
  Entry point of the application. Builds and configures the Avalonia `App` (platform detection, logging) and starts the classic desktop lifetime.

- **`src/App.axaml` / `src/App.axaml.cs`**  
  Defines the Avalonia `Application` class, global styles, theme, and DataGrid styling. Responsible for bootstrapping the visual tree and setting up resources shared across windows.

- **`src/Views/MainWindow.axaml` / `src/Views/MainWindow.axaml.cs`**  
  The main UI window of the simulator.  
  - XAML lays out the configuration panel (L1/L2/L3 cache settings, simulation options, action buttons), result dashboard (cards + main chart), details DataGrid, and report/compare tab (comparison chart + logs + policy description).  
  - Code‑behind simply initializes the view; all logic lives in the view model and services.

- **`src/ViewModels/MainViewModel.cs`**  
  Core presentation logic for the UI, implemented with CommunityToolkit.Mvvm.  
  - Exposes bindable properties for all cache parameters, simulation options, statistics, chart series/axes, comparison‑chart data, logs, and results.  
  - Implements commands for **Run**, **Compare**, and **Reset**, orchestrating simulations via `SimulatorService`, filling `Results` and `Logs`, computing aggregated metrics, and preparing data for LiveCharts.

- **`src/Models/Models.cs`**  
  Domain and data models for the memory hierarchy.  
  - Cache structures (`CacheBlock`, `CacheSet`), configuration (`CacheLevelConfig`), enums (`ReplacementPolicy`, `AccessType`, `AccessPattern`).  
  - Simulation data types (`MemoryAccess`, `AccessResult`) and statistics containers (`SimulationStatistics`, `CacheLevelStatistics`) used by services and view models.

- **`src/Services/Services.cs`**  
  Core simulation engine.  
  - `CacheLevel` implements behavior of a single cache level (sets, blocks, replacement strategy, hit/miss accounting).  
  - `MainMemory` and `SecondaryStorage` track accesses and latencies for deeper memory.  
  - `MemoryAccessGenerator` creates synthetic access patterns (sequential, random, locality, stride, loop, mixed).  
  - `SimulatorService` ties everything together: runs accesses through cache levels, updates stats, returns `AccessResult` lists, and produces a human‑readable summary.

- **`src/Services/ReplacementStrategies.cs`**  
  All cache replacement policy implementations and the factory to create them.  
  - Concrete strategies (`LruStrategy`, `FifoStrategy`, `RandomStrategy`, `LfuStrategy`, `MruStrategy`, `RoundRobinStrategy`, `SecondChanceStrategy`, `LfruStrategy`) implement `IReplacementStrategy` and encapsulate victim‑selection logic plus per‑access state updates.  
  - `ReplacementStrategyFactory` maps `ReplacementPolicy` enum values to strategy instances and provides short descriptions used in the UI.

- **`src/MemoryHierarchySimulator.csproj`**  
  .NET project file. Defines target framework, Avalonia and LiveCharts dependencies, build options (including disabling compiled bindings by default), and the DataGrid control package reference.

- **`src/app.manifest`**  
  Application manifest for platform‑specific configuration (windowing/runtime metadata).


