# TaskManagement App — Cross-Platform Rewrite Plan

## Status: Planning Phase
## Branch: `react-crossplatform`
## Date: 2026-09-05

---

## Warum ein Rewrite?

Die bestehende WPF/.NET 6 App funktioniert, aber:
- **Nicht cross-platform** — nur Windows
- **Keine Tests möglich** — keine .NET SDK in der Sandbox, jeder Build erfordert Visual Studio
- **XAML-Compiler-Fallen** — `Label.Resources`, `Button.Resources` etc. brechen lautlos den Build
- **Kein Mobile** — Android/iOS nicht möglich mit WPF

## Ziel

Eine Task-Management-App, die:
- Auf **Windows, Linux, macOS, Android** läuft
- Eine **clean, modern UI** hat (bestehende Farbpalette)
- Alle Features der WPF-App übernimmt
- In der **Sandbox testbar** ist (Node.js/TypeScript verfügbar)
- Schnelle Iteration ohne Build-Locks

---

## Framework-Wahl: React + Vite + Capacitor

| Option | Pro | Contra | Entscheidung |
|--------|-----|--------|---------------|
| **Tauri 2 + React** | Kleinste Binaries, Rust-Backend, Desktop+Mobile | Mobile noch jung, WebView-Inkonsistenz, Rust-Lernkurve | ❌ |
| **Flutter** | Beste Mobile-Performance, eigene Rendering-Engine | Dart statt TS, kein Web-Target, Sandbox kann nicht kompilieren | ❌ |
| **React Native** | Native Mobile UI, großes Ökosystem | Desktop nur via Electron-Wrapper, zusätzliche Komplexität | ❌ |
| **React + Vite + Capacitor** | Web-First, testbar in Sandbox, Capacitor für Android/Desktop, größtes Ökosystem | WebView-basiert (keine nativen Komponenten) | ✅ |

### Warum React + Vite + Capacitor?

1. **In der Sandbox testbar**: Node.js + TypeScript sind verfügbar, `npm run dev` läuft sofort
2. **Web-First**: Die App läuft als Web-App (im Browser), dann gewrappt für:
   - **Android** via Capacitor (Play Store)
   - **Desktop** (Windows/Linux/macOS) via Capacitor oder Tauri-Electron-Wrapper
3. **Ein Codebase, eine Sprache**: TypeScript für alles
4. **Schnelle Iteration**: Hot Reload, keine Build-Locks, keine XAML-Compiler-Fallen
5. **Größtes Ökosystem**: npm-Pakete für Drag-and-Drop, Date-picker, Charts etc.
6. **Clean UI**: Tailwind CSS / CSS Modules für modernes Styling

---

## Technologie-Stack

| Schicht | Technologie |
|---------|-------------|
| **Frontend** | React 18 + TypeScript |
| **Build** | Vite 5 |
| **Styling** | Tailwind CSS (bestehende Farbpalette) |
| **State** | Zustand (lightweight, kein Boilerplate) |
| **Drag-and-Drop** | @dnd-kit/core (modern, accessible, flexibel) |
| **Date-Picker** | react-datepicker oder native HTML5 |
| **Icons** | lucide-react (clean, modern) |
| **Mobile/Desktop** | Capacitor 6 (Android) + optional Tauri/Electron (Desktop) |
| **Storage** | localStorage (Web) + Capacitor Preferences (Mobile) |
| **Testing** | Vitest + Testing Library |

---

## Farbpalette (bestehend)

```css
--color-primary: #5E97D9;     /* Blau — Akzent, Buttons, aktive Tabs */
--color-danger: #e35d48;      /* Rot — Delete, Warnung */
--color-highlight: #FFC941;   /* Gelb — Highlight, Focus */
--color-success: #509C6E;     /* Grün — Done, Erfolg */
--color-bg: #ededed;          /* Background */
--color-panel: #ffffff;       /* Panel/Card */
--color-border: #abadb3;      /* Border */
--color-hover: #FFEFD9;       /* Pale yellow — Hover */
--color-today: #FFFCF1D9;     /* Today tile */
--font-family: Bahnschrift, 'Segoe UI', system-ui, sans-serif;
```

---

## Feature-Liste (Migration aus WPF-App)

### Phase 1 — Core (MVP)
- [ ] **Task CRUD**: Create, Read, Update, Delete Tasks
- [ ] **Task Model**: Description, Hours, Delivery Date, Importance (1-3), Weighting
- [ ] **Todo Tab**: Liste mit Filter (Description) + Sort (Due/Importance/Hours/Description)
- [ ] **Create Form**: Description, Hours, Delivery Date, Importance
- [ ] **Edit Task**: Modal/Dialog zum Bearbeiten (Double-Click)
- [ ] **Delete**: Mit Bestätigungsdialog
- [ ] **Persistence**: JSON-Dateien (todos.json, done.json, settings.json)

### Phase 2 — Planner
- [ ] **Task Distributor**: Gewichtungsbasierte Verteilung über Planungs-Horizont
- [ ] **Calendar Events**: Fixed Appointments, Work Hours, Free Time, Sleep
- [ ] **Plan Tab**: Wochenansicht mit 0-24h Stundenraster, Event-Tiles proportional zur Dauer
- [ ] **Week Tab**: 7-Spalten Wochenansicht mit distribuierten Tasks + Drag-and-Drop
- [ ] **Home Tab**: Today's Todos + Today's Appointments + Focus Timer

### Phase 3 — Focus Timer
- [ ] **Timer**: Start/Pause/Resume/Stop mit Countdown
- [ ] **Focus Blocks**: Konfigurierbare Focus-Dauer, Short/Long Break
- [ ] **Stats**: Focus Blocks Today, Total

### Phase 4 — Settings & Polish
- [ ] **Settings Tab**: Max Hours/Day, Plan Horizon, Work Hours, Timer-Config
- [ ] **Dark Mode**: Echter Dark/Light Toggle
- [ ] **Keyboard Shortcuts**: Ctrl+1..5 Tab-Switch, Ctrl+N New Task, Ctrl+Shift+N Quick-Capture
- [ ] **Status Bar**: Open Tasks, Today Hours, Focus Blocks
- [ ] **Empty States**: Für alle Listen
- [ ] **Responsive**: Mobile-Layout (Bottom-Nav statt Sidebar)

### Phase 5 — Cross-Platform
- [ ] **Android Build**: Capacitor Android Plugin
- [ ] **Desktop Build**: Tauri oder Electron Wrapper
- [ ] **App Icon**: Eigenes Icon
- [ ] **Offline Storage**: Capacitor Preferences API

---

## Domain-Logik (Port aus C#)

### Task Weighting Formel (aus Task.cs)
```
BaseWeight = 1000
Weight = BaseWeight / (1 + daysTillDelivery * 0.05)
        + 300 / Importance
        + 10 * Hours
        + DependencyFactor
        + RemainingSlotFactor
```

### Task Distributor (aus TaskSorter.cs)
1. Sortiere Tasks nach Weighting (absteigend)
2. Baue pro-Tag Verfügbarkeit: `min(CalendarEvent Hours, maxHoursPerDay)`
3. First-Fit-Decreasing: Erste Task in ersten freien Slot, bei Bedarf Split über mehrere Tage
4. Split-Label: `Task [part 1/3]`, `Task [part 2/3]`, etc.

### Calendar Event Types
- `FixedAppointment` (0) — harter Block, keine Tasks
- `WorkHours` (1) — Tasks können hier platziert werden
- `FreeTime` (2) — harter Block (Sport, Familie)
- `Sleep` (3) — harter Block, typischerweise ganzer Tag

---

## Projekt-Struktur

```
taskmanagement-app/
├── src/
│   ├── components/
│   │   ├── Sidebar.tsx          # Navigation (Desktop: Sidebar, Mobile: Bottom-Nav)
│   │   ├── StatusBar.tsx        # Untere Statusleiste
│   │   ├── TaskCard.tsx         # Einzelne Task-Karte
│   │   ├── EventTile.tsx        # Calendar Event Tile (proportional)
│   │   ├── FilterBar.tsx        # Todo Filter + Sort
│   │   └── Timer.tsx            # Focus Timer Komponente
│   ├── pages/
│   │   ├── Home.tsx             # Timer + Today Todos + Today Appointments
│   │   ├── Todo.tsx             # Task Liste mit Create/Edit/Filter
│   │   ├── Plan.tsx             # Wochen-Kalender mit Event-Tiles
│   │   ├── Week.tsx             # 7-Spalten Drag-and-Drop Wochenansicht
│   │   └── Settings.tsx         # Settings
│   ├── stores/
│   │   ├── taskStore.ts         # Task CRUD, done, weighting
│   │   ├── calendarStore.ts     # Calendar Events CRUD
│   │   ├── settingsStore.ts     # Settings + Persistence
│   │   └── timerStore.ts        # Focus Timer State
│   ├── lib/
│   │   ├── distributor.ts       # Task-Verteilungs-Algorithmus
│   │   ├── storage.ts           # JSON-Datei Persistence (localStorage/capacitor)
│   │   └── weighting.ts         # Task-Weighting-Formel
│   ├── types/
│   │   └── index.ts             # TypeScript Types (Task, CalendarEvent, etc.)
│   ├── App.tsx                  # Root mit Routing
│   ├── main.tsx                 # Vite Entry Point
│   └── index.css                # Tailwind + CSS Variables
├── public/
│   └── icons/                   # App Icons
├── capacitor.config.ts          # Capacitor Mobile Config
├── tailwind.config.js           # Tailwind Config (Farbpalette)
├── vite.config.ts               # Vite Config
├── tsconfig.json                # TypeScript Config
└── package.json
```

---

## UI-Design

### Desktop-Layout (>768px)
```
┌──────┬───────────────────────────┐
│      │                           │
│ S    │  Page Content             │
│ i    │                           │
│ d    │  (Home / Todo / Plan /     │
│ e    │   Week / Settings)        │
│ b    │                           │
│ a    │                           │
│ r    │                           │
│      ├───────────────────────────┤
│      │  Status Bar               │
└──────┴───────────────────────────┘
```

### Mobile-Layout (<768px)
```
┌───────────────────────┐
│  Page Content         │
│                       │
│  (scrollable)         │
│                       │
├───────────────────────┤
│ 🏠 ✅ 📅 🗓 ⚙️        │
│  Bottom Navigation    │
└───────────────────────┘
```

### Design-Prinzipien
- **Cards**: weiße Background, `border: 1px solid #ededed`, `border-radius: 10px`
- **Buttons**: Primary (Blau), Secondary (Weiß/Border), Danger (Rot), `border-radius: 8px`
- **Inputs**: `border-radius: 6px`, `border: 1px solid #ededed`, Focus: Blau
- **Sidebar**: 88px breit, Icons + Labels, Hover: `#FFEFD9`, Active: Blauer Left-Border
- **Timer**: Großes Display (36px), Blau, darunter State-Label
- **Task Cards**: Mini-Cards mit Description + Hours, `border-radius: 8px`

---

## Build-Targets

| Plattform | Tool | Output |
|-----------|------|--------|
| **Web** | Vite | `dist/` → direkt im Browser lauffähig |
| **Android** | Capacitor | `android/` → APK/AAB |
| **Linux** | Tauri 2 (optional) | `.deb`, `.AppImage` |
| **Windows** | Tauri 2 (optional) | `.exe`, `.msi` |
| **macOS** | Tauri 2 (optional) | `.dmg` |

> Phase 1-4 werden als **Web-App** entwickelt und getestet. Phase 5 fügt Mobile/Desktop-Wrapper hinzu.

---

## Testing-Strategie

1. **Sandbox**: `npm run dev` → App läuft im Browser, sofort testbar
2. **Unit Tests**: Vitest für `distributor.ts`, `weighting.ts`, Stores
3. **Component Tests**: Testing Library für TaskCard, FilterBar, Timer
4. **E2E**: Optional Playwright für kritische Flows
5. **User Testing**: Du kannst die App direkt im Browser testen (Portal-URL)

---

## Zeitplan (Schätzung)

| Phase | Inhalt | Schätzung |
|-------|--------|-----------|
| 1 | Setup + Task CRUD + Todo Tab | 1-2 Sessions |
| 2 | Planner + Distributor + Plan/Week Tab | 2-3 Sessions |
| 3 | Focus Timer + Home Tab | 1 Session |
| 4 | Settings + Dark Mode + Polish | 1-2 Sessions |
| 5 | Android + Desktop Wrapper | 1-2 Sessions |
| **Total** | | **6-10 Sessions** |

---

## Nächste Schritte

1. ✅ Plan erstellt (dieses Dokument)
2. ⬜ GitHub Branch `react-crossplatform` erstellen
3. ⬜ Vite + React + TypeScript Setup
4. ⬜ Tailwind + Farbpalette konfigurieren
5. ⬜ Types definieren (Task, CalendarEvent, Settings)
6. ⬜ Task Store + Storage (localStorage)
7. ⬜ Todo Tab (erste testbare Page)