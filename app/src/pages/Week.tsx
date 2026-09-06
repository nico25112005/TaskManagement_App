import { useState, useMemo } from 'react';
import {
  DndContext,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  useDraggable,
  useDroppable,
} from '@dnd-kit/core';
import { Undo2, Clock, Calendar } from 'lucide-react';
import { useTaskStore } from '../stores/taskStore';
import { useCalendarStore } from '../stores/calendarStore';
import { useSettingsStore } from '../stores/settingsStore';
import { distributeTasks } from '../lib/distributor';
import type { Task, WeekDay as WeekDayType } from '../types';

interface WeekProps {
  onToast: (message: string, type?: 'success' | 'error' | 'info') => void;
}

function getWeekStart(date: Date): Date {
  const d = new Date(date);
  d.setHours(0, 0, 0, 0);
  const day = d.getDay();
  const diff = d.getDate() - day + (day === 0 ? -6 : 1);
  return new Date(d.setDate(diff));
}

const dayNames = ['Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa', 'So'];

function DraggableTask({ task, dayIndex }: { task: Task; dayIndex: number }) {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: `${task.id}|${dayIndex}`,
    data: { taskId: task.id, fromDay: dayIndex },
  });

  const style = transform
    ? { transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`, zIndex: 50 }
    : undefined;

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`bg-primary text-white rounded-lg px-3 py-2 text-sm cursor-grab hover:bg-primary/90 transition-colors shadow-sm ${
        isDragging ? 'opacity-50' : ''
      }`}
      {...listeners}
      {...attributes}
    >
      <div className="font-medium truncate">{task.description}</div>
      <div className="text-xs opacity-80 flex items-center gap-1">
        <Clock size={11} />
        {task.hours.toFixed(1)}h
      </div>
    </div>
  );
}

function DroppableDay({
  day,
  dayIndex,
  maxHours,
  dayEvents,
  children,
}: {
  day: WeekDayType;
  dayIndex: number;
  maxHours: number;
  dayEvents: { id: string; title: string; start: string; end: string; type: string }[];
  children: React.ReactNode;
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: `day-${dayIndex}`,
    data: { dayIndex, date: day.date },
  });

  const isOverloaded = day.plannedHours > maxHours;
  const hoursColor = isOverloaded ? 'text-danger' : day.plannedHours > 0 ? 'text-success' : 'text-gray-400';
  const hoursBg = isOverloaded ? 'bg-danger/10' : isOver ? 'bg-success/10' : '';

  const typeDotColors: Record<string, string> = {
    FixedAppointment: 'bg-danger',
    WorkHours: 'bg-primary',
    FreeTime: 'bg-success',
    Sleep: 'bg-gray-500',
  };

  return (
    <div
      ref={setNodeRef}
      className={`flex-1 min-w-[130px] border-r border-gray-200 dark:border-gray-700 last:border-r-0 transition-colors ${hoursBg}`}
    >
      {/* Day header */}
      <div className="h-16 border-b border-gray-200 dark:border-gray-700 flex flex-col items-center justify-center gap-0.5">
        <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
          {dayNames[dayIndex]}
        </span>
        <span className="text-xs text-gray-400">
          {new Date(day.date).toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit' })}
        </span>
        <span className={`text-xs font-bold ${hoursColor}`}>
          {day.plannedHours.toFixed(1)}h / {maxHours.toFixed(1)}h
        </span>
      </div>
      {/* Events section */}
      {dayEvents.length > 0 && (
        <div className="px-2 py-1.5 border-b border-gray-100 dark:border-gray-800 space-y-1">
          {dayEvents.map((evt) => (
            <div key={evt.id} className="flex items-center gap-1.5 text-xs">
              <span className={`w-2 h-2 rounded-full shrink-0 ${typeDotColors[evt.type] ?? 'bg-gray-400'}`} />
              <span className="text-gray-600 dark:text-gray-400 font-mono">
                {new Date(evt.start).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' })}
              </span>
              <span className="truncate text-gray-700 dark:text-gray-300">{evt.title}</span>
            </div>
          ))}
        </div>
      )}
      {/* Task area */}
      <div className="p-2 space-y-2" style={{ minHeight: '300px' }}>
        {children}
      </div>
    </div>
  );
}

export function Week({ onToast }: WeekProps) {
  const tasks = useTaskStore((s) => s.tasks);
  const events = useCalendarStore((s) => s.events);
  const settings = useSettingsStore((s) => s.settings);
  const [weekStart, setWeekStart] = useState(getWeekStart(new Date()));
  const [localDistribution, setLocalDistribution] = useState<WeekDayType[] | null>(null);
  const [undoData, setUndoData] = useState<WeekDayType[] | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } })
  );

  const computedDays = useMemo(() => {
    const days = distributeTasks(Object.values(tasks), events, settings);
    const weekDays: WeekDayType[] = [];
    for (let i = 0; i < 7; i++) {
      const date = new Date(weekStart);
      date.setDate(date.getDate() + i);
      const isoDate = date.toISOString().split('T')[0];
      const day = days.find((d) => d.date === isoDate) ?? { date: isoDate, tasks: [], plannedHours: 0 };
      weekDays.push(day);
    }
    return weekDays;
  }, [tasks, events, settings, weekStart]);

  const displayDays = localDistribution ?? computedDays;

  // Get events for a specific day
  const getEventsForDay = (dayDate: string) => {
    return events
      .filter((e) => new Date(e.start).toISOString().split('T')[0] === dayDate)
      .sort((a, b) => new Date(a.start).getTime() - new Date(b.start).getTime())
      .map((e) => ({ id: e.id, title: e.title, start: e.start, end: e.end, type: e.type }));
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over) return;

    const activeData = active.data.current;
    const overData = over.data.current;
    if (!activeData || !overData) return;

    const taskId = activeData.taskId as string;
    const fromDay = activeData.fromDay as number;
    const toDay = overData.dayIndex as number;

    if (fromDay === toDay) return;

    setUndoData(displayDays.map((d) => ({ ...d, tasks: [...d.tasks] })));

    setLocalDistribution((prev) => {
      const days = (prev ?? computedDays).map((d) => ({ ...d, tasks: [...d.tasks] }));
      const task = days[fromDay].tasks.find((t) => t.id === taskId);
      if (!task) return prev;

      days[fromDay].tasks = days[fromDay].tasks.filter((t) => t.id !== taskId);
      days[fromDay].plannedHours -= task.hours;
      days[toDay].tasks.push(task);
      days[toDay].plannedHours += task.hours;

      return days;
    });

    onToast('Task verschoben — Rückgängig', 'info');
  };

  const handleUndo = () => {
    if (undoData) {
      setLocalDistribution(undoData);
      setUndoData(null);
      onToast('Rückgängig gemacht', 'success');
    }
  };

  const weekEnd = new Date(weekStart);
  weekEnd.setDate(weekEnd.getDate() + 6);

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

  // Calculate total week hours
  const totalWeekHours = displayDays.reduce((sum, d) => sum + d.plannedHours, 0);
  const maxWeekHours = settings.maxHoursPerDay * 7;

  return (
    <div className="p-4 md:p-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Woche</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
            Gesamt: {totalWeekHours.toFixed(1)}h / {maxWeekHours.toFixed(1)}h
          </p>
        </div>
        {undoData && (
          <button onClick={handleUndo} className="btn-secondary flex items-center gap-2 text-sm">
            <Undo2 size={16} />
            Rückgängig
          </button>
        )}
      </div>

      {/* Date Navigation */}
      <div className="flex items-center justify-center gap-3 mb-4">
        <button onClick={goToPrevWeek} className="btn-secondary p-2">◀</button>
        <span className="text-sm font-medium text-gray-700 dark:text-gray-300 min-w-[180px] text-center">
          {weekStart.toLocaleDateString('de-DE')} – {weekEnd.toLocaleDateString('de-DE')}
        </span>
        <button onClick={goToNextWeek} className="btn-secondary p-2">▶</button>
        <button onClick={goToToday} className="btn-secondary text-xs">Heute</button>
      </div>

      {/* Week Grid with DnD */}
      <div className="card overflow-x-auto">
        <DndContext sensors={sensors} onDragEnd={handleDragEnd}>
          <div className="flex min-w-[700px]">
            {displayDays.map((day, dayIdx) => (
              <DroppableDay
                key={dayIdx}
                day={day}
                dayIndex={dayIdx}
                maxHours={settings.maxHoursPerDay}
                dayEvents={getEventsForDay(day.date)}
              >
                {day.tasks.length === 0 ? (
                  <div className="text-center text-xs text-gray-300 dark:text-gray-600 py-8">
                    Keine Tasks
                  </div>
                ) : (
                  day.tasks.map((task) => (
                    <DraggableTask key={task.id + '-' + dayIdx} task={task} dayIndex={dayIdx} />
                  ))
                )}
              </DroppableDay>
            ))}
          </div>
        </DndContext>
      </div>

      <p className="text-xs text-gray-400 mt-3 text-center flex items-center justify-center gap-1">
        <Calendar size={12} />
        Tasks per Drag & Drop zwischen Tagen verschieben · Termine aus dem Plan werden hier angezeigt
      </p>
    </div>
  );
}