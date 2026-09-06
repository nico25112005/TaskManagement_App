import { useState, useMemo, useRef, useCallback } from 'react';
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react';
import { useCalendarStore } from '../stores/calendarStore';
import { EventTile } from '../components/EventTile';
import type { CalendarEvent, CalendarEventType } from '../types';

function getWeekStart(date: Date): Date {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  const day = d.getDay();
  const diff = d.getDate() - day + (day === 0 ? -6 : 1);
  return new Date(d.setDate(diff));
}

const dayNames = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'];

/** Group overlapping events and assign column indices for side-by-side layout. */
function layoutEvents(events: CalendarEvent[]): Map<string, { col: number; cols: number }> {
  const sorted = [...events].sort((a, b) => {
    const aStart = new Date(a.start).getTime();
    const bStart = new Date(b.start).getTime();
    if (aStart !== bStart) return aStart - bStart;
    return new Date(a.end).getTime() - new Date(b.end).getTime();
  });

  const result = new Map<string, { col: number; cols: number }>();
  let activeColumns: number[] = [];
  let group: CalendarEvent[] = [];

  for (const evt of sorted) {
    const evtStart = new Date(evt.start).getTime();
    const evtEnd = new Date(evt.end).getTime();

    // Remove columns and events that have ended before this one starts
    activeColumns = activeColumns.filter((end) => end > evtStart);
    group = group.filter((e) => new Date(e.end).getTime() > evtStart);

    // Find first free column
    let col = 0;
    while (col < activeColumns.length && activeColumns[col] > evtStart) col++;

    if (col === activeColumns.length) {
      activeColumns.push(evtEnd);
    } else {
      activeColumns[col] = evtEnd;
    }

    result.set(evt.id, { col, cols: 1 });
    group.push(evt);

    // Update cols for all in current group
    const currentCols = activeColumns.length;
    for (const e of group) {
      const existing = result.get(e.id)!;
      result.set(e.id, { col: existing.col, cols: currentCols });
    }
  }

  return result;
}

export function Plan() {
  const { events, addEvent, deleteEvent } = useCalendarStore();
  const [weekStart, setWeekStart] = useState(getWeekStart(new Date()));
  const [showAdd, setShowAdd] = useState(false);

  // Add event form
  const [evtType, setEvtType] = useState<CalendarEventType>('FixedAppointment');
  const [evtTitle, setEvtTitle] = useState('');
  const [evtDay, setEvtDay] = useState(0);
  const [evtStart, setEvtStart] = useState('09:00');
  const [evtEnd, setEvtEnd] = useState('10:00');

  const weekDays = useMemo(() => {
    const days: Date[] = [];
    for (let i = 0; i < 7; i++) {
      const d = new Date(weekStart);
      d.setDate(d.getDate() + i);
      days.push(d);
    }
    return days;
  }, [weekStart]);

  const weekEnd = weekDays[6];
  const todayKey = new Date().toISOString().split('T')[0];

  // 0-24h grid — full day, 25 labels (0 through 24)
  const hours = useMemo(() => {
    const result: number[] = [];
    for (let h = 0; h <= 24; h++) result.push(h);
    return result;
  }, []);

  const rowHeight = 48; // px per hour
  const firstHour = 0;
  const totalGridHeight = 24 * rowHeight;

  const handleAddEvent = (e: React.FormEvent) => {
    e.preventDefault();
    if (!evtTitle.trim()) return;

    // Validate times
    const [sh, sm] = evtStart.split(':').map(Number);
    const [eh, em] = evtEnd.split(':').map(Number);
    if (isNaN(sh) || isNaN(eh) || (eh * 60 + em) <= (sh * 60 + sm)) return;

    const day = new Date(weekStart);
    day.setDate(day.getDate() + evtDay);
    const startDate = new Date(day);
    startDate.setHours(sh, sm, 0, 0);
    const endDate = new Date(day);
    endDate.setHours(eh, em, 0, 0);

    addEvent({
      title: evtTitle.trim(),
      start: startDate.toISOString(),
      end: endDate.toISOString(),
      type: evtType,
    });
    setEvtTitle('');
    setShowAdd(false);
  };

  const handleRemoveEvent = (eventId: string) => {
    deleteEvent(eventId);
  };

  const nowIndicator = useMemo(() => {
    const now = new Date();
    const nowDay = now.toISOString().split('T')[0];
    const isThisWeek = weekDays.some((d) => d.toISOString().split('T')[0] === nowDay);
    if (!isThisWeek) return null;
    const hour = now.getHours() + now.getMinutes() / 60;
    return { dayKey: nowDay, top: hour * rowHeight };
  }, [weekDays]);

  const goToPrevWeek = () => {
    const d = new Date(weekStart);
    d.setDate(d.getDate() - 7);
    setWeekStart(d);
  };

  const goToNextWeek = () => {
    const d = new Date(weekStart);
    d.setDate(d.getDate() + 7);
    setWeekStart(d);
  };

  const goToToday = () => setWeekStart(getWeekStart(new Date()));

  // --- Drag-to-Create ---
  const [drag, setDrag] = useState<{ dayIdx: number; startHour: number; currentHour: number } | null>(null);
  const gridRefs = useRef<HTMLDivElement[]>([]);

  const pixelToHour = useCallback((clientY: number, dayIdx: number) => {
    const gridEl = gridRefs.current[dayIdx];
    if (!gridEl) return firstHour;
    const rect = gridEl.getBoundingClientRect();
    const y = clientY - rect.top;
    const rawHour = firstHour + y / rowHeight;
    // Snap to 30-minute increments
    return Math.round(rawHour * 2) / 2;
  }, []);

  const handleGridMouseDown = (e: React.MouseEvent, dayIdx: number) => {
    // Only start drag on left button
    if (e.button !== 0) return;
    const hour = pixelToHour(e.clientY, dayIdx);
    if (hour < firstHour || hour > 24) return;
    setDrag({ dayIdx, startHour: hour, currentHour: hour });
    e.preventDefault();
  };

  const handleGridMouseMove = (e: React.MouseEvent, dayIdx: number) => {
    if (!drag || drag.dayIdx !== dayIdx) return;
    const hour = pixelToHour(e.clientY, dayIdx);
    const clamped = Math.max(firstHour, Math.min(24, hour));
    setDrag({ ...drag, currentHour: clamped });
  };

  const handleGridMouseUp = () => {
    if (!drag) return;
    const start = Math.min(drag.startHour, drag.currentHour);
    const end = Math.max(drag.startHour, drag.currentHour);
    if (end - start >= 0.5) {
      // Open add-event form with pre-filled times
      const startH = Math.floor(start);
      const startM = start % 1 === 0.5 ? 30 : 0;
      const endH = Math.floor(end);
      const endM = end % 1 === 0.5 ? 30 : 0;
      setEvtDay(drag.dayIdx);
      setEvtType('FixedAppointment');
      setEvtTitle('');
      setEvtStart(`${startH.toString().padStart(2, '0')}:${startM.toString().padStart(2, '0')}`);
      setEvtEnd(`${endH.toString().padStart(2, '0')}:${endM.toString().padStart(2, '0')}`);
      setShowAdd(true);
    }
    setDrag(null);
  };

  const formatDragTime = (h: number) => {
    const hh = Math.floor(h);
    const mm = h % 1 === 0.5 ? '30' : '00';
    return `${hh.toString().padStart(2, '0')}:${mm}`;
  };

  // Helper to convert an event's local hour into grid coordinates (1..24 => 0..23 rows)
  const eventTop = (date: Date) => (date.getHours() + date.getMinutes() / 60 - firstHour) * rowHeight;

  return (
    <div className="p-4 md:p-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Plan</h1>
        <button onClick={() => setShowAdd(!showAdd)} className="btn-primary flex items-center gap-2">
          <Plus size={18} />
          <span className="hidden sm:inline">Event</span>
        </button>
      </div>

      {/* Date Navigation */}
      <div className="flex items-center justify-center gap-3 mb-4">
        <button onClick={goToPrevWeek} className="btn-secondary p-2" aria-label="Vorherige Woche">
          <ChevronLeft size={18} />
        </button>
        <span className="text-sm font-medium text-gray-700 dark:text-gray-300 min-w-[180px] text-center">
          Woche vom {weekStart.toLocaleDateString('de-DE')} – {weekEnd.toLocaleDateString('de-DE')}
        </span>
        <button onClick={goToNextWeek} className="btn-secondary p-2" aria-label="Nächste Woche">
          <ChevronRight size={18} />
        </button>
        <button onClick={goToToday} className="btn-secondary text-xs">Heute</button>
      </div>

      {/* Add Event Form */}
      {showAdd && (
        <form onSubmit={handleAddEvent} className="card p-4 mb-4 flex flex-wrap gap-2 items-end">
          <div className="flex flex-col gap-1">
            <label className="text-xs text-gray-500">Typ</label>
            <select value={evtType} onChange={(e) => setEvtType(e.target.value as CalendarEventType)} className="input">
              <option value="FixedAppointment">Termin</option>
              <option value="WorkHours">Arbeit</option>
              <option value="FreeTime">Freizeit</option>
              <option value="Sleep">Schlaf</option>
            </select>
          </div>
          <div className="flex flex-col gap-1 flex-1 min-w-[150px]">
            <label className="text-xs text-gray-500">Titel</label>
            <input type="text" value={evtTitle} onChange={(e) => setEvtTitle(e.target.value)} placeholder="Event-Titel" className="input" autoFocus />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-xs text-gray-500">Tag</label>
            <select value={evtDay} onChange={(e) => setEvtDay(Number(e.target.value))} className="input">
              {dayNames.map((d, i) => (
                <option key={i} value={i}>{d}</option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-xs text-gray-500">Von</label>
            <input type="time" value={evtStart} onChange={(e) => setEvtStart(e.target.value)} className="input" required />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-xs text-gray-500">Bis</label>
            <input type="time" value={evtEnd} onChange={(e) => setEvtEnd(e.target.value)} className="input" required />
          </div>
          <button type="submit" className="btn-primary">Hinzufügen</button>
        </form>
      )}

      {/* Week Grid — 0 to 24 hours */}
      <div className="card overflow-x-auto">
        <div className="flex min-w-[800px]">
          {/* Hour labels column (0-24) */}
          <div className="w-12 shrink-0 border-r border-gray-200 dark:border-gray-700">
            <div className="h-10 border-b border-gray-200 dark:border-gray-700 flex items-center justify-center text-[10px] text-gray-400">
              Uhr
            </div>
            {/* 24 hour rows (0-23), each rowHeight tall */}
            {hours.slice(0, 24).map((h) => (
              <div key={h} className="text-xs text-gray-400 text-right pr-1.5 leading-none flex items-end justify-end" style={{ height: rowHeight }}>
                {h.toString().padStart(2, '0')}:00
              </div>
            ))}
            {/* 24:00 label at bottom */}
            <div className="text-xs text-gray-400 text-right pr-1.5 leading-none flex items-end justify-end" style={{ height: 0 }}>
            </div>
          </div>

          {/* Day columns */}
          {weekDays.map((day, dayIdx) => {
            const dayKey = day.toISOString().split('T')[0];
            const isToday = dayKey === todayKey;
            const dayEvents = events.filter((e) => {
              const eventDate = new Date(e.start).toISOString().split('T')[0];
              return eventDate === dayKey;
            });

            // Compute overlap layout for this day's events
            const eventLayout = layoutEvents(dayEvents);

            // Drag overlay for this day
            const dayDrag = drag && drag.dayIdx === dayIdx ? {
              start: Math.min(drag.startHour, drag.currentHour),
              end: Math.max(drag.startHour, drag.currentHour),
            } : null;

            return (
              <div key={dayIdx} className="flex-1 border-r border-gray-200 dark:border-gray-700 last:border-r-0 relative">
                {/* Header */}
                <div className={`h-10 border-b border-gray-200 dark:border-gray-700 flex items-center justify-center text-xs font-medium ${
                  isToday ? 'bg-primary text-white' : 'text-gray-600 dark:text-gray-400'
                }`}>
                  {dayNames[dayIdx]} {day.getDate().toString().padStart(2, '0')}.{(day.getMonth() + 1).toString().padStart(2, '0')}
                </div>
                {/* Hour grid 1-24 */}
                <div
                  className="relative overflow-hidden select-none"
                  style={{ height: totalGridHeight }}
                  ref={(el) => { if (el) gridRefs.current[dayIdx] = el; }}
                  onMouseDown={(e) => handleGridMouseDown(e, dayIdx)}
                  onMouseMove={(e) => handleGridMouseMove(e, dayIdx)}
                  onMouseUp={handleGridMouseUp}
                  onMouseLeave={() => { if (drag) setDrag(null); }}
                >
                  {/* Hour slot lines */}
                  {hours.map((h) => (
                    <div
                      key={h}
                      className="border-b border-gray-100 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition-colors"
                      style={{ height: rowHeight }}
                    />
                  ))}
                  {/* Drag selection overlay */}
                  {dayDrag && (
                    <div
                      className="absolute left-0 right-0 bg-primary/20 border-2 border-primary rounded-md z-30 pointer-events-none flex items-start justify-center pt-1"
                      style={{
                        top: `${(dayDrag.start - firstHour) * rowHeight}px`,
                        height: `${(dayDrag.end - dayDrag.start) * rowHeight}px`,
                      }}
                    >
                      <span className="text-xs font-semibold text-primary bg-white/80 dark:bg-dark-panel/80 px-2 py-0.5 rounded">
                        {formatDragTime(dayDrag.start)} – {formatDragTime(dayDrag.end)}
                      </span>
                    </div>
                  )}
                  {/* Now indicator */}
                  {nowIndicator && nowIndicator.dayKey === dayKey && (
                    <div
                      className="absolute left-0 right-0 h-0.5 bg-danger z-20 pointer-events-none"
                      style={{ top: `${nowIndicator.top}px` }}
                    >
                      <div className="w-2 h-2 bg-danger rounded-full -ml-1 -mt-[3px]" />
                    </div>
                  )}
                  {/* Events positioned by time */}
                  {dayEvents.map((event) => {
                    const eventStart = new Date(event.start);
                    const eventEnd = new Date(event.end);
                    const top = eventTop(eventStart);
                    const durationHours = (eventEnd.getTime() - eventStart.getTime()) / (1000 * 60 * 60);
                    const heightPx = Math.max(24, durationHours * rowHeight);
                    const layout = eventLayout.get(event.id) ?? { col: 0, cols: 1 };
                    const widthPct = 100 / layout.cols;
                    const leftPct = layout.col * widthPct;

                    return (
                      <div
                        key={event.id}
                        className="absolute group z-10"
                        style={{
                          top: `${top}px`,
                          height: `${heightPx}px`,
                          left: `${leftPct}%`,
                          width: `${widthPct}%`,
                          paddingLeft: '2px',
                          paddingRight: '2px',
                        }}
                      >
                        <EventTile event={event} />
                        <button
                          onClick={(e) => { e.stopPropagation(); handleRemoveEvent(event.id); }}
                          className="absolute -top-1 -right-1 w-5 h-5 bg-danger text-white rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity text-[10px] z-30"
                        >
                          ✕
                        </button>
                      </div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}