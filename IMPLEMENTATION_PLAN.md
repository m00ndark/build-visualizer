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
- [ ] Add required NuGet packages to `BuildVisualizer.csproj`:
  - `Microsoft.VisualStudio.SDK` (or specific assemblies)
  - `Microsoft.VSSDK.BuildTools`
  - `Microsoft.VisualStudio.Shell.15.0`
  - `Microsoft.VisualStudio.Shell.Interop`

- [ ] Create `Commands/ShowToolWindowCommand.cs`:
  - Command ID and GUID
  - Execute method to show tool window
  - Register with command service

- [ ] Create `ToolWindow/BuildVisualizerToolWindow.cs`:
  - Inherit from `ToolWindowPane`
  - Set window title and content

- [ ] Create `ToolWindow/BuildVisualizerControl.xaml`:
  - Simple TextBlock with "Hello Build Visualizer"

- [ ] Create `ToolWindow/BuildVisualizerControl.xaml.cs`:
  - Code-behind (can be empty for now)

- [ ] Update `BuildVisualizerPackage.cs`:
  - Add `[ProvideToolWindow]` attribute
  - Add `[ProvideMenuResource]` attribute
  - Register command in `InitializeAsync()`

- [ ] Create `VSCommandTable.vsct`:
  - Define menu group under View → Other Windows
  - Define command button

- [ ] Update `source.extension.vsixmanifest`:
  - Add VSCommandTable asset if needed
  - Verify package asset is registered

**✓ Checkpoint:** Press F5, see the tool window in the View menu, open it and see your message!

---

## Increment 2: Display Project List (Runnable ✓)

**Goal:** Show a simple list of all projects in the current solution

**Verification:** Open a solution with multiple projects, see them listed in the tool window

### Tasks
- [ ] Create `Models/BuildStatus.cs`:
  - Enum: `NotBuilt`, `Building`, `Success`, `Failed`, `Skipped`

- [ ] Create `Models/ProjectInfo.cs`:
  - Properties: Name, UniqueName, Status
  - Simple class (no dependencies yet)

- [ ] Create `Services/SolutionService.cs`:
  - Get `DTE2` service
  - Method `GetProjects()` - return list of project names
  - No dependency parsing yet

- [ ] Update `ToolWindow/BuildVisualizerControl.xaml`:
  - Add `ListBox` to display projects
  - ItemTemplate showing project name

- [ ] Update `ToolWindow/BuildVisualizerControl.xaml.cs`:
  - Constructor: call SolutionService, populate ListBox
  - Simple data binding or Items.Add()

- [ ] Update `BuildVisualizerPackage.cs`:
  - Pass DTE2 to tool window if needed

**✓ Checkpoint:** Open a solution, open the tool window, see all project names listed!

---

## Increment 3: Show Build Status with Colors (Runnable ✓)

**Goal:** Update project colors in real-time as builds happen

**Verification:** Open tool window, start a build, watch projects turn yellow → green/red

### Tasks
- [ ] Create `Resources/Colors.cs`:
  - Define color constants as `SolidColorBrush`:
    - Gray (#808080) - NotBuilt
    - Yellow (#FFA500) - Building  
    - Green (#4CAF50) - Success
    - Red (#F44336) - Failed
    - Blue (#2196F3) - Skipped

- [ ] Update `Models/ProjectInfo.cs`:
  - Add `INotifyPropertyChanged` interface
  - Add `Status` property with change notification
  - Add computed `StatusColor` property

- [ ] Create `Services/BuildEventService.cs`:
  - Subscribe to `DTE.Events.BuildEvents`
  - `OnBuildBegin`: Set all projects to Building (Yellow)
  - `OnBuildProjConfigBegin`: Set specific project to Building
  - `OnBuildProjConfigDone`: Set Success (Green) or Failed (Red) based on result
  - `OnBuildDone`: Final cleanup
  - Expose `ProjectStatusChanged` event

- [ ] Update `ToolWindow/BuildVisualizerControl.xaml`:
  - Update ItemTemplate to bind Background to StatusColor
  - Show status text alongside project name

- [ ] Update `ToolWindow/BuildVisualizerControl.xaml.cs`:
  - Subscribe to BuildEventService events
  - Use `Dispatcher.Invoke` to update UI on main thread
  - Update ProjectInfo Status when events fire

- [ ] Update `BuildVisualizerPackage.cs`:
  - Initialize BuildEventService in InitializeAsync
  - Pass to tool window

**✓ Checkpoint:** Start a build, watch projects change color in real-time. Failed projects show red!

---

## Increment 4: Show Project Dependencies (Runnable ✓)

**Goal:** Display which projects depend on which, in a flat list format

**Verification:** See "ProjectA → depends on → ProjectB, ProjectC" type display

### Tasks
- [ ] Update `Models/ProjectInfo.cs`:
  - Add `List<string> Dependencies` property
  - Add `List<string> Dependents` property

- [ ] Update `Services/SolutionService.cs`:
  - Method `ParseProjectReferences()`:
    - For each project, get VSProject references
    - Build dependency lists
    - Return updated ProjectInfo objects

- [ ] Update `ToolWindow/BuildVisualizerControl.xaml`:
  - Update ItemTemplate to show dependencies as text
  - Example: "ProjectName [→ Dep1, Dep2]"
  - Or use TreeView with expandable dependencies

- [ ] Update `ToolWindow/BuildVisualizerControl.xaml.cs`:
  - Call ParseProjectReferences on solution load
  - Refresh when projects change

**✓ Checkpoint:** Open tool window, see dependencies listed for each project!

---

## Increment 5: Tree/Hierarchy View (Runnable ✓)

**Goal:** Show projects in a hierarchical dependency tree

**Verification:** See collapsible tree showing dependency relationships

### Tasks
- [ ] Create `ViewModels/ProjectNodeViewModel.cs`:
  - Wrap ProjectInfo
  - `ObservableCollection<ProjectNodeViewModel> Children`
  - Properties for data binding
  - INotifyPropertyChanged for Status updates

- [ ] Create `Services/DependencyGraphBuilder.cs`:
  - Method `BuildHierarchy()`:
    - Find root projects (no dependents)
    - Recursively build tree structure
    - Return collection of root ViewModels

- [ ] Update `ToolWindow/BuildVisualizerControl.xaml`:
  - Replace ListBox with TreeView
  - HierarchicalDataTemplate:
    - Bind to Children
    - Show project name + status color
    - Expandable items

- [ ] Update `ToolWindow/BuildVisualizerControl.xaml.cs`:
  - Use DependencyGraphBuilder to create hierarchy
  - Bind TreeView to root nodes

**✓ Checkpoint:** See projects in a tree structure. Expand nodes to see dependencies!

---

## Increment 6: Graph Canvas Visualization (Runnable ✓)

**Goal:** Display projects as nodes on a Canvas with connecting lines

**Verification:** See boxes representing projects, connected by arrows showing dependencies

### Tasks
- [ ] Create `Layout/GraphLayoutEngine.cs`:
  - Method `CalculateLayout(List<ProjectNodeViewModel>)`:
    - Implement simple layered layout
    - Assign X, Y positions to each node
    - Layer 0: projects with no dependencies
    - Layer N: projects depending on layer N-1

- [ ] Update `ViewModels/ProjectNodeViewModel.cs`:
  - Add `X`, `Y`, `Width`, `Height` properties for position

- [ ] Create `Controls/ProjectNodeControl.xaml`:
  - Border with rounded corners
  - Background bound to StatusColor
  - TextBlock for project name
  - Fixed size (e.g., 120x60)

- [ ] Create `Controls/DependencyLineControl.cs` (code-only):
  - Custom control that draws a Line or Path
  - Properties: FromX, FromY, ToX, ToY
  - Draw arrow head at end

- [ ] Update `ToolWindow/BuildVisualizerControl.xaml`:
  - Replace TreeView with Canvas
  - ItemsControl with Canvas as ItemsPanel:
    - Canvas.Left/Top bound to X/Y
    - ItemTemplate: ProjectNodeControl
  - Draw lines in Canvas for dependencies

- [ ] Update `ToolWindow/BuildVisualizerControl.xaml.cs`:
  - Run GraphLayoutEngine to position nodes
  - Create line elements for each dependency
  - Update lines when layout changes

**✓ Checkpoint:** See a graph with boxes and arrows! Projects positioned automatically!

---

## Increment 7: Improved Layout Algorithm (Runnable ✓)

**Goal:** Better positioning - minimize line crossings, center alignment

**Verification:** Cleaner graph layout, especially for complex dependencies

### Tasks
- [ ] Update `Layout/GraphLayoutEngine.cs`:
  - Implement layer-based layout:
    - Topological sort for layer assignment
    - Barycenter heuristic for horizontal positioning
    - Minimize edge crossings
  - Add spacing parameters
  - Handle multiple disconnected graphs

- [ ] Add automatic Canvas sizing:
  - Calculate required Canvas size based on positions
  - Add margins/padding

**✓ Checkpoint:** Open a complex solution - see improved, readable graph layout!

---

## Increment 8: User Interactions (Runnable ✓)

**Goal:** Click nodes to interact, zoom/pan the graph

**Verification:** Double-click a project to see it in Solution Explorer, zoom with mouse wheel

### Tasks
- [ ] Add project node interactions:
  - [ ] Double-click → Navigate to project in Solution Explorer
  - [ ] Right-click context menu:
    - Build Project
    - Rebuild Project  
    - Show in Solution Explorer
  - [ ] Hover → Show tooltip with full path and details

- [ ] Add Canvas interactions:
  - [ ] Mouse wheel → Zoom in/out (ScaleTransform)
  - [ ] Click + drag → Pan the canvas (TranslateTransform)
  - [ ] "Fit to Window" button → Reset zoom to show all
  - [ ] "Refresh" button → Reload solution and rebuild graph

- [ ] Create `Commands/` helper methods:
  - `BuildProject(string projectUniqueName)`
  - `ShowInSolutionExplorer(string projectUniqueName)`

**✓ Checkpoint:** Interact with the graph - click, zoom, build individual projects!

---

## Increment 9: Polish & Edge Cases (Runnable ✓)

**Goal:** Handle solution changes, improve performance, add finishing touches

**Verification:** Add/remove projects dynamically, handles large solutions smoothly

### Tasks
- [ ] Handle solution events:
  - [ ] Solution opened → Load and display graph
  - [ ] Solution closed → Clear tool window (show "Open a solution" message)
  - [ ] Project added → Add to graph dynamically
  - [ ] Project removed → Remove from graph dynamically
  - [ ] Project references changed → Rebuild graph

- [ ] Performance optimizations:
  - [ ] Virtualization for large solutions (50+ projects)
  - [ ] Incremental updates instead of full rebuild
  - [ ] Cache project information
  - [ ] Async loading with progress indicator

- [ ] Visual polish:
  - [ ] Smooth animations for status color changes
  - [ ] Highlight build path (projects being built)
  - [ ] Show build errors count on failed nodes
  - [ ] Theme support (light/dark)
  - [ ] Custom icons for project types

- [ ] Error handling:
  - [ ] Handle circular dependencies (mark with special color/icon)
  - [ ] Handle projects that fail to load
  - [ ] Show errors in output window
  - [ ] Graceful degradation

**✓ Checkpoint:** Robust extension that handles all edge cases smoothly!

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

- [ ] Increment 1: Basic Tool Window ✓ Runnable
- [ ] Increment 2: Display Project List ✓ Runnable
- [ ] Increment 3: Build Status with Colors ✓ Runnable
- [ ] Increment 4: Show Dependencies ✓ Runnable
- [ ] Increment 5: Tree/Hierarchy View ✓ Runnable
- [ ] Increment 6: Graph Canvas Visualization ✓ Runnable
- [ ] Increment 7: Improved Layout ✓ Runnable
- [ ] Increment 8: User Interactions ✓ Runnable
- [ ] Increment 9: Polish & Edge Cases ✓ Runnable
- [ ] Increment 10: Documentation & Testing ✓ Ready to Ship!

---

## 📁 File Structure (Final)

```
src/BuildVisualizer/
├── BuildVisualizerPackage.cs          # Main package entry point
├── source.extension.vsixmanifest      # VSIX manifest
├── BuildVisualizer.csproj             # Project file
├── VSCommandTable.vsct                # Command definitions
│
├── Commands/
│   └── ShowToolWindowCommand.cs       # Menu command to show tool window
│
├── ToolWindow/
│   ├── BuildVisualizerToolWindow.cs   # Tool window host (ToolWindowPane)
│   ├── BuildVisualizerControl.xaml    # Main UI (Canvas-based graph)
│   └── BuildVisualizerControl.xaml.cs # Code-behind with event handling
│
├── Models/
│   ├── ProjectInfo.cs                 # Project data model
│   └── BuildStatus.cs                 # Status enum
│
├── ViewModels/
│   └── ProjectNodeViewModel.cs        # Node view model with positioning
│
├── Services/
│   ├── SolutionService.cs             # Solution/project parsing
│   ├── BuildEventService.cs           # Build event handling
│   └── DependencyGraphBuilder.cs      # Build hierarchy from dependencies
│
├── Layout/
│   └── GraphLayoutEngine.cs           # Node positioning algorithm
│
├── Controls/
│   ├── ProjectNodeControl.xaml        # Node visual (colored rectangle)
│   └── DependencyLineControl.cs       # Line/arrow drawing
│
├── Resources/
│   ├── Colors.cs                      # Status color constants
│   └── Icons/                         # Extension icons
│
└── Properties/
    └── AssemblyInfo.cs
```

---

## 🔑 Key VS SDK APIs by Increment

| Increment | APIs Needed |
|-----------|-------------|
| **1** | `ToolWindowPane`, `AsyncPackage`, `OleMenuCommand` |
| **2** | `DTE2`, `Solution.Projects`, `Project` |
| **3** | `BuildEvents`, `INotifyPropertyChanged`, `Dispatcher` |
| **4** | `VSProject`, `References`, project hierarchy navigation |
| **5** | WPF `TreeView`, `HierarchicalDataTemplate` |
| **6** | WPF `Canvas`, `ItemsControl`, `Path` geometry |
| **7** | Graph algorithms (topological sort) |
| **8** | `IVsSolutionBuildManager`, `DTE.ExecuteCommand` |
| **9** | `SolutionEvents`, `IVsHierarchyEvents` |
| **10** | Testing & packaging |

---

## 💡 Key Implementation Notes

### Thread Safety
- Build events fire on background threads → Use `JoinableTaskFactory.SwitchToMainThreadAsync()` or `Dispatcher.Invoke()` for UI updates
- Always check `ThreadHelper.ThrowIfNotOnUIThread()` when calling DTE

### Performance Tips
- Cache project information after parsing
- Use incremental updates for build status (don't rebuild entire graph)
- For 50+ projects, consider Canvas virtualization or Level-of-Detail rendering
- Debounce rapid status changes

### Common Pitfalls to Avoid
- ❌ Don't hold references to DTE objects long-term (can cause memory leaks)
- ❌ Don't block UI thread while parsing projects
- ❌ Don't forget to unsubscribe from events in Dispose()
- ✅ Use `IAsyncServiceProvider` for getting services
- ✅ Test with various solution sizes regularly

### Useful VS Commands
```csharp
// Show in Solution Explorer
DTE.ExecuteCommand("View.SolutionExplorer");
DTE.ExecuteCommand("Project.OpenSolution", projectPath);

// Build commands  
DTE.ExecuteCommand("Build.BuildSelection");
DTE.ExecuteCommand("Build.RebuildSelection");
```

---

## 🚀 Quick Start Guide

1. **Start with Increment 1** - Get the tool window showing
2. **Test after each increment** - Don't move forward until current increment works
3. **Commit after each increment** - Easy to roll back if needed
4. **Keep it simple first** - Polish can come later in Increment 9
5. **Use Debug output** - Add logging to understand build events flow

### First Session Goals (2-3 hours)
- Complete Increment 1 (empty window)
- Complete Increment 2 (project list)
- Start Increment 3 (build status)

### Second Session Goals (3-4 hours)
- Complete Increment 3 (colors working)
- Complete Increment 4 (dependencies shown)
- Try Increment 5 (tree view)

---

*Last updated: 2024*
