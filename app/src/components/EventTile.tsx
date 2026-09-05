import type { CalendarEvent, CalendarEventType } from '../types';

interface EventTileProps {
  event: CalendarEvent;
}

const typeColors: Record<CalendarEventType, { bg: string; text: string; border: string }> = {
  FixedAppointment: { bg: 'bg-danger/20', text: 'text-danger', border: 'border-danger/40' },
  WorkHours: { bg: 'bg-primary/15', text: 'text-primary', border: 'border-primary/30' },
  FreeTime: { bg: 'bg-success/15', text: 'text-success', border: 'border-success/30' },
  Sleep: { bg: 'bg-gray-700/40', text: 'text-gray-300', border: 'border-gray-600/40' },
};

const typeLabels: Record<CalendarEventType, string> = {
  FixedAppointment: 'Termin',
  WorkHours: 'Arbeit',
  FreeTime: 'Freizeit',
  Sleep: 'Schlaf',
};

function formatTime(dateStr: string): string {
  return new Date(dateStr).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' });
}

export function EventTile({ event }: EventTileProps) {
  const start = new Date(event.start);
  const end = new Date(event.end);
  const durationMin = (end.getTime() - start.getTime()) / (1000 * 60);
  const heightPx = Math.max(20, (durationMin / 30) * 28);

  const colors = typeColors[event.type];

  return (
    <div
      className={`absolute left-1 right-1 ${colors.bg} ${colors.text} ${colors.border} border rounded-md px-2 py-1 overflow-hidden cursor-pointer hover:shadow-md transition-all`}
      style={{ height: `${heightPx}px` }}
    >
      <div className="text-xs font-medium truncate">{event.title}</div>
      <div className="text-[10px] opacity-75">
        {formatTime(event.start)} – {formatTime(event.end)}
      </div>
      <div className="text-[9px] opacity-50 mt-0.5">{typeLabels[event.type]}</div>
    </div>
  );
}