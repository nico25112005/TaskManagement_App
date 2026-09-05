import { Play, Pause, Square } from 'lucide-react';
import { useTimerStore } from '../stores/timerStore';
import { useSettingsStore } from '../stores/settingsStore';

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
}

export function Timer() {
  const { status, remaining, total, mode, start, pause, resume, stop } = useTimerStore();
  const settings = useSettingsStore((s) => s.settings);

  const progress = total > 0 ? ((total - remaining) / total) * 100 : 0;
  const radius = 80;
  const circumference = 2 * Math.PI * radius;
  const dashOffset = circumference * (1 - progress / 100);

  const isBreak = mode !== 'focus';
  const ringColor = isBreak ? '#509C6E' : '#5E97D9';

  return (
    <div className="flex flex-col items-center gap-4">
      <div className="relative w-[200px] h-[200px]">
        <svg className="w-full h-full -rotate-90" viewBox="0 0 200 200">
          <circle
            cx="100"
            cy="100"
            r={radius}
            fill="none"
            stroke="currentColor"
            strokeWidth="8"
            className="text-gray-200 dark:text-gray-700"
          />
          <circle
            cx="100"
            cy="100"
            r={radius}
            fill="none"
            stroke={ringColor}
            strokeWidth="8"
            strokeLinecap="round"
            strokeDasharray={circumference}
            strokeDashoffset={dashOffset}
            style={{ transition: 'stroke-dashoffset 1s linear' }}
          />
        </svg>
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <span className="text-4xl font-bold text-gray-900 dark:text-dark-text">
            {formatTime(remaining)}
          </span>
          <span className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            {status === 'idle' && 'Bereit'}
            {status === 'running' && (isBreak ? 'Pause' : 'Focus')}
            {status === 'paused' && 'Pausiert'}
            {status === 'completed' && 'Fertig!'}
          </span>
        </div>
      </div>

      <div className="flex items-center gap-2">
        {status === 'idle' && (
          <button
            onClick={() => start(settings.focusBlockMinutes * 60)}
            className="btn-primary flex items-center gap-2"
          >
            <Play size={18} />
            Start
          </button>
        )}
        {status === 'running' && (
          <button
            onClick={pause}
            className="btn-secondary flex items-center gap-2"
          >
            <Pause size={18} />
            Pause
          </button>
        )}
        {status === 'paused' && (
          <button
            onClick={resume}
            className="btn-primary flex items-center gap-2"
          >
            <Play size={18} />
            Resume
          </button>
        )}
        {(status === 'running' || status === 'paused') && (
          <button
            onClick={stop}
            className="btn-danger flex items-center gap-2"
          >
            <Square size={18} />
            Stop
          </button>
        )}
        {status === 'completed' && (
          <button
            onClick={() => start(settings.focusBlockMinutes * 60)}
            className="btn-primary flex items-center gap-2"
          >
            <Play size={18} />
            Neuer Block
          </button>
        )}
      </div>
    </div>
  );
}