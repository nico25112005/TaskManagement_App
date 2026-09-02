# Implementierungsplan: TaskManagement_App – Prio-1-Features

## Modus
**D – Referenz + Eigenbau**: Ich baue 1-5 als Referenz-Implementierung in `reference/nova-implementation`. Du baust die gleiche Feature-Liste selbst in `master` (oder einem Feature-Branch) und cherry-pickst am Ende Sachen, die du übernehmen willst.

## Branch-Strategie
- `master` bleibt wie er ist
- `reference/nova-implementation` ist mein Branch – wird nicht gemerged
- Du arbeitest auf Feature-Branches: `feature/calendar-events`, `feature/split-tasks`, etc.
- Convention: jeder Commit mit `feat:`, `fix:`, `chore:`-Prefix

## Prio-1-Features (Reihenfolge ist wichtig – jedes baut auf dem vorigen auf)

1. **CalendarEvent-Datenmodell + Persistenz** (Fundament)
2. **Fix-Termine + Freizeit-Blöcke UI** (Kalender-Tab)
3. **SplitTasks-Algorithmus** (braucht CalendarEvent-Daten)
4. **Timer mit Auto-Pause + Tracking** (unabhängig, aber UI-Integration in Home)
5. **Drag-and-Drop verdrahten** (braucht 2 als visuelles Feedback)

## Reihenfolge der Commits in meinem Referenz-Branch

```
docs: implementation plan and feature roadmap
feat: CalendarEvent data model + JSON persistence
feat: CalendarViewModel + ObservableCollection-based week view
feat: split tasks algorithm with LPTF + best-fit decreasing
feat: timer with adaptive pause logic
feat: drag-and-drop wiring in WeekPlan
test: unit tests for SplitTasks (edge cases: 1h tasks, >24h, all days full)
docs: usage guide for the new features
```

## Nicht-Ziele (bewusst weggelassen für jetzt)
- ML/Pattern-Lernen (Phase 2)
- Burnout-Radar (Phase 2)
- Onboarding-Interview (Phase 2)
- Cloud-Sync, Mobile, Web

## Geschätzter Aufwand
- Mein Referenz-Branch: 4-5 Commits, 800-1200 Zeilen zusätzlich
- Dein Eigenbau (parallel): gleicher Scope, 2-3 Wochen mit Review

## Nächster Schritt
Ich lege den Branch an und baue Feature 1. Du schaust dir parallel an, was ich tue, und überlegst dir, wie du es selbst bauen würdest.
