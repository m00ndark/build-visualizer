# Build Visualizer — Project Guide

## What This Is

A **Visual Studio 2022+ extension** (VSIX) that visualizes project build status in real time, organized by project dependency relationships. Built by m00ndark.

- **Language:** C# (.NET Framework 4.7.2)
- **UI:** WPF / XAML (MVVM pattern)
- **Extension SDK:** `Microsoft.VisualStudio.SDK` v17.0, `Microsoft.VisualStudio.ProjectSystem` v17.9
- **Target:** VS Community 2022+ (amd64), multi-platform configs (AnyCPU, arm64, x86)

## Repository Layout

```
BuildVisualizer.slnx                    # Solution file
src/BuildVisualizer/
├── BuildVisualizerPackage.cs           # VS Package entry point
├── source.extension.vsixmanifest       # VSIX metadata (publisher: m00ndark)
├── ToolWindow/
│   ├── BuildVisualizerToolWindow.cs
│   ├── BuildVisualizerToolWindowCommand.cs
│   ├── BuildVisualizerToolWindowControl.xaml[.cs]   # Main UI (list + graph tabs)
│   └── BuildVisualizerPackage.vsct                  # VS command table
├── Views/
│   └── ProjectNodeControl.xaml[.cs]    # Graph node visual (rounded border, status color)
├── ViewModels/
│   ├── BuildVisualizerViewModel.cs     # Main ViewModel — coordinates services, holds project collections
│   ├── ProjectNodeViewModel.cs         # Per-node ViewModel (dynamic width, highlight state)
│   ├── GraphRowGroupViewModel.cs       # Dependency layer grouping
│   └── ViewModelBase.cs                # INotifyPropertyChanged base
├── Models/
│   ├── ProjectInfo.cs                  # Project data: name, status, timestamps, dependencies
│   └── BuildStatus.cs                  # Enum: NotBuilt, Building, Success, Failed, Skipped
├── Services/
│   ├── BuildEventService.cs            # Hooks DTE build events (begin/end per project)
│   ├── SolutionService.cs              # Project discovery & dependency resolution
│   ├── SolutionEventsService.cs        # Solution/project lifecycle (open/close/load/unload)
│   ├── SolutionReferenceWatcher.cs     # Watches project dependencies (delegates to CPS or legacy watcher)
│   ├── SolutionReferenceSnapshot.cs    # One-shot reference resolution (used by Refresh/catch-up)
│   ├── ResolvedReferenceWatcher.cs     # SDK-style (CPS) project references
│   ├── LegacyReferenceWatcher.cs       # Legacy .csproj project references
│   ├── ProjectDataHelper.cs            # Shared helpers: project metadata, output type, test detection, enumeration
│   └── ThemeService.cs                 # VS theme detection (dark/light), fires ThemeChanged
├── Layout/
│   └── GraphLayoutEngine.cs            # Topological sort into layers + barycenter crossing minimization
├── Commands/
│   └── RelayCommand.cs                 # ICommand implementation
├── Converters/                         # WPF value converters
└── Resources/
    └── Colors.cs                       # Theme-aware color palette (light/dark), WCAG 2.1 text contrast
```

## Architecture

### Data Flow

```
VS Build Event → BuildEventService → ProjectStatusChanged event
  → BuildVisualizerViewModel updates ProjectInfo.Status
  → WPF data binding updates UI in real time

Solution Opened → SolutionEventsService → SolutionReferenceWatcher resolves all refs
  → ProjectsChanged event → SolutionService caches project data
  → ViewModel calls GetProjectsAsync() → GraphLayoutEngine computes layers
  → UI renders graph nodes grouped by dependency layer
```

### Key Design Decisions

- **Two views:** List view (sortable data grid) and Graph view (dependency-layered nodes)
- **Graph layout:** Topological sort assigns layers (layer 0 = no deps), barycenter algorithm (3 passes) minimizes edge crossings
- **Dynamic node sizing:** Width adapts to text content (Segoe UI, 12pt measurement), fixed 28px height
- **Responsive wrapping:** WrapPanel handles window resize; nodes wrap within their layer group
- **Theme integration:** Detects VS theme via `VSColorTheme`, auto-selects black/white text per WCAG 2.1 luminance
- **Dual reference support:** CPS (SDK-style) via `ResolvedReferenceWatcher`, legacy via `LegacyReferenceWatcher`
- **Thread safety:** Uses `JoinableTaskFactory` for UI-thread marshaling per VSSDK guidelines

## Development Status

See `TODO.md` for improvements, ideas and known bugs.

## Building & Debugging

- Open `BuildVisualizer.slnx` in Visual Studio 2022
- Build with standard VS build (F5 or Ctrl+Shift+B)
- Debug launches VS Experimental Instance (`devenv /rootsuffix Exp`) — the extension is auto-installed there
- The tool window appears under **View → Other Windows → Build Visualizer**
