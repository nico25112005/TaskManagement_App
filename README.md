# Task Management

A planner that respects your calendar and your time. Built with C# / WPF / .NET 6.

Instead of just being another to-do list, this app **looks at when you're actually free** (your fixed appointments, work hours, free time, sleep) and places your tasks into the gaps that fit. Miss a deadline and it warns you. Skip a focus block and the timer keeps track.

## Features

### 📅 Calendar-aware planning
- **Fixed appointments** (Uni, Arzt) — hard blocks, no tasks placed here
- **Work hours** — the slots where the planner is allowed to put tasks
- **Free time** — hard blocks (sport, family)
- **Sleep** — hard block, typically whole-day

The planner never schedules a task on top of a fixed appointment or during sleep. If your calendar shows you have 3.5h free on Tuesday, you won't get 8h of work pushed there.

### 🪓 Split-aware task distribution
A 12-hour task doesn't have to live on a single day. If you only have 2h free on Wednesday, the planner puts 2h there and the remaining 10h elsewhere — automatically.

Tasks get a `[part 2/4]` suffix when split, so you always know which chunk you're working on.

### ⏱ Focus timer with adaptive breaks
- Default 50-minute focus block (Pomodoro-style)
- 5-minute short break between blocks
- 15-minute long break every 3rd block
- All durations adjustable in Settings
- When the timer runs out, the hours you spent are auto-booked back to the task

### 📆 Google-Calendar-style week view
- 7-day week starting Monday
- 24-hour grid (0:00 – 23:00) with events positioned exactly in their hour slot
- Multi-hour events span their full duration
- **Now-indicator**: red 1px line on today's column at the current time, so you always see where you are
- Empty-state when nothing is scheduled

### 📋 Home dashboard
- **Today** bucket: tasks planned today, due today, or overdue
- **Upcoming** bucket: everything else, sorted by deadline
- **Recently done**: last 10 completed tasks with Undo
- Load indicator ("5.5h / 3.5h") turns red when you're overloaded

### 🛠 Settings (live, persisted)
- Max hours per day (slider, 0.5h – 12h)
- Plan horizon (1 – 30 days)
- Work-day boundaries (e.g. 09:00 – 18:00)
- All timer durations
- Data folder + JSON export of everything (tasks, events, settings)

### 🔧 Editing
- Double-click a todo → modal edit dialog (description, hours, importance, delivery date)
- "✓ Done" button per todo → moves to Done archive with timestamp
- "↶ Undo" on any done item
- "✕" remove button on every calendar event (right-click still works)

### ⌨️ Keyboard shortcuts
- **Ctrl+1** — Home
- **Ctrl+2** — To-do
- **Ctrl+3** — Plan
- **Ctrl+4** — Settings
- **Ctrl+N** — focus new-task input (when on To-do tab)
- **Ctrl+Shift+N** — Quick-capture overlay: create a task from any page

### 📊 Status bar
The status bar at the bottom is always visible and shows:
- open tasks count
- planned hours for today
- completed focus blocks today
- a short hint or overload warning

### 🎯 Drag-and-drop
- Drag any task chip in the week plan to a different day
- Target day highlights while hovering
- Flashes yellow on successful drop

## Architecture

```
TaskManagement/
  App.xaml / App.xaml.cs          — WPF app entry point
  MainWindow.xaml / .cs           — UI shell + page routing + planner UI
  EditTaskWindow.xaml / .cs       — Modal edit dialog for tasks
  Task.cs                          — Task model (Hours, Description, Delivery, Importance, Done)
  Tasks.cs                         — Static container, JSON I/O, MarkDone/Undone
  CalendarEvent.cs                 — Event model (Fixed/Work/Free/Sleep) + recurrence
  CalendarEvents.cs                — Static container, JSON I/O, available-hours calc
  TaskSorter.cs                    — Split-aware distribution algorithm
  Week.cs                          — Day bucket with Date + PlanedHours
  HomeTodoViewModel.cs             — MVVM for Home tab buckets
  TimerViewModel.cs                — Focus-timer state + PropertyChanged
  FocusSession.cs                  — Block/break state machine
  Settings.cs                      — User preferences + JSON persistence
  TaskWidthConverter.cs            — XAML value converter (legacy)
```

No external dependencies beyond Newtonsoft.Json. Data is persisted as plain JSON files (`todos.json`, `done.json`, `calendar_events.json`, `settings.json`) next to the executable — easy to back up, easy to inspect, easy to migrate.

## Getting started

### Run from source

Requires .NET 6 SDK on Windows:

```bash
dotnet build
dotnet run --project TaskManagement
```

### Data location

JSON files live next to the executable. Use **Settings → Open folder** to jump there in Explorer.

## Roadmap

Done (in master):
- ✅ Calendar events (4 types) + JSON persistence
- ✅ Split-aware, calendar-aware task distribution
- ✅ Focus timer with adaptive breaks
- ✅ Drag-and-drop in week plan with hover highlight
- ✅ Google-Calendar-style week view (0–24h grid, Now-indicator, Empty-state)
- ✅ Settings tab with planner/timer controls + JSON export
- ✅ Edit dialog for tasks
- ✅ Task history / Done-archive with Undo
- ✅ Window icon, dynamic title, keyboard shortcuts
- ✅ Quick-capture (Ctrl+Shift+N)
- ✅ Focus-block counter
- ✅ Status bar

Coming next:
- Sub-tasks / checklists inside a task
- Tray-icon minimize
- Statistics dashboard
- Dark mode (toggle exists, theme is the only thing missing)

## License

MIT
