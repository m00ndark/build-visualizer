# Build Visualizer - Implementation Plan

A Visual Studio extension that visualizes project build dependencies in a tool window and updates projects with colors (red/green) based on build results in real-time.

## Overview

The extension will:
1. Display a graph/tree visualization of project build dependencies
2. Show real-time build status with color coding (green = success, red = failure, yellow = building)
3. Provide a tool window accessible from View menu
4. React to build events as they happen

---

## 🎯 Implementation Strategy

Each increment is **runnable and testable** - you can verify functionality at every step!

---

## Increment 1: Basic Tool Window (Runnable ✓)

**Goal:** Show an empty tool window with "Hello Build Visualizer" text

**Verification:** Open VS, go to View → Other Windows → Build Visualizer, see the window appear

### Tasks
- [x] Add required NuGet packages to `BuildVisualizer.csproj`:
  - `Microsoft.VisualStudio.SDK` (or specific assemblies)
  - `Microsoft.VSSDK.BuildTools`
  - `Microsoft.VisualStudio.Shell.15.0`
  - `Microsoft.VisualStudio.Shell.Interop`

- [x] Create `Commands/ShowToolWindowCommand.cs`:
  - Command ID and GUID
  - Execute method to show tool window
  - Register with command service

- [x] Create `ToolWindow/BuildVisualizerToolWindow.cs`:
  - Inherit from `ToolWindowPane`
  - Set window title and content

- [x] Create `ToolWindow/BuildVisualizerControl.xaml`:
  - Simple TextBlock with "Hello Build Visualizer"

- [x] Create `ToolWindow/BuildVisualizerControl.xaml.cs`:
  - Code-behind (can be empty for now)

- [x] Update `BuildVisualizerPackage.cs`:
  - Add `[ProvideToolWindow]` attribute
  - Add `[ProvideMenuResource]` attribute
  - Register command in `InitializeAsync()`

- [x] Create `BuildVisualizerPackage.vsct`:
  - Define menu group under View → Other Windows
  - Define command button

- [x] Update `source.extension.vsixmanifest`:
  - Add BuildVisualizerPackage asset if needed
  - Verify package asset is registered

**✓ Checkpoint:** Press F5, see the tool window in the View menu, open it and see your message!

---

## Increment 2: Display Project List (Runnable ✓)

**Goal:** Show a simple list of all projects in the current solution

**Verification:** Open a solution with multiple projects, see them listed in the tool window

### Tasks
- [x] Create `Models/BuildStatus.cs`:
  - Enum: `NotBuilt`, `Building`, `Success`, `Failed`, `Skipped`

- [x] Create `Models/ProjectInfo.cs`:
  - Properties: Name, UniqueName, Status
  - Simple class (no dependencies yet)

- [x] Create `Services/SolutionService.cs`:
  - Get `DTE2` service
  - Method `GetProjects()` - return list of project names
  - No dependency parsing yet

- [x] Update `ToolWindow/BuildVisualizerControl.xaml`:
  - Add `ListBox` to display projects
  - ItemTemplate showing project name

- [x] Update `ToolWindow/BuildVisualizerControl.xaml.cs`:
  - Constructor: call SolutionService, populate ListBox
  - Simple data binding or Items.Add()

- [x] Update `BuildVisualizerPackage.cs`:
  - Pass DTE2 to tool window if needed

**✓ Checkpoint:** Open a solution, open the tool window, see all project names listed!

---

## Increment 2.5: Refactor to MVVM Architecture ✨ **NEW** (Runnable ✓)

**Goal:** Refactor existing code-behind approach to MVVM pattern with proper data binding

**Verification:** Tool window works exactly as before, but now using MVVM (check code structure)

### Tasks
- [x] Create `ViewModels/ViewModelBase.cs`:
  - Implement `INotifyPropertyChanged`
  - Protected method `OnPropertyChanged(string propertyName)`
  - Protected method `SetProperty<T>(ref T field, T value, string propertyName)` that returns bool

- [x] Create `Commands/RelayCommand.cs`:
  - Implement `ICommand` interface
  - Constructor: `Action<object> execute`, `Func<object, bool> canExecute = null`
  - Handle `CanExecuteChanged` event
  - Public method `RaiseCanExecuteChanged()`

- [x] Update `Models/ProjectInfo.cs`:
  - Inherit from `ViewModelBase`
  - Make `Name`, `UniqueName`, `Status` properties use `SetProperty` pattern
  - Ensure PropertyChanged events fire correctly

- [x] Create `ViewModels/BuildVisualizerViewModel.cs`:
  - Inherit from `ViewModelBase`
  - Property: `ObservableCollection<ProjectInfo> Projects { get; set; }`
  - Property: `ICommand RefreshCommand { get; }`
  - Private field: `SolutionService _solutionService`
  - Constructor: Accept `SolutionService` parameter, initialize RefreshCommand
  - Private async method: `LoadProjectsAsync()` - call `_solutionService.GetProjects()`, populate Projects
  - Call `LoadProjectsAsync()` in constructor

- [x] Update `ToolWindow/BuildVisualizerToolWindowControl.xaml`:
  - Remove `x:Name` attributes from Button and ListBox
  - Change Button: Remove `Click` attribute, add `Command="{Binding RefreshCommand}"`
  - Change ListBox: Remove `Name` attribute, add `ItemsSource="{Binding Projects}"`
  - Add design-time DataContext: `xmlns:vm="clr-namespace:BuildVisualizer.ViewModels"` and `d:DataContext="{d:DesignInstance Type=vm:BuildVisualizerViewModel}"`

- [x] Update `ToolWindow/BuildVisualizerToolWindowControl.xaml.cs`:
  - Remove `LoadProjects()` method
  - Remove `RefreshButton_Click` event handler
  - In constructor, after `InitializeComponent()`, set: `DataContext = new BuildVisualizerViewModel(new SolutionService(_dte))`
  - Keep only minimal code: constructor with DTE parameter and DataContext assignment

- [x] Verify functionality:
  - Press F5, open tool window, see projects listed
  - Click Refresh button, verify it reloads projects
  - Ensure no code-behind logic remains except DataContext setup

**✓ Checkpoint:** Extension works identically, but with clean MVVM separation! Code-behind is minimal.

---

## Increment 3: Show Build Status with Colors (Runnable ✓)

**Goal:** Update project colors in real-time as builds happen using MVVM data binding

**Verification:** Open tool window, start a build, watch projects turn yellow → green/red

### Tasks
- [x] Create `Resources/Colors.cs`:
  - Static class with readonly `SolidColorBrush` properties:
    - `NotBuiltBrush` - `new SolidColorBrush(Color.FromRgb(128, 128, 128))` - Gray
    - `BuildingBrush` - `new SolidColorBrush(Color.FromRgb(255, 165, 0))` - Orange/Yellow
    - `SuccessBrush` - `new SolidColorBrush(Color.FromRgb(76, 175, 80))` - Green
    - `FailedBrush` - `new SolidColorBrush(Color.FromRgb(244, 67, 54))` - Red
    - `SkippedBrush` - `new SolidColorBrush(Color.FromRgb(33, 150, 243))` - Blue
  - Freeze all brushes for performance

- [x] Update `Models/ProjectInfo.cs`:
  - Add computed property: `SolidColorBrush StatusColor { get; }` (no setter)
  - In getter: Return appropriate brush from `Colors.cs` based on `Status` value
  - Override `OnPropertyChanged`: When "Status" changes, also call `OnPropertyChanged("StatusColor")`

- [x] Create `Services/BuildEventService.cs`:
  - Private field: `DTE2 _dte`, `BuildEvents _buildEvents`
  - Public event: `EventHandler<ProjectStatusChangedEventArgs> ProjectStatusChanged`
  - Constructor: Accept `DTE2`, subscribe to `_dte.Events.BuildEvents`:
    - `OnBuildBegin`: Fire event for all projects with `BuildStatus.Building`
    - `OnBuildProjConfigBegin`: Fire event for specific project (parse `project` param) with `BuildStatus.Building`
    - `OnBuildProjConfigDone`: Fire event with `BuildStatus.Success` if success, else `BuildStatus.Failed`
  - Implement `IDisposable`: Unsubscribe from events
  - Use `ThreadHelper.ThrowIfNotOnUIThread()` appropriately

- [x] Create `Models/ProjectStatusChangedEventArgs.cs`:
  - Inherit from `EventArgs`
  - Properties: `string ProjectUniqueName { get; }`, `BuildStatus NewStatus { get; }`
  - Constructor: Accept and assign both properties

- [x] Update `ViewModels/BuildVisualizerViewModel.cs`:
  - Add private field: `BuildEventService _buildEventService`
  - Update constructor: Accept `BuildEventService` parameter
  - In constructor: Subscribe to `_buildEventService.ProjectStatusChanged`
  - Event handler: Find matching project in `Projects` collection by UniqueName, update its `Status` property
  - Use `ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()` if needed for UI thread

- [x] Update `ToolWindow/BuildVisualizerToolWindowControl.xaml`:
  - Update `ListBox.ItemTemplate`:
    - Add `Border` with `Background="{Binding StatusColor}"`, `Width="16"`, `Height="16"`, `Margin="0,0,5,0"`
    - Add `StackPanel` with `Orientation="Horizontal"`
    - Show project name and status: `<TextBlock Text="{Binding Name}"/>` and `<TextBlock Text="{Binding Status}" Margin="5,0,0,0" FontSize="10" Foreground="Gray"/>`

- [x] Update `ToolWindow/BuildVisualizerToolWindow.cs`:
  - In `BuildVisualizerToolWindow` constructor, get `BuildEventService` instance
  - Pass to `BuildVisualizerToolWindowControl` constructor or create in control

- [x] Update `BuildVisualizerPackage.cs`:
  - Create `BuildEventService` instance in `InitializeAsync()`
  - Store as package-level field for reuse
  - Ensure proper disposal

**✓ Checkpoint:** Start a build, watch projects change color in real-time through data binding!

---

## Increment 4: Show Project Dependencies (Runnable ✓)

**Goal:** Display which projects depend on which using MVVM data binding

**Verification:** See "ProjectA → depends on → ProjectB, ProjectC" in the UI

### Tasks
- [ ] Update `Models/ProjectInfo.cs`:
  - Add property: `ObservableCollection<string> Dependencies { get; set; }` (initialize in constructor)
  - Add property: `ObservableCollection<string> Dependents { get; set; }` (initialize in constructor)
  - Add computed property: `string DependenciesText { get; }` - returns formatted string like "→ Dep1, Dep2" or "No dependencies" if empty
  - Raise PropertyChanged for DependenciesText when Dependencies collection changes

- [ ] Update `Services/SolutionService.cs`:
  - Add method: `void ParseProjectDependencies(List<ProjectInfo> projects)`
  - For each project:
    - Get `Project` from DTE by UniqueName
    - Cast to `VSProject` (using VSLangProj namespace)
    - Iterate through `References` collection
    - For each reference that is a project reference (check `SourceProject != null`):
      - Add reference name to project's `Dependencies`
      - Add current project name to referenced project's `Dependents`
  - Handle exceptions for projects that can't be cast to VSProject

- [ ] Update `ViewModels/BuildVisualizerViewModel.cs`:
  - In `LoadProjectsAsync()`: After getting projects, call `_solutionService.ParseProjectDependencies(projects)`
  - Ensure ObservableCollection updates trigger UI refresh

- [ ] Update `ToolWindow/BuildVisualizerToolWindowControl.xaml`:
  - Update `ListBox.ItemTemplate`:
    - Add second `TextBlock` below project name
    - Bind to `DependenciesText` with smaller font and gray foreground
    - Example: `<TextBlock Text="{Binding DependenciesText}" FontSize="10" Foreground="Gray" Margin="20,0,0,0"/>`

**✓ Checkpoint:** See dependencies listed under each project in the list!

---

## Increment 5: Tree/Hierarchy View with MVVM (Runnable ✓)

**Goal:** Show projects in a hierarchical dependency tree using proper ViewModel pattern

**Verification:** See collapsible tree showing dependency relationships

### Tasks
- [ ] Create `ViewModels/ProjectNodeViewModel.cs`:
  - Inherit from `ViewModelBase`
  - Property: `ProjectInfo ProjectData { get; }` - wrapped model (readonly, set in constructor)
  - Property: `ObservableCollection<ProjectNodeViewModel> Children { get; set; }` - child nodes
  - Property: `bool IsExpanded { get; set; }` - for TreeView expansion state, use SetProperty
  - Delegated properties from ProjectInfo: `string Name { get; }`, `BuildStatus Status { get; }`, `SolidColorBrush StatusColor { get; }`
  - Subscribe to ProjectData.PropertyChanged: When Status changes, raise PropertyChanged for Status and StatusColor
  - Constructor: Accept ProjectInfo, initialize Children collection

- [ ] Create `Services/DependencyGraphBuilder.cs`:
  - Method: `ObservableCollection<ProjectNodeViewModel> BuildHierarchy(List<ProjectInfo> projects)`
  - Algorithm:
    - Create dictionary mapping project names to ProjectNodeViewModel instances
    - Find root projects: projects with no dependencies (Dependencies.Count == 0)
    - For each project with dependencies, find its dependency nodes and add current node to their Children
    - Return collection of root nodes only (nodes that are not children of any other node)
  - Handle circular dependencies: Track visited nodes to prevent infinite recursion

- [ ] Update `ViewModels/BuildVisualizerViewModel.cs`:
  - Add property: `ObservableCollection<ProjectNodeViewModel> ProjectTree { get; set; }`
  - Add private field: `DependencyGraphBuilder _graphBuilder`
  - Update constructor: Initialize `_graphBuilder = new DependencyGraphBuilder()`
  - In `LoadProjectsAsync()`: After parsing dependencies, call `ProjectTree = _graphBuilder.BuildHierarchy(projects.ToList())`
  - Keep `Projects` collection for backwards compatibility or remove if not needed

- [ ] Update `ToolWindow/BuildVisualizerToolWindowControl.xaml`:
  - Add `Grid.Row` or `TabControl` to switch between List and Tree views
  - Add `TreeView`:
    - Bind `ItemsSource="{Binding ProjectTree}"`
    - `HierarchicalDataTemplate`:
      - Set `ItemsSource="{Binding Children}"`
      - Bind `IsExpanded="{Binding IsExpanded, Mode=TwoWay}"`
      - Show: Colored rectangle (Border with StatusColor background) + project name
      - Example:
        ```xml
        <StackPanel Orientation="Horizontal">
          <Border Width="16" Height="16" Background="{Binding StatusColor}" Margin="0,0,5,0"/>
          <TextBlock Text="{Binding Name}"/>
          <TextBlock Text="{Binding Status}" FontSize="10" Foreground="Gray" Margin="5,0,0,0"/>
        </StackPanel>
        ```

**✓ Checkpoint:** See projects in a collapsible tree structure showing dependency hierarchy!

---

## Increment 6: Graph Canvas Visualization with MVVM (Runnable ✓)

**Goal:** Display projects as positioned nodes on a Canvas using MVVM data binding

**Verification:** See boxes representing projects connected by arrows

### Tasks
- [ ] Create `Layout/GraphLayoutEngine.cs`:
  - Method: `void CalculateLayout(List<ProjectNodeViewModel> nodes, double canvasWidth = 800)`
  - Implement simple layered layout algorithm:
    - Perform topological sort to assign layers (projects with no dependencies = layer 0)
    - Calculate X positions: Space nodes evenly within each layer
    - Calculate Y positions: Vertical spacing between layers (e.g., 150px)
  - Directly set X, Y properties on ProjectNodeViewModel instances
  - Return required canvas dimensions (width, height)

- [ ] Update `ViewModels/ProjectNodeViewModel.cs`:
  - Add property: `double X { get; set; }` - use SetProperty
  - Add property: `double Y { get; set; }` - use SetProperty
  - Add property: `double Width { get; set; } = 120` - node width
  - Add property: `double Height { get; set; } = 60` - node height
  - Add property: `ObservableCollection<ProjectNodeViewModel> DependencyNodes { get; set; }` - references to actual dependency node objects (not just names)

- [ ] Create `ViewModels/DependencyLineViewModel.cs`:
  - Inherit from `ViewModelBase`
  - Property: `double X1 { get; set; }`, `double Y1 { get; set; }`, `double X2 { get; set; }`, `double Y2 { get; set; }`
  - Constructor: Accept `ProjectNodeViewModel source`, `ProjectNodeViewModel target`
  - Calculate line endpoints from node positions (center of source to center of target)
  - Subscribe to source/target PropertyChanged for X, Y to recalculate line endpoints

- [ ] Create `Views/ProjectNodeControl.xaml` + `.xaml.cs`:
  - UserControl with `Border` as root:
    - `Background="{Binding StatusColor}"`
    - `BorderBrush="Black"`, `BorderThickness="2"`, `CornerRadius="5"`
    - `Width="{Binding Width}"`, `Height="{Binding Height}"`
  - Inside Border: `TextBlock` with `Text="{Binding Name}"`, centered, white foreground

- [ ] Update `ViewModels/BuildVisualizerViewModel.cs`:
  - Add property: `ObservableCollection<ProjectNodeViewModel> GraphNodes { get; set; }` - flat list of all nodes with positions
  - Add property: `ObservableCollection<DependencyLineViewModel> DependencyLines { get; set; }`
  - Add property: `double CanvasWidth { get; set; }`, `double CanvasHeight { get; set; }`
  - Add private field: `GraphLayoutEngine _layoutEngine = new GraphLayoutEngine()`
  - Add private method: `BuildGraphLayout()`:
    - Flatten ProjectTree into GraphNodes (recursive traversal)
    - Populate DependencyNodes for each node (resolve string names to node references)
    - Call `_layoutEngine.CalculateLayout(GraphNodes.ToList())`
    - Build DependencyLines from each node's DependencyNodes
    - Update CanvasWidth and CanvasHeight
  - Call `BuildGraphLayout()` after building ProjectTree

- [ ] Update `ToolWindow/BuildVisualizerToolWindowControl.xaml`:
  - Add `TabControl` with tabs: "List", "Tree", "Graph"
  - Graph tab content:
    - `ScrollViewer` with `HorizontalScrollBarVisibility="Auto"`, `VerticalScrollBarVisibility="Auto"`
    - `Canvas` with `Width="{Binding CanvasWidth}"`, `Height="{Binding CanvasHeight}"`
    - Lines layer: `ItemsControl` bound to `DependencyLines`:
      - ItemsPanel: `Canvas`
      - ItemTemplate: `Line` with `X1="{Binding X1}"`, `Y1="{Binding Y1}"`, `X2="{Binding X2}"`, `Y2="{Binding Y2}"`, `Stroke="Gray"`, `StrokeThickness="2"`
    - Nodes layer: `ItemsControl` bound to `GraphNodes`:
      - ItemsPanel: `Canvas`
      - ItemTemplate: `ProjectNodeControl`
      - ItemContainerStyle: Set `Canvas.Left="{Binding X}"`, `Canvas.Top="{Binding Y}"`

**✓ Checkpoint:** Click Graph tab, see projects positioned as boxes connected by lines!

---

## Increment 7: Improved Layout Algorithm (Runnable ✓)

**Goal:** Better graph positioning - minimize line crossings, improve readability

**Verification:** Cleaner graph layout, especially for complex dependency graphs

### Tasks
- [ ] Update `Layout/GraphLayoutEngine.cs`:
  - Implement proper layered graph layout (Sugiyama framework):
    - **Phase 1 - Layer assignment**: Topological sort with longest path layer assignment
    - **Phase 2 - Crossing reduction**: Barycenter heuristic to reorder nodes within layers
    - **Phase 3 - X-coordinate assignment**: Center nodes based on their dependencies
  - Add configurable spacing parameters: `NodeHorizontalSpacing = 150`, `NodeVerticalSpacing = 120`
  - Handle disconnected graphs: Process each connected component separately, arrange side-by-side
  - Return actual required canvas size based on layout
  - Add method: `Dictionary<int, List<ProjectNodeViewModel>> AssignLayers()` - returns nodes grouped by layer
  - Add method: `void MinimizeCrossings(Dictionary<int, List<ProjectNodeViewModel>> layers)` - reorders nodes
  - Add method: `void AssignCoordinates(Dictionary<int, List<ProjectNodeViewModel>> layers)` - calculates X, Y

- [ ] Update `ViewModels/BuildVisualizerViewModel.cs`:
  - Update `BuildGraphLayout()` to use enhanced layout algorithm
  - Update CanvasWidth and CanvasHeight based on layout engine return values
  - Add padding/margins (e.g., 50px on all sides)

**✓ Checkpoint:** Open a complex solution, see much cleaner graph layout with fewer crossing lines!

---

## Increment 8: User Interactions with MVVM Commands (Runnable ✓)

**Goal:** Add click interactions, zoom/pan using MVVM commands and behaviors

**Verification:** Double-click project to see it in Solution Explorer, zoom with mouse wheel

### Tasks
- [ ] Update `ViewModels/ProjectNodeViewModel.cs`:
  - Add property: `ICommand BuildProjectCommand { get; }`
  - Add property: `ICommand NavigateToProjectCommand { get; }`
  - Add property: `string FullPath { get; }` - delegate from ProjectInfo or calculate
  - In constructor: Initialize commands using RelayCommand
  - BuildProjectCommand execute: Call DTE to build specific project
  - NavigateToProjectCommand execute: Call DTE to show project in Solution Explorer

- [ ] Update `Views/ProjectNodeControl.xaml`:
  - Add `InputBindings`: `<MouseBinding Gesture="LeftDoubleClick" Command="{Binding NavigateToProjectCommand}"/>`
  - Add `ContextMenu`:
    - MenuItem "Build Project" bound to `BuildProjectCommand`
    - MenuItem "Rebuild Project" (create new command)
    - MenuItem "Show in Solution Explorer" bound to `NavigateToProjectCommand`
  - Add `ToolTip` showing project full path and status details

- [ ] Create `Behaviors/ZoomBehavior.cs`:
  - Attached behavior for Canvas zoom using ScaleTransform
  - Attach to MouseWheel event
  - Apply ScaleTransform on Ctrl+MouseWheel
  - Zoom centered on mouse position

- [ ] Create `Behaviors/PanBehavior.cs`:
  - Attached behavior for Canvas panning using TranslateTransform
  - Attach to MouseMove with left button down
  - Track drag start position, calculate delta, apply TranslateTransform

- [ ] Update `ViewModels/BuildVisualizerViewModel.cs`:
  - Add property: `ICommand FitToWindowCommand { get; }`
  - Add property: `double ZoomLevel { get; set; } = 1.0`
  - Add property: `double PanX { get; set; } = 0`
  - Add property: `double PanY { get; set; } = 0`
  - FitToWindowCommand: Calculate zoom level to fit all nodes, reset pan, update properties
  - RefreshCommand: Reload solution and rebuild graph

- [ ] Update `ToolWindow/BuildVisualizerToolWindowControl.xaml`:
  - Add toolbar above Canvas with buttons:
    - "Fit to Window" button bound to `FitToWindowCommand`
    - "Zoom In" / "Zoom Out" buttons (optional, or just use mouse wheel)
    - "Reset View" button
  - Apply behaviors to Canvas: `<i:Interaction.Behaviors>` with ZoomBehavior and PanBehavior
  - Apply RenderTransform to Canvas: ScaleTransform and TranslateTransform bound to ZoomLevel, PanX, PanY

**✓ Checkpoint:** Interact with graph - double-click nodes, right-click for menu, zoom/pan the canvas!

---

## Increment 9: Polish & Reactive Updates (Runnable ✓)

**Goal:** Handle solution events reactively, improve performance, add polish using MVVM

**Verification:** Add/remove projects dynamically, handles large solutions smoothly

### Tasks
- [ ] Create `Services/SolutionEventService.cs`:
  - Subscribe to DTE solution events: `SolutionEvents`
  - Events: `Opened`, `AfterClosing`, `ProjectAdded`, `ProjectRemoved`, `ProjectRenamed`
  - Expose public events that ViewModels can subscribe to
  - Implement IDisposable

- [ ] Update `ViewModels/BuildVisualizerViewModel.cs`:
  - Inject `SolutionEventService` in constructor
  - Subscribe to solution events:
    - `SolutionOpened`: Call `LoadProjectsAsync()`
    - `SolutionClosed`: Clear all collections, show "Open a solution" message
    - `ProjectAdded`: Incrementally add to Projects collection and rebuild graph
    - `ProjectRemoved`: Remove from Projects and rebuild graph
  - Add property: `string StatusMessage { get; set; }` - show "Loading...", "No solution open", etc.
  - Add property: `bool IsLoading { get; set; }` - for progress indication
  - Make `LoadProjectsAsync()` truly async with `Task.Run()` for project parsing
  - Show/hide loading indicator based on `IsLoading`

- [ ] Performance optimizations:
  - Update `LoadProjectsAsync()`: Use `await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()` appropriately
  - Cache project information in `SolutionService`
  - Incremental graph updates: Only recalculate layout for affected nodes when single project changes
  - For 50+ projects: Add virtualization hints or consider viewport-based rendering

- [ ] Visual polish:
  - Add `DoubleAnimation` for StatusColor changes (smooth color transitions)
  - Create `Converters/BuildStatusToColorConverter.cs` if needed for animations
  - Highlight build path: When building, increase border thickness or add glow effect to building projects
  - Add project type icons: Detect project type and show appropriate icon in node

- [ ] Error handling:
  - Wrap all DTE calls in try-catch blocks
  - Add `string ErrorMessage { get; set; }` property in ViewModel
  - Show error messages in UI (e.g., InfoBar or TextBlock at top of window)
  - Handle circular dependencies: Mark with warning icon or different color
  - Log errors to Visual Studio Output window

- [ ] Update `ToolWindow/BuildVisualizerToolWindowControl.xaml`:
  - Add `TextBlock` bound to `StatusMessage` - show at top or center when no solution
  - Add `ProgressBar` bound to `IsLoading` - show when loading
  - Add visual states for different UI states (loading, empty, error, ready)

**✓ Checkpoint:** Extension handles all solution changes gracefully, performs well with large solutions!

---

## Increment 10: Documentation & Testing (Final ✓)

**Goal:** Complete, documented, tested extension ready for use

**Verification:** Extension works reliably, good documentation, tests pass

### Tasks
- [ ] Testing:
  - [ ] Test with small solution (2-3 projects)
  - [ ] Test with medium solution (10-20 projects)
  - [ ] Test with large solution (50+ projects)
  - [ ] Test with circular dependencies
  - [ ] Test build cancellation
  - [ ] Test with different project types (.NET Framework, .NET Core, C++, etc.)

- [ ] Documentation:
  - [ ] Create README.md:
    - Extension description
    - Features list
    - Installation instructions
    - Usage guide with screenshots
    - Keyboard shortcuts
    - Known limitations
  - [ ] Add XML doc comments to public APIs
  - [ ] Create CHANGELOG.md

- [ ] Package for distribution:
  - [ ] Update extension metadata (description, tags, icon)
  - [ ] Create extension icon
  - [ ] Prepare for Visual Studio Marketplace
  - [ ] Add license file

**✓ Final Checkpoint:** Ship it! 🚀

---

## 📊 Progress Tracking

Track your progress through the increments:

- [x] Increment 1: Basic Tool Window ✓ Runnable
- [x] Increment 2: Display Project List ✓ Runnable
- [x] Increment 2.5: Refactor to MVVM ✓ Runnable
- [x] Increment 3: Build Status with Colors ✓ Runnable
- [ ] Increment 4: Show Dependencies ✓ Runnable
- [ ] Increment 5: Tree/Hierarchy View ✓ Runnable
- [ ] Increment 6: Graph Canvas Visualization ✓ Runnable
- [ ] Increment 7: Improved Layout ✓ Runnable
- [ ] Increment 8: User Interactions ✓ Runnable
- [ ] Increment 9: Polish & Reactive Updates ✓ Runnable
- [ ] Increment 10: Documentation & Testing ✓ Ready to Ship!

---

## 📁 File Structure (Final)

```
src/BuildVisualizer/
├── BuildVisualizerPackage.cs          # Main package entry point
├── source.extension.vsixmanifest      # VSIX manifest
├── BuildVisualizer.csproj             # Project file
│
├── Commands/
│   ├── ShowToolWindowCommand.cs       # Menu command to show tool window
│   └── RelayCommand.cs                # ICommand implementation for MVVM
│
├── ToolWindow/
│   ├── BuildVisualizerToolWindow.cs   # Tool window host (ToolWindowPane)
│   ├── BuildVisualizerToolWindowControl.xaml      # Main UI with TabControl (List/Tree/Graph views)
│   ├── BuildVisualizerToolWindowControl.xaml.cs   # Minimal code-behind (DataContext setup only)
│   └── BuildVisualizerPackage.vsct    # Command definitions
│
├── Models/
│   ├── ProjectInfo.cs                 # Project data model (inherits ViewModelBase)
│   ├── BuildStatus.cs                 # Status enum
│   └── ProjectStatusChangedEventArgs.cs  # Event args for build status changes
│
├── ViewModels/
│   ├── ViewModelBase.cs               # Base class with INotifyPropertyChanged
│   ├── BuildVisualizerViewModel.cs    # Main ViewModel for tool window
│   ├── ProjectNodeViewModel.cs        # Node ViewModel with positioning & hierarchy
│   └── DependencyLineViewModel.cs     # Line ViewModel for graph edges
│
├── Services/
│   ├── SolutionService.cs             # Solution/project parsing
│   ├── BuildEventService.cs           # Build event handling
│   ├── SolutionEventService.cs        # Solution event handling (open/close/add/remove)
│   └── DependencyGraphBuilder.cs      # Build hierarchy from dependencies
│
├── Layout/
│   └── GraphLayoutEngine.cs           # Node positioning algorithm (Sugiyama)
│
├── Views/
│   ├── ProjectNodeControl.xaml        # Node visual (colored rectangle with text)
│   └── ProjectNodeControl.xaml.cs     # Code-behind for node control
│
├── Behaviors/
│   ├── ZoomBehavior.cs                # Attached behavior for canvas zoom
│   └── PanBehavior.cs                 # Attached behavior for canvas pan
│
├── Converters/
│   └── BuildStatusToColorConverter.cs # Value converter (if needed)
│
├── Resources/
│   ├── Colors.cs                      # Status color constants (SolidColorBrush)
│   └── Icons/                         # Extension icons
│
└── Properties/
    └── AssemblyInfo.cs
```

---

## 🔑 Key VS SDK APIs by Increment

| Increment | APIs Needed | MVVM Components |
|-----------|-------------|-----------------|
| **1** | `ToolWindowPane`, `AsyncPackage`, `OleMenuCommand` | N/A |
| **2** | `DTE2`, `Solution.Projects`, `Project` | N/A |
| **2.5** | N/A | `INotifyPropertyChanged`, `ICommand`, `ObservableCollection`, Data Binding |
| **3** | `BuildEvents`, `INotifyPropertyChanged`, `Dispatcher`, `JoinableTaskFactory` | ViewModels, RelayCommand, Property binding |
| **4** | `VSProject`, `References`, project hierarchy navigation | ObservableCollection, Computed properties |
| **5** | WPF `TreeView`, `HierarchicalDataTemplate` | ViewModel tree structure, Parent-Child relationships |
| **6** | WPF `Canvas`, `ItemsControl`, `Path` geometry, `ScaleTransform` | Multiple ViewModels, Collection binding |
| **7** | Graph algorithms (topological sort, barycenter heuristic) | Layout coordination in ViewModel |
| **8** | `IVsSolutionBuildManager`, `DTE.ExecuteCommand` | Commands, Attached Behaviors, InputBindings |
| **9** | `SolutionEvents`, `IVsHierarchyEvents`, async/await patterns | Reactive properties, Event-driven updates |
| **10** | Testing & packaging | N/A |

---

## 💡 Key Implementation Notes

### MVVM Pattern
- **Separation of Concerns**: ViewModels handle all business logic and state, Views are declarative XAML with minimal code-behind
- **Data Binding**: All UI updates happen through property changes and data binding - no direct UI manipulation
- **Commands**: Use `ICommand` (RelayCommand) for all user actions instead of event handlers
- **ObservableCollection**: Use for any collection that needs to notify UI of changes (add/remove items)
- **ViewModelBase**: Single base class implementing INotifyPropertyChanged reduces boilerplate

### Thread Safety
- Build events fire on background threads → Use `JoinableTaskFactory.SwitchToMainThreadAsync()` or `Dispatcher.Invoke()` for UI updates
- Always check `ThreadHelper.ThrowIfNotOnUIThread()` when calling DTE
- ViewModel property setters automatically marshal to UI thread through data binding

### Performance Tips
- Cache project information after parsing - store in ViewModel or Service
- Use incremental updates for build status (don't rebuild entire graph) - update specific ProjectInfo.Status
- For 50+ projects, consider Canvas virtualization or Level-of-Detail rendering
- Debounce rapid status changes in BuildEventService
- Make expensive operations async (`LoadProjectsAsync`, layout calculations)

### Common Pitfalls to Avoid
- ❌ Don't hold references to DTE objects long-term (can cause memory leaks) - use and release
- ❌ Don't block UI thread while parsing projects - use async/await
- ❌ Don't forget to unsubscribe from events in Dispose() - ViewModels and Services implementing IDisposable
- ❌ Don't put business logic in code-behind - keep it in ViewModels
- ❌ Don't manipulate UI elements directly from ViewModels - use data binding
- ✅ Use `IAsyncServiceProvider` for getting services
- ✅ Test with various solution sizes regularly
- ✅ Use proper MVVM: View → ViewModel → Model → Service

### Useful VS Commands
```csharp
// Show in Solution Explorer
DTE.ExecuteCommand("View.SolutionExplorer");
DTE.ExecuteCommand("Project.OpenSolution", projectPath);

// Build commands  
DTE.ExecuteCommand("Build.BuildSelection");
DTE.ExecuteCommand("Build.RebuildSelection");
```

### MVVM Best Practices for VS Extensions
- Keep DTE references in Services, not ViewModels
- Pass services to ViewModels via constructor injection
- ViewModels should be testable without VS SDK
- Use events or callbacks to communicate from Services to ViewModels
- Dispose services properly when tool window closes

---

## 🚀 Quick Start Guide

1. **Start with Increment 2.5** - Refactor existing code to MVVM (critical foundation)
2. **Test after each increment** - Don't move forward until current increment works
3. **Commit after each increment** - Easy to roll back if needed
4. **Follow MVVM strictly** - No business logic in code-behind, all in ViewModels
5. **Use Debug output** - Add logging to understand build events flow

### Session Goals

**Session 1: MVVM Foundation (2-3 hours)**
- Complete Increment 2.5 (MVVM refactoring)
- Verify existing functionality still works
- Understand ViewModel → Service → DTE flow

**Session 2: Build Events (2-3 hours)**
- Complete Increment 3 (build status with colors)
- Watch real-time color updates through data binding
- Ensure thread-safe property updates

**Session 3: Dependencies (2-3 hours)**
- Complete Increment 4 (show dependencies)
- Complete Increment 5 (tree view)
- Master hierarchical ViewModels

**Session 4: Graph Visualization (3-4 hours)**
- Complete Increment 6 (canvas graph)
- Implement basic layout algorithm
- See first version of graph view

**Session 5: Advanced Features (3-4 hours)**
- Complete Increment 7 (improved layout)
- Complete Increment 8 (user interactions)
- Add zoom, pan, commands

**Session 6: Production Ready (2-3 hours)**
- Complete Increment 9 (polish & reactive updates)
- Complete Increment 10 (documentation & testing)
- Package and deploy

---

*Last updated: 2025 - MVVM Edition*
