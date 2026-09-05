import { useState, useMemo } from 'react';
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react';
import { useCalendarStore } from '../stores/calendarStore';
import { EventTile } from '../components/EventTile';
import type { CalendarEventType } from '../types';

function getWeekStart(date: Date): Date {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  const day = d.getDay();
  const diff = d.getDate() - day + (day === 0 ? -6 : 1);
  return new Date(d.setDate(diff));
}

const dayNames = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'];

export function Plan() {
  const { events, addEvent, deleteEvent } = useCalendarStore();
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

  // 0-24h grid — full day
  const hours = useMemo(() => {
    const result: number[] = [];
    for (let h = 0; h < 24; h++) result.push(h);
    return result;
  }, []);

  const rowHeight = 48; // px per hour
  const totalGridHeight = 24 * rowHeight;

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
            <input type="text" value={evtTitle} onChange={(e) => setEvtTitle(e.target.value)} placeholder="Event-Titel" className="input" />
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
            <input type="time" value={evtStart} onChange={(e) => setEvtStart(e.target.value)} className="input" />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-xs text-gray-500">Bis</label>
            <input type="time" value={evtEnd} onChange={(e) => setEvtEnd(e.target.value)} className="input" />
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
            {hours.map((h) => (
              <div key={h} className="text-xs text-gray-400 text-right pr-1.5 leading-none flex items-end justify-end" style={{ height: rowHeight }}>
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
              <div key={dayIdx} className="flex-1 border-r border-gray-200 dark:border-gray-700 last:border-r-0 relative">
                {/* Header */}
                <div className={`h-10 border-b border-gray-200 dark:border-gray-700 flex items-center justify-center text-xs font-medium ${
                  isToday ? 'bg-primary text-white' : 'text-gray-600 dark:text-gray-400'
                }`}>
                  {dayNames[dayIdx]} {day.getDate().toString().padStart(2, '0')}.{(day.getMonth() + 1).toString().padStart(2, '0')}
                </div>
                {/* Hour grid 0-24 */}
                <div className="relative" style={{ height: totalGridHeight }}>
                  {hours.map((h) => (
                    <div
                      key={h}
                      className="border-b border-gray-100 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/30 transition-colors cursor-pointer"
                      style={{ height: rowHeight }}
                      onClick={() => {
                        // Click on hour slot → pre-fill add form
                        setEvtDay(dayIdx);
                        setEvtStart(`${h.toString().padStart(2, '0')}:00`);
                        setEvtEnd(`${(h + 1).toString().padStart(2, '0')}:00`);
                        setShowAdd(true);
                      }}
                    />
                  ))}
                  {/* Now indicator */}
                  {nowIndicator && nowIndicator.dayKey === dayKey && (
                    <div
                      className="absolute left-0 right-0 h-0.5 bg-danger z-10"
                      style={{ top: `${nowIndicator.top}px` }}
                    >
                      <div className="w-2 h-2 bg-danger rounded-full -ml-1 -mt-[3px]" />
                    </div>
                  )}
                  {/* Events */}
                  {dayEvents.map((event) => {
                    const eventStart = new Date(event.start);
                    const startHour = eventStart.getHours() + eventStart.getMinutes() / 60;
                    const top = startHour * rowHeight;
                    return (
                      <div key={event.id} className="absolute left-0.5 right-0.5 group" style={{ top: `${top}px` }}>
                        <EventTile event={event} />
                        <button
                          onClick={() => handleRemoveEvent(event.id)}
                          className="absolute -top-1 -right-1 w-5 h-5 bg-danger text-white rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity text-[10px]"
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