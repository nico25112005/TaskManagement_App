import { CheckCircle, Clock, Target } from 'lucide-react';
import { useTaskStore } from '../stores/taskStore';
import { useTimerStore } from '../stores/timerStore';

export function StatusBar() {
  const tasks = useTaskStore((s) => s.tasks);
  const blocksToday = useTimerStore((s) => s.totalBlocksToday);

  const openCount = Object.values(tasks).filter((t) => !t.done).length;
  const todayKey = new Date().toISOString().split('T')[0];
  const todayHours = Object.values(tasks)
    .filter((t) => t.delivery === todayKey)
    .reduce((sum, t) => sum + t.hours, 0);

  return (
    <footer className="h-8 bg-panel dark:bg-dark-panel border-t border-border dark:border-dark-border flex items-center justify-between px-4 text-xs text-gray-600 dark:text-gray-400">
      <div className="flex items-center gap-4">
        <span className="flex items-center gap-1">
          <CheckCircle size={14} className="text-primary" />
          {openCount} offen
        </span>
        <span className="flex items-center gap-1">
          <Clock size={14} className="text-highlight" />
          {todayHours.toFixed(1)}h heute
        </span>
        <span className="flex items-center gap-1">
          <Target size={14} className="text-success" />
          {blocksToday} Focus-Blöcke
        </span>
      </div>
      <span className="hidden sm:inline">TaskManager v1.0</span>
    </footer>
  );
}