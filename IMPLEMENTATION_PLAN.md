# Build Visualizer - Implementation Plan

## Overview
This implementation plan outlines the development of features for the Build Visualizer Visual Studio extension. Each increment is designed to be independently runnable and testable.

---

## Increment 1: Solution Event Handling & UI Cleanup

### Description
Improve solution/project lifecycle management and remove the tree tab view.

### Tasks
- [x] Remove tree tab from tool window UI
  - [x] Remove tree tab XAML definition from tool window
  - [x] Remove tree-related ViewModel properties and logic
  - [x] Remove tree-related event handlers
  - [x] Update tab control to remove tree tab reference
  - [x] Clean up unused tree view code files (if any)

- [x] Implement solution event handlers
  - [x] Subscribe to `IVsSolutionEvents` for solution lifecycle
  - [x] Handle `OnAfterOpenSolution` event (solution loaded)
  - [x] Handle `OnBeforeCloseSolution` event (solution closed)
  - [x] Handle `OnAfterLoadProject` event (project added)
  - [x] Handle `OnBeforeUnloadProject` event (project removed)
  - [x] Clear visualizations when solution closes
  - [x] Reload/refresh visualizations when solution opens
  - [x] Dynamically add project to visualization when added
  - [x] Dynamically remove project from visualization when removed

### Testing Criteria
- [x] Tool window opens with only list and graph tabs visible
- [x] Opening a solution loads and displays projects correctly
- [x] Closing a solution clears the visualization
- [x] Adding a project to solution updates the visualization
- [x] Removing a project from solution updates the visualization
- [x] No crashes or exceptions during solution lifecycle operations

---

## Increment 2: Graph View - Node Sizing & Remove Dependency Lines

### Description
Remove visual dependency lines and adjust node sizing to fit content properly.

### Tasks
- [ ] Remove dependency line rendering
  - [ ] Remove line drawing logic from graph view
  - [ ] Remove line-related properties from graph ViewModel
  - [ ] Keep the dependency calculation logic (needed for grouping)
  - [ ] Clean up unused visual elements related to lines

- [ ] Implement dynamic node sizing
  - [ ] Calculate node width based on project name text length
  - [ ] Set fixed node height for consistency
  - [ ] Add thin padding around text (e.g., 8-12 pixels)
  - [ ] Ensure text is never broken/truncated
  - [ ] Update node template to use dynamic sizing
  - [ ] Test with short and long project names

### Testing Criteria
- [ ] No dependency lines are visible in graph view
- [ ] Node width adjusts to fit project name without text wrapping
- [ ] All nodes have consistent height
- [ ] Padding around text looks balanced
- [ ] Long project names display correctly without truncation
- [ ] Nodes from dependency groups still organized in rows

---

## Increment 3: Graph View - Responsive Layout with Row Grouping

### Description
Implement responsive layout with row wrapping and visual grouping indicators.

### Tasks
- [ ] Implement responsive row layout
  - [ ] Calculate available width in graph view panel
  - [ ] Implement logic to determine when to break a row
  - [ ] Maintain consistent spacing between nodes in a row
  - [ ] Keep nodes left-aligned within each row
  - [ ] Wrap rows when there's insufficient space
  - [ ] Ensure wrapped row segments continue underneath

- [ ] Add visual grouping for dependency rows
  - [ ] Create subtle background element for each dependency group
  - [ ] Ensure background spans across wrapped row segments
  - [ ] Use semi-transparent background to not obscure nodes
  - [ ] Test different colors/opacities for readability
  - [ ] Ensure backgrounds don't overlap inappropriately

- [ ] Handle window resize events
  - [ ] Subscribe to window size change events
  - [ ] Recalculate row breaks on resize
  - [ ] Animate/smooth transition (optional)
  - [ ] Maintain scroll position during resize

### Testing Criteria
- [ ] Nodes are left-aligned with consistent spacing
- [ ] Rows wrap when window width decreases
- [ ] Row wrapping unwraps when window width increases
- [ ] Background grouping clearly shows which nodes belong together
- [ ] Wrapped rows have continuous background across segments
- [ ] Resizing window smoothly adjusts layout
- [ ] No overlapping or misaligned nodes

---

## Increment 4: Context Menu - Build Operations

### Description
Add context menu to graph nodes and list rows with build/rebuild operations.

### Tasks
- [ ] Create context menu for graph view
  - [ ] Define context menu XAML for graph nodes
  - [ ] Add "Build Project" menu item
  - [ ] Add "Rebuild Project" menu item
  - [ ] Bind menu items to ViewModel commands
  - [ ] Ensure context menu shows on right-click

- [ ] Create context menu for list view
  - [ ] Define context menu XAML for list rows
  - [ ] Add "Build Project" menu item
  - [ ] Add "Rebuild Project" menu item
  - [ ] Bind menu items to ViewModel commands
  - [ ] Ensure context menu shows on right-click

- [ ] Implement build operation commands
  - [ ] Create `BuildProjectCommand` in ViewModel
  - [ ] Create `RebuildProjectCommand` in ViewModel
  - [ ] Use `IVsSolutionBuildManager` to trigger build
  - [ ] Use `IVsSolutionBuildManager` to trigger rebuild
  - [ ] Pass correct project context to build manager
  - [ ] Handle build errors gracefully
  - [ ] Show feedback/status during build operation

### Testing Criteria
- [ ] Right-clicking a graph node shows context menu
- [ ] Right-clicking a list row shows context menu
- [ ] "Build Project" successfully builds the selected project
- [ ] "Rebuild Project" successfully rebuilds the selected project
- [ ] Build status updates in real-time during operation
- [ ] Error handling works for invalid/unloaded projects
- [ ] Multiple projects can be built sequentially

---

## Increment 5: Context Menu - Solution Explorer Integration

### Description
Add "Show in Solution Explorer" functionality to context menus.

### Tasks
- [ ] Add menu item to context menus
  - [ ] Add "Show in Solution Explorer" to graph context menu
  - [ ] Add "Show in Solution Explorer" to list context menu
  - [ ] Add separator before this menu item for visual grouping

- [ ] Implement Solution Explorer navigation
  - [ ] Create `ShowInSolutionExplorerCommand` in ViewModel
  - [ ] Get `IVsUIHierarchy` for the project
  - [ ] Get `IVsUIHierarchyWindow` for Solution Explorer
  - [ ] Use `ExpandItem` to expand project in Solution Explorer
  - [ ] Bring Solution Explorer window to front/focus
  - [ ] Select the project node in Solution Explorer

### Testing Criteria
- [ ] "Show in Solution Explorer" appears in both context menus
- [ ] Clicking the menu item navigates to Solution Explorer
- [ ] Solution Explorer window gets focus
- [ ] Correct project is selected and expanded
- [ ] Works for projects at different nesting levels
- [ ] Handles unloaded projects gracefully

---

## Increment 6: Visual Studio Theme Support

### Description
Integrate with Visual Studio theming system to support light, dark, and custom themes.

### Tasks
- [ ] Reference Visual Studio theme colors
  - [ ] Add reference to `Microsoft.VisualStudio.Shell` theming APIs
  - [ ] Use `EnvironmentColors` for standard UI elements
  - [ ] Use `ThemedDialogColors` where appropriate
  - [ ] Create custom theme color resources if needed

- [ ] Update tool window styling
  - [ ] Apply theme-aware background colors
  - [ ] Apply theme-aware foreground/text colors
  - [ ] Apply theme-aware border colors
  - [ ] Update node styling to use theme colors
  - [ ] Update row grouping backgrounds to use theme colors

- [ ] Update build status colors
  - [ ] Ensure green (success) is visible in all themes
  - [ ] Ensure red (failure) is visible in all themes
  - [ ] Ensure yellow (building) is visible in all themes
  - [ ] Test color contrast in light and dark themes
  - [ ] Adjust color opacity if needed for readability

- [ ] Handle theme change events
  - [ ] Subscribe to `VSColorTheme.ThemeChanged` event
  - [ ] Refresh UI colors when theme changes
  - [ ] Update cached color resources
  - [ ] Test switching themes while tool window is open

### Testing Criteria
- [ ] Tool window looks native in light theme
- [ ] Tool window looks native in dark theme
- [ ] Tool window looks native in blue theme
- [ ] Build status colors are clearly visible in all themes
- [ ] Switching themes updates the tool window immediately
- [ ] No hard-coded colors remain in XAML/code
- [ ] Row grouping backgrounds adapt to theme
- [ ] Context menus match VS theme

---

## Increment 7: Column-Based Layout Option

### Description
Add ability to switch between row-based and column-based layout in graph view.

### Tasks
- [ ] Add layout toggle UI
  - [ ] Add toggle button/control to graph view toolbar
  - [ ] Add icons for row/column layout modes
  - [ ] Bind toggle to ViewModel property
  - [ ] Persist layout preference (optional)

- [ ] Implement column-based layout algorithm
  - [ ] Create layout logic for vertical columns
  - [ ] Calculate column breaks based on available height
  - [ ] Maintain dependency grouping in column layout
  - [ ] Keep nodes top-aligned within columns
  - [ ] Add consistent spacing between nodes
  - [ ] Wrap columns when there's insufficient height

- [ ] Update visual grouping for columns
  - [ ] Apply background grouping vertically
  - [ ] Ensure backgrounds work for wrapped columns
  - [ ] Maintain visual consistency with row layout

- [ ] Handle layout switching
  - [ ] Smoothly transition between layouts
  - [ ] Recalculate positions when switching
  - [ ] Preserve scroll position if possible
  - [ ] Handle window resize in both layouts

### Testing Criteria
- [ ] Toggle button switches between row and column layouts
- [ ] Column layout organizes nodes vertically
- [ ] Column layout maintains dependency grouping
- [ ] Columns wrap when window height is insufficient
- [ ] Switching layouts works smoothly without errors
- [ ] Both layouts respond to window resizing
- [ ] Visual grouping works in both layouts
- [ ] Layout preference persists across sessions (if implemented)

---

## Notes & Considerations

### Performance
- Ensure layout calculations are efficient for solutions with 100+ projects
- Consider using virtualization for large project counts
- Debounce window resize events to avoid excessive recalculations

### Accessibility
- Ensure sufficient color contrast in all themes
- Support keyboard navigation in context menus
- Consider screen reader compatibility

### Future Enhancements
- Add filtering options (show only failed projects, etc.)
- Add search/find functionality
- Add zoom controls for graph view
- Add export visualization as image
- Add project dependency statistics
- Add build time metrics

### Dependencies
- Ensure minimum VS SDK version supports all required APIs
- Test on VS 2022 and VS 2019 if backwards compatibility needed
- Document any extension dependencies

---

## Progress Tracking

**Overall Progress:**
- [x] Increment 1: Solution Event Handling & UI Cleanup
- [ ] Increment 2: Graph View - Node Sizing & Remove Dependency Lines
- [ ] Increment 3: Graph View - Responsive Layout with Row Grouping
- [ ] Increment 4: Context Menu - Build Operations
- [ ] Increment 5: Context Menu - Solution Explorer Integration
- [ ] Increment 6: Visual Studio Theme Support
- [ ] Increment 7: Column-Based Layout Option

**Current Increment:** Increment 1 - Completed

**Last Updated:** 2024
