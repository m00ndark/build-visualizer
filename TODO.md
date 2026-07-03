# Features
- [ ] 1. Add a timeline view (like in Chrome DevTools Network view)
- [ ] 2. Add a "Build History" view, showing previous builds and their durations, errors/warnings/information counts, etc.

# Improvements

- [x] 1. Load projects when window is opened
- [x] 2. Add a metadata view (popup or sidebar?) for a hovered node
- [ ] ~~3. Show which project the build was started for, if not entire solution?~~
	- [ ] Revisit this - investigate if there is any way to find out for which project the build was started (could then be visualized in graph view)
- [x] 4. Show overall build start time and duration, continuously updated
- [x] 5. Show error/warning/information counts for each project and totals in the toolbar
- [x] 6. Sort dependency lists
- [x] 7. Continuously update project build duration
- [x] 8. Consider build duration with lower resolution
- [x] 9. Add settings, available from "toolbar":
	- [x] Focus on build start?
	- [x] List view column selection
- [x] 10. Update project info when a project dependency is added/removed
- [x] 11. Make list view rows selectable and add clean/build/rebuild buttons to "toolbar" (disabled in graph view)
- [x] 12. Add context menu to graph nodes and list rows:
	- [x] Clean Project
	- [x] Build Project
	- [x] Rebuild Project
- [x] 13. Add "Reveal in Solution Explorer" to context menus
- [ ] 14. Add column-based layout option for graph view (toggle between row/column)
- [x] 15. Store user settings/choices to remember state between starts
- [x] 16. Add additional columns to list view
	- [x] Configuration
	- [x] Platform
	- [x] Error count
	- [x] Warning count
	- [x] Information count
- [x] 17. Add a Cancel build toolbar button
- [x] 18. Make it possible to switch between showing all dependencies and only direct dependencies (like it is currently) in graph view (only?)
- [x] 19. Make row backgrounds in graph view darker
- [x] 20. Store chosen view

# Bugs

- [x] 1. Test projects are often tagged as "Executable". It would be nice if it said e.g. "Test Library".
- [x] 2. Build/rebuild/clean solution toolbar buttons are enabled when a build is ongoing
- [x] 3. Overall build status at the top of the window showed "Started at 00:00:00 and lasted -35791394 minutes and -8 seconds."
- [x] 4. Overall build status at the top of the window did not update when building, showed "No build information available."
