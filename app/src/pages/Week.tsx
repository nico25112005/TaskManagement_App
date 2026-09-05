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
import { Undo2 } from 'lucide-react';
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

  const heightPx = Math.max(28, (task.hours * 60) / 30 * 28);

  const style = transform
    ? { transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`, zIndex: 50 }
    : undefined;

  return (
    <div
      ref={setNodeRef}
      style={{ ...style, height: `${heightPx}px` }}
      className={`bg-primary/20 border border-primary/40 rounded-md px-2 py-1 text-xs text-primary cursor-grab hover:bg-primary/30 transition-colors ${
        isDragging ? 'opacity-50' : ''
      }`}
      {...listeners}
      {...attributes}
    >
      <div className="font-medium truncate">{task.description}</div>
      <div className="text-[10px] opacity-70">{task.hours}h</div>
    </div>
  );
}

function DroppableDay({
  day,
  dayIndex,
  children,
}: {
  day: WeekDayType;
  dayIndex: number;
  children: React.ReactNode;
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: `day-${dayIndex}`,
    data: { dayIndex, date: day.date },
  });

  const hoursColor = day.plannedHours > 5 ? 'text-danger' : day.plannedHours > 3 ? 'text-highlight' : 'text-success';

  return (
    <div
      ref={setNodeRef}
      className={`flex-1 min-w-[120px] border-r border-gray-200 dark:border-dark-border last:border-r-0 ${
        isOver ? 'bg-success/10 border-2 border-success/40 rounded' : ''
      }`}
    >
      {/* Day header */}
      <div className="h-14 border-b border-gray-200 dark:border-dark-border flex flex-col items-center justify-center">
        <span className="text-xs font-medium text-gray-600 dark:text-gray-400">
          {dayNames[dayIndex]}
        </span>
        <span className="text-[10px] text-gray-400">
          {new Date(day.date).toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit' })}
        </span>
        <span className={`text-[10px] font-medium ${hoursColor}`}>
          {day.plannedHours.toFixed(1)}h
        </span>
      </div>
      {/* Task area */}
      <div className="p-1 space-y-1" style={{ minHeight: '300px' }}>
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
    // Filter to current week
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

    // Save undo state
    setUndoData(displayDays.map((d) => ({ ...d, tasks: [...d.tasks] })));

    // Move task
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

  // Hour labels
  const hourLabels = useMemo(() => {
    const labels: string[] = [];
    for (let h = settings.workStartHour; h <= settings.workEndHour; h++) {
      labels.push(`${h.toString().padStart(2, '0')}:00`);
    }
    return labels;
  }, [settings.workStartHour, settings.workEndHour]);

  return (
    <div className="p-4 md:p-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-dark-text">Woche</h1>
        {undoData && (
          <button onClick={handleUndo} className="btn-secondary flex items-center gap-2 text-sm">
            <Undo2 size={16} />
            Rückgängig
          </button>
        )}
      </div>

      {/* Date Navigation */}
      <div className="flex items-center justify-center gap-4 mb-4">
        <button
          onClick={() => setWeekStart(new Date(new Date(weekStart).setDate(weekStart.getDate() - 7)))}
          className="btn-secondary p-2"
        >
          ◀
        </button>
        <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
          {weekStart.toLocaleDateString('de-DE')} – {weekEnd.toLocaleDateString('de-DE')}
        </span>
        <button
          onClick={() => setWeekStart(new Date(new Date(weekStart).setDate(weekStart.getDate() + 7)))}
          className="btn-secondary p-2"
        >
          ▶
        </button>
        <button onClick={() => setWeekStart(getWeekStart(new Date()))} className="btn-secondary text-xs">
          Heute
        </button>
      </div>

      {/* Week Grid with DnD */}
      <div className="card overflow-x-auto">
        <DndContext sensors={sensors} onDragEnd={handleDragEnd}>
          <div className="flex min-w-[700px]">
            {/* Hour labels */}
            <div className="w-12 shrink-0 border-r border-gray-200 dark:border-dark-border">
              <div className="h-14 border-b border-gray-200 dark:border-dark-border" />
              {hourLabels.map((label) => (
                <div key={label} className="text-[10px] text-gray-400 text-right pr-1" style={{ height: '56px' }}>
                  {label}
                </div>
              ))}
            </div>

            {/* Day columns */}
            {displayDays.map((day, dayIdx) => (
              <DroppableDay key={dayIdx} day={day} dayIndex={dayIdx}>
                {day.tasks.length === 0 ? (
                  <div className="text-center text-[10px] text-gray-300 dark:text-gray-600 py-4">
                    Frei
                  </div>
                ) : (
                  day.tasks.map((task) => (
                    <DraggableTask key={task.id + dayIdx} task={task} dayIndex={dayIdx} />
                  ))
                )}
              </DroppableDay>
            ))}
          </div>
        </DndContext>
      </div>
    </div>
  );
}