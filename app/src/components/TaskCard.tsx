import { Trash2, AlertCircle, CheckCircle } from 'lucide-react';
import type { Task } from '../types';

interface TaskCardProps {
  task: Task;
  onDelete?: (id: string) => void;
  onDoubleClick?: (task: Task) => void;
  onToggleDone?: (id: string) => void;
}

const importanceColors: Record<number, string> = {
  1: 'bg-danger text-white',
  2: 'bg-highlight text-gray-900',
  3: 'bg-primary text-white',
};

const importanceLabels: Record<number, string> = {
  1: 'Hoch',
  2: 'Mittel',
  3: 'Niedrig',
};

function getDueDateColor(delivery: string): string {
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  const due = new Date(delivery);
  due.setHours(0, 0, 0, 0);
  const diffDays = Math.floor((due.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));

  if (diffDays < 0) return 'text-danger';
  if (diffDays <= 2) return 'text-highlight';
  return 'text-gray-500 dark:text-gray-400';
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr);
  return date.toLocaleDateString('de-DE', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

export function TaskCard({ task, onDelete, onDoubleClick, onToggleDone }: TaskCardProps) {
  return (
    <div
      className="card p-3 flex items-start gap-3 cursor-pointer hover:shadow-md transition-all"
      onDoubleClick={() => onDoubleClick?.(task)}
    >
      <button
        onClick={(e) => {
          e.stopPropagation();
          onToggleDone?.(task.id);
        }}
        className={`mt-1 w-5 h-5 rounded-full border-2 flex items-center justify-center shrink-0 transition-all ${
          task.done
            ? 'bg-success border-success text-white'
            : 'border-gray-300 dark:border-dark-border hover:border-primary'
        }`}
        aria-label={task.done ? 'Als erledigt markieren' : 'Als offen markieren'}
      >
        {task.done && <CheckCircle size={12} />}
      </button>

      <div className="flex-1 min-w-0">
        <p className={`text-sm font-medium ${task.done ? 'line-through text-gray-400' : 'text-gray-900 dark:text-dark-text'}`}>
          {task.description}
        </p>
        <div className="flex items-center gap-3 mt-1 flex-wrap">
          <span className={`text-xs ${getDueDateColor(task.delivery)} flex items-center gap-1`}>
            <AlertCircle size={12} />
            {formatDate(task.delivery)}
          </span>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            {task.hours}h
          </span>
          <span className={`badge ${importanceColors[task.importance]}`}>
            {importanceLabels[task.importance]}
          </span>
        </div>
      </div>

      {onDelete && (
        <button
          onClick={(e) => {
            e.stopPropagation();
            onDelete(task.id);
          }}
          className="text-gray-400 hover:text-danger p-1 rounded transition-colors"
          aria-label="Task löschen"
        >
          <Trash2 size={16} />
        </button>
      )}
    </div>
  );
}

