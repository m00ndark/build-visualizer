# Features
- [ ] 1. Add a timeline view (like in Chrome DevTools Network view)
- [ ] 2. Add a "Build History" view, showing previous builds and their durations, errors/warnings/information counts, etc.

# Improvements

- [x] 1. Load projects when window is opened
- [ ] 2. Add a metadata view (popup or sidebar?) for a hovered node
- [ ] ~~3. Show which project the build was started for, if not entire solution?~~
- [x] 4. Show overall build start time and duration, continuously updated
- [ ] 5. Show error/warning/information counts for each project
- [x] 6. Sort dependency lists
- [x] 7. Continuously update project build duration
- [x] 8. Consider build duration with lower resolution
- [ ] 9. Add settings, available from "toolbar":
	- [ ] Focus on build start?
	- [ ] List view column selection
- [x] 10. Update project info when a project dependency is added/removed
- [x] 11. Make list view rows selectable and add clean/build/rebuild buttons to "toolbar" (disabled in graph view)
- [x] 12. Add context menu to graph nodes and list rows:
	- [x] Clean Project
	- [x] Build Project
	- [x] Rebuild Project
- [x] 13. Add "Reveal in Solution Explorer" to context menus
- [ ] 14. Add column-based layout option for graph view (toggle between row/column)
- [ ] 15. Store user settings/choices to remember state between starts
- [ ] 16. Add additional columns to list view
	- [ ] Configuration
	- [ ] Platform
	- [ ] Error count
	- [ ] Warning count
	- [ ] Information count
- [ ] 17. Add a Cancel build toolbar button

# Bugs

- [x] 1. Test projects are often tagged as "Executable". It would be nice if it said e.g. "Test Library".
