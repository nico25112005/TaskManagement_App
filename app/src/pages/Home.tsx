import { useMemo } from 'react';
import { Calendar, AlertCircle, Plus } from 'lucide-react';
import { useTaskStore } from '../stores/taskStore';
import { useCalendarStore } from '../stores/calendarStore';
import { useSettingsStore } from '../stores/settingsStore';
import { distributeTasks } from '../lib/distributor';
import { Timer } from '../components/Timer';
import { ProgressRing } from '../components/ProgressRing';
import { TaskCard } from '../components/TaskCard';

interface HomeProps {
  onNavigate: (page: 'home' | 'todo' | 'plan' | 'week' | 'settings') => void;
}

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 11) return 'Guten Morgen';
  if (hour < 17) return 'Guten Tag';
  return 'Guten Abend';
}

export function Home({ onNavigate }: HomeProps) {
  const tasks = useTaskStore((s) => s.tasks);
  const events = useCalendarStore((s) => s.events);
  const settings = useSettingsStore((s) => s.settings);
  const markDone = useTaskStore((s) => s.markDone);

  const todayKey = new Date().toISOString().split('T')[0];

  const weekDays = useMemo(
    () => distributeTasks(Object.values(tasks), events, settings),
    [tasks, events, settings]
  );

  const todayPlan = weekDays.find((d) => d.date === todayKey);
  const todayTasks = todayPlan?.tasks ?? [];
  const todayHours = todayPlan?.plannedHours ?? 0;
  const taskCount = todayTasks.length;

  const todayEvents = events.filter((e) => {
    const eventDate = new Date(e.start).toISOString().split('T')[0];
    return eventDate === todayKey;
  });

  const upcomingDeadlines = useMemo(() => {
    return Object.values(tasks)
      .filter((t) => !t.done)
      .sort((a, b) => new Date(a.delivery).getTime() - new Date(b.delivery).getTime())
      .slice(0, 3);
  }, [tasks]);

  const progressPct = settings.maxHoursPerDay > 0 ? (todayHours / settings.maxHoursPerDay) * 100 : 0;

  return (
    <div className="p-4 md:p-6 max-w-4xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold text-gray-900 dark:text-dark-text">
        {getGreeting()}! 👋
      </h1>

      {/* Today Summary + Timer */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Summary Card */}
        <div className="card p-5 flex items-center gap-4">
          <ProgressRing progress={progressPct} size={100} strokeWidth={8}>
            <div className="text-center">
              <div className="text-xl font-bold text-gray-900 dark:text-dark-text">{taskCount}</div>
              <div className="text-[10px] text-gray-500">Tasks</div>
            </div>
          </ProgressRing>
          <div>
            <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300">Heute</h2>
            <p className="text-2xl font-bold text-primary">{todayHours.toFixed(1)}h</p>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              von {settings.maxHoursPerDay}h geplant
            </p>
          </div>
        </div>

        {/* Timer Card */}
        <div className="card p-5 flex flex-col items-center justify-center">
          <Timer />
        </div>
      </div>

      {/* Today's Todos */}
      <div className="card p-4">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
          Heutige Tasks
        </h2>
        {todayTasks.length === 0 ? (
          <p className="text-sm text-gray-400 py-4 text-center">Keine Tasks für heute geplant 🎉</p>
        ) : (
          <div className="space-y-2">
            {todayTasks.map((task) => (
              <TaskCard
                key={task.id}
                task={task}
                onToggleDone={markDone}
                onDoubleClick={() => onNavigate('todo')}
              />
            ))}
          </div>
        )}
      </div>

      {/* Today's Appointments */}
      <div className="card p-4">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-2">
          <Calendar size={16} />
          Heutige Termine
        </h2>
        {todayEvents.length === 0 ? (
          <p className="text-sm text-gray-400 py-4 text-center">Keine Termine heute</p>
        ) : (
          <div className="space-y-2">
            {todayEvents.map((event) => {
              const start = new Date(event.start).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' });
              const end = new Date(event.end).toLocaleTimeString('de-DE', { hour: '2-digit', minute: '2-digit' });
              return (
                <div key={event.id} className="flex items-center gap-3 py-2 border-b border-gray-100 dark:border-dark-border last:border-0">
                  <span className="text-xs font-mono text-gray-500 dark:text-gray-400">{start}–{end}</span>
                  <span className="text-sm text-gray-900 dark:text-dark-text">{event.title}</span>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Upcoming Deadlines */}
      <div className="card p-4">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-2">
          <AlertCircle size={16} />
          Anstehende Deadlines
        </h2>
        {upcomingDeadlines.length === 0 ? (
          <p className="text-sm text-gray-400 py-4 text-center">Keine anstehenden Deadlines</p>
        ) : (
          <div className="space-y-2">
            {upcomingDeadlines.map((task) => (
              <div key={task.id} className="flex items-center justify-between py-2 border-b border-gray-100 dark:border-dark-border last:border-0">
                <span className="text-sm text-gray-900 dark:text-dark-text">{task.description}</span>
                <span className="text-xs text-gray-500 dark:text-gray-400">
                  {new Date(task.delivery).toLocaleDateString('de-DE')}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>

      <button
        onClick={() => onNavigate('todo')}
        className="fixed bottom-20 md:bottom-8 right-4 btn-primary rounded-full w-14 h-14 flex items-center justify-center shadow-lg z-30"
        aria-label="Neuer Task"
      >
        <Plus size={24} />
      </button>
    </div>
  );
}