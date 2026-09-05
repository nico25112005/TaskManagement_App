import { useState, useMemo } from 'react';
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react';
import { useCalendarStore } from '../stores/calendarStore';
import { useSettingsStore } from '../stores/settingsStore';
import { EventTile } from '../components/EventTile';
import type { CalendarEventType } from '../types';

function getWeekStart(date: Date): Date {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  const day = d.getDay();
  const diff = d.getDate() - day + (day === 0 ? -6 : 1); // Monday as start
  return new Date(d.setDate(diff));
}

function formatDateHeader(date: Date): string {
  const days = ['So', 'Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa'];
  const months = ['01', '02', '03', '04', '05', '06', '07', '08', '09', '10', '11', '12'];
  return `${days[date.getDay()]} ${date.getDate().toString().padStart(2, '0')}.${months[date.getMonth() - 1] ?? '01'}`;
}

export function Plan() {
  const { events, addEvent } = useCalendarStore();
  const settings = useSettingsStore((s) => s.settings);
  const [weekStart, setWeekStart] = useState(getWeekStart(new Date()));
  const [showAdd, setShowAdd] = useState(false);

  // Add event form
  const [evtType, setEvtType] = useState<CalendarEventType>('WorkHours');
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

  const hours = useMemo(() => {
    const start = settings.workStartHour;
    const end = settings.workEndHour;
    const result: number[] = [];
    for (let h = start; h <= end; h++) result.push(h);
    return result;
  }, [settings.workStartHour, settings.workEndHour]);

  const rowHeight = 56; // px per hour

  const handleAddEvent = (e: React.FormEvent) => {
    e.preventDefault();
    if (!evtTitle.trim()) return;
    const day = new Date(weekStart);
    day.setDate(day.getDate() + evtDay);
    const startDate = new Date(day);
    const [sh, sm] = evtStart.split(':').map(Number);
    startDate.setHours(sh, sm, 0, 0);
    const endDate = new Date(day);
    const [eh, em] = evtEnd.split(':').map(Number);
    endDate.setHours(eh, em, 0, 0);
    addEvent({ title: evtTitle.trim(), start: startDate.toISOString(), end: endDate.toISOString(), type: evtType });
    setEvtTitle('');
    setShowAdd(false);
  };

  const nowIndicator = useMemo(() => {
    const now = new Date();
    const nowDay = now.toISOString().split('T')[0];
    const isThisWeek = weekDays.some((d) => d.toISOString().split('T')[0] === nowDay);
    if (!isThisWeek) return null;
    const hour = now.getHours() + now.getMinutes() / 60;
    if (hour < settings.workStartHour || hour > settings.workEndHour) return null;
    return { dayKey: nowDay, top: (hour - settings.workStartHour) * rowHeight };
  }, [weekDays, settings.workStartHour, settings.workEndHour]);

  return (
    <div className="p-4 md:p-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-dark-text">Plan</h1>
        <button onClick={() => setShowAdd(!showAdd)} className="btn-primary flex items-center gap-2">
          <Plus size={18} />
          <span className="hidden sm:inline">Event</span>
        </button>
      </div>

      {/* Date Navigation */}
      <div className="flex items-center justify-center gap-4 mb-4">
        <button
          onClick={() => setWeekStart(new Date(weekStart.setDate(weekStart.getDate() - 7)))}
          className="btn-secondary p-2"
          aria-label="Vorherige Woche"
        >
          <ChevronLeft size={18} />
        </button>
        <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Woche vom {weekStart.toLocaleDateString('de-DE')} – {weekEnd.toLocaleDateString('de-DE')}
        </span>
        <button
          onClick={() => setWeekStart(new Date(weekStart.setDate(weekStart.getDate() + 7)))}
          className="btn-secondary p-2"
          aria-label="Nächste Woche"
        >
          <ChevronRight size={18} />
        </button>
        <button onClick={() => setWeekStart(getWeekStart(new Date()))} className="btn-secondary text-xs">
          Heute
        </button>
      </div>

      {/* Add Event Form */}
      {showAdd && (
        <form onSubmit={handleAddEvent} className="card p-4 mb-4 animate-slide-up flex flex-wrap gap-2 items-end">
          <select value={evtType} onChange={(e) => setEvtType(e.target.value as CalendarEventType)} className="input">
            <option value="FixedAppointment">Termin</option>
            <option value="WorkHours">Arbeit</option>
            <option value="FreeTime">Freizeit</option>
            <option value="Sleep">Schlaf</option>
          </select>
          <input type="text" value={evtTitle} onChange={(e) => setEvtTitle(e.target.value)} placeholder="Titel" className="input flex-1 min-w-[150px]" />
          <select value={evtDay} onChange={(e) => setEvtDay(Number(e.target.value))} className="input">
            {['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'].map((d, i) => (
              <option key={i} value={i}>{d}</option>
            ))}
          </select>
          <input type="time" value={evtStart} onChange={(e) => setEvtStart(e.target.value)} className="input" />
          <input type="time" value={evtEnd} onChange={(e) => setEvtEnd(e.target.value)} className="input" />
          <button type="submit" className="btn-primary">Hinzufügen</button>
        </form>
      )}

      {/* Week Grid */}
      <div className="card overflow-x-auto">
        <div className="flex min-w-[700px]">
          {/* Hour labels column */}
          <div className="w-12 shrink-0 border-r border-gray-200 dark:border-dark-border">
            <div className="h-10 border-b border-gray-200 dark:border-dark-border" />
            {hours.map((h) => (
              <div key={h} className="text-xs text-gray-400 text-right pr-1" style={{ height: rowHeight }}>
                {h.toString().padStart(2, '0')}:00
              </div>
            ))}
          </div>

          {/* Day columns */}
          {weekDays.map((day, dayIdx) => {
            const dayKey = day.toISOString().split('T')[0];
            const isToday = dayKey === todayKey;
            const dayEvents = events.filter((e) => {
              const eventDate = new Date(e.start).toISOString().split('T')[0];
              return eventDate === dayKey;
            });

            return (
              <div key={dayIdx} className="flex-1 border-r border-gray-200 dark:border-dark-border last:border-r-0 relative">
                {/* Header */}
                <div className={`h-10 border-b border-gray-200 dark:border-dark-border flex items-center justify-center text-xs font-medium ${
                  isToday ? 'bg-primary text-white' : 'text-gray-600 dark:text-gray-400'
                }`}>
                  {formatDateHeader(day)}
                </div>
                {/* Hour grid */}
                <div className="relative" style={{ height: hours.length * rowHeight }}>
                  {hours.map((h) => (
                    <div key={h} className="border-b border-gray-100 dark:border-gray-700/50" style={{ height: rowHeight }} />
                  ))}
                  {/* Now indicator */}
                  {nowIndicator && nowIndicator.dayKey === dayKey && (
                    <div
                      className="absolute left-0 right-0 h-0.5 bg-danger z-10"
                      style={{ top: nowIndicator.top }}
                    >
                      <div className="w-2 h-2 bg-danger rounded-full -ml-1 -mt-[3px]" />
                    </div>
                  )}
                  {/* Events */}
                  {dayEvents.map((event) => {
                    const eventStart = new Date(event.start);
                    const startHour = eventStart.getHours() + eventStart.getMinutes() / 60;
                    const top = (startHour - settings.workStartHour) * rowHeight;
                    return (
                      <div key={event.id} className="absolute left-1 right-1" style={{ top: `${top}px` }}>
                        <EventTile event={event} />
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