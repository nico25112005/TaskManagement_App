# UX Feature Pack

## Design-Prinzipien
- **Hell und clean** — viel Whitespace, weiße Cards, subtle Shadows
- **Keine visuelle Überladung** — maximal 2 Akzentfarben pro View
- **Schnelle Feedback-Schleifen** — Toasts, Inline-Validation, Loading-States
- **Mobil zuerst denkbar** — Touch-Targets min 44px, Bottom-Nav, Swipe-Gesten

## UX Features

### Navigation & Layout
- **Sidebar (Desktop)**: 72px schmal, Icon + Label, aktive Tab mit blauem Akzent
- **Bottom-Nav (Mobile)**: 5 Tabs, 56px hoch, Touch-optimiert
- **Responsive Breakpoint**: 768px — darunter Bottom-Nav, darüber Sidebar
- **Übergänge**: Sanfte Page-Transitions (fade/slide)
- **Breadcrumbs**: keiner — Sidebar ist selbsterklärend

### Task Management
- **Quick Capture** (Ctrl+Shift+N): Overlay mit Backdrop, sofortiges Erfassen
- **Inline Edit**: Double-Click auf Task → Edit-Modal mit Backdrop
- **Swipe to Delete** (Mobile): Nach links wischen → roter Delete-Hintergrund
- **Drag Handle** (Desktop): Hover zeigt Grip-Icon zum Sortieren
- **Bulk Actions**: Checkbox-Multi-Select → "Delete Selected", "Mark Done"
- **Task Priority Badge**: Farbiges Punkt-Indikator (Rot=High, Gelb=Medium, Blau=Low)
- **Due Date Warning**: Rot wenn overdue, Gelb wenn ≤2 Tage, Grau sonst
- **Task Estimate Validation**: Warnung wenn Hours > maxHoursPerDay

### Calendar / Plan
- **Event Tiles proportional**: 30min = halbe Row-Height, visuell klar
- **Click to Create**: Klick auf leeren Slot → Quick-Add Event
- **Drag to Resize**: Event-Größe ziehen um Dauer anzupassen
- **Conflict Warning**: Rot-Outline bei überlappenden Events
- **Day View / Week View Toggle**: Umsschalten zwischen 1-Tag und 7-Tage
- **Now-Indicator**: Rote Linie an aktueller Uhrzeit
- **Mini-Calendar**: Monats-Übersicht in Sidebar zum schnellen Datum-Sprung

### Week / Drag-and-Drop
- **30-min Raster**: Visuelle Slots, Tasks als Chips mit Höhe = Dauer
- **Drag between Days**: Task von einem Tag zum anderen ziehen
- **Drop Indicator**: Grüner Outline beim gültigen Drop-Target
- **Undo Toast**: "Task verschoben — Rückgängig" nach Drag-and-Drop
- **Hours Summary**: Pro Tag "3.5h / 4h" mit Farb-Indikator (grün/gelb/rot)

### Focus Timer
- **Circular Progress**: SVG-Ring Countdown statt nur Text
- **Pulse Animation**: Subtiles Pulsieren während Focus-Block
- **Break Mode**: Grüner Hintergrund bei Pause, anderer Text
- **Notification**: Browser-Notification bei Block-Ende (optional)
- **Session Counter**: "Block 2/3 today" Badge
- **Auto-Start Next**: Optional: nach Break automatisch nächsten Block starten

### Home Dashboard
- **Greeting**: "Guten Tag" mit Uhrzeit-basierter Begrüßung
- **Today Summary Card**: "3 Tasks, 4.5h geplant" — große Übersicht
- **Progress Ring**: Visualisierter Tages-Fortschritt
- **Upcoming Deadlines**: Top 3 Tasks nach Deadline
- **Quick Actions**: "+ New Task" Button prominent

### Settings
- **Live Preview**: Slider-Änderungen zeigen sofort Wirkung
- **Danger Zone**: Reset All Data Button mit Bestätigung
- **Export/Import**: JSON Download/Upload für Backup
- **Theme Toggle**: Light/Dark Toggle mit Smooth Transition

### Feedback & Polish
- **Toast Notifications**: Bottom-Right, auto-dismiss nach 3s
- **Empty States**: Illustration + Text + CTA für jede leere Liste
- **Loading States**: Skeleton-Spinner für Async-Operationen
- **Error States**: Rote Error-Banner mit Retry-Button
- **Animations**: Framer Motion für sanfte Ein-/Aus-Animationen
- **Hover States**: Subtle Scale/Shadow auf interaktiven Elementen
- **Focus States**: Blaue Outline für Keyboard-Navigation

### Dark Mode (echte Implementierung)
- **CSS Variables**: `--color-bg`, `--color-panel`, `--color-text` etc.
- **Light**: `#ededed` BG, `#ffffff` Panel, `#000` Text
- **Dark**: `#1a1a2e` BG, `#252540` Panel, `#e0e0e0` Text
- **Auto**: System-Preference erkennen (`prefers-color-scheme`)
- **Smooth**: `transition: background-color 0.3s` beim Umschalten

### Accessibility
- **Keyboard Navigation**: Tab durch alle interaktiven Elemente
- **ARIA Labels**: Screen-Reader-freundlich
- **Color Contrast**: WCAG AA (mindestens 4.5:1 für Text)
- **Focus Visible**: Blaue Outline bei Keyboard-Focus
- **Touch Targets**: Min 44px für alle Buttons/Links

### Responsive Verhalten
- **Desktop (>1024px)**: Full Sidebar + alle Features sichtbar
- **Tablet (768-1024px)**: Sidebar schmaler, Panels stapeln
- **Mobile (<768px)**: Bottom-Nav, Single-Column, Swipe-Gesten
- **Landscape/Portrait**: Auto-Adjust Layout