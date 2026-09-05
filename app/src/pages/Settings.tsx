import { useRef } from 'react';
import { Moon, Sun, Download, Upload, Trash2, Info } from 'lucide-react';
import { useSettingsStore } from '../stores/settingsStore';
import { useTaskStore } from '../stores/taskStore';
import { useCalendarStore } from '../stores/calendarStore';
import { exportAllData, importAllData, clearAllData } from '../lib/storage';

interface SettingsProps {
  onToast: (message: string, type?: 'success' | 'error' | 'info') => void;
}

export function Settings({ onToast }: SettingsProps) {
  const { settings, updateSettings, toggleDarkMode, resetSettings } = useSettingsStore();
  const { tasks, done, loadTasks } = useTaskStore();
  const { loadEvents } = useCalendarStore();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleExport = () => {
    const data = exportAllData();
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `taskmanager-backup-${new Date().toISOString().split('T')[0]}.json`;
    a.click();
    URL.revokeObjectURL(url);
    onToast('Daten exportiert', 'success');
  };

  const handleImport = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (ev) => {
      try {
        const data = JSON.parse(ev.target?.result as string);
        importAllData(data);
        loadTasks();
        loadEvents();
        onToast('Daten importiert', 'success');
      } catch {
        onToast('Import fehlgeschlagen', 'error');
      }
    };
    reader.readAsText(file);
  };

  const handleReset = () => {
    if (confirm('Wirklich alle Daten löschen? Dies kann nicht rückgängig gemacht werden.')) {
      clearAllData();
      loadTasks();
      loadEvents();
      resetSettings();
      onToast('Alle Daten gelöscht', 'info');
    }
  };

  const openCount = Object.values(tasks).filter((t) => !t.done).length;
  const doneCount = Object.keys(done).length;

  return (
    <div className="p-4 md:p-6 max-w-2xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold text-gray-900 dark:text-dark-text">Settings</h1>

      {/* Planner Section */}
      <section className="card p-4">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Planer</h2>
        <div className="space-y-4">
          <div>
            <label className="text-xs text-gray-500 dark:text-gray-400 flex justify-between">
              <span>Max. Stunden pro Tag</span>
              <span className="font-bold">{settings.maxHoursPerDay}h</span>
            </label>
            <input
              type="range"
              min="1"
              max="12"
              step="0.5"
              value={settings.maxHoursPerDay}
              onChange={(e) => updateSettings({ maxHoursPerDay: parseFloat(e.target.value) })}
              className="w-full mt-1 accent-primary"
            />
          </div>
          <div>
            <label className="text-xs text-gray-500 dark:text-gray-400 flex justify-between">
              <span>Planungs-Horizont (Tage)</span>
              <span className="font-bold">{settings.maxPlanableDays}</span>
            </label>
            <input
              type="range"
              min="7"
              max="30"
              step="1"
              value={settings.maxPlanableDays}
              onChange={(e) => updateSettings({ maxPlanableDays: parseInt(e.target.value) })}
              className="w-full mt-1 accent-primary"
            />
          </div>
          <div className="flex gap-2 items-end">
            <div className="flex-1">
              <label className="text-xs text-gray-500 dark:text-gray-400">Arbeitsbeginn</label>
              <select
                value={settings.workStartHour}
                onChange={(e) => updateSettings({ workStartHour: parseInt(e.target.value) })}
                className="input w-full mt-1"
              >
                {Array.from({ length: 24 }, (_, i) => (
                  <option key={i} value={i}>{i.toString().padStart(2, '0')}:00</option>
                ))}
              </select>
            </div>
            <div className="flex-1">
              <label className="text-xs text-gray-500 dark:text-gray-400">Arbeitsende</label>
              <select
                value={settings.workEndHour}
                onChange={(e) => updateSettings({ workEndHour: parseInt(e.target.value) })}
                className="input w-full mt-1"
              >
                {Array.from({ length: 24 }, (_, i) => (
                  <option key={i} value={i}>{i.toString().padStart(2, '0')}:00</option>
                ))}
              </select>
            </div>
          </div>
        </div>
      </section>

      {/* Focus Timer Section */}
      <section className="card p-4">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Focus Timer</h2>
        <div className="space-y-4">
          <div>
            <label className="text-xs text-gray-500 dark:text-gray-400 flex justify-between">
              <span>Focus Block (Minuten)</span>
              <span className="font-bold">{settings.focusBlockMinutes}min</span>
            </label>
            <input
              type="range"
              min="10"
              max="60"
              step="5"
              value={settings.focusBlockMinutes}
              onChange={(e) => updateSettings({ focusBlockMinutes: parseInt(e.target.value) })}
              className="w-full mt-1 accent-primary"
            />
          </div>
          <div>
            <label className="text-xs text-gray-500 dark:text-gray-400 flex justify-between">
              <span>Kurze Pause (Minuten)</span>
              <span className="font-bold">{settings.shortBreakMinutes}min</span>
            </label>
            <input
              type="range"
              min="1"
              max="15"
              step="1"
              value={settings.shortBreakMinutes}
              onChange={(e) => updateSettings({ shortBreakMinutes: parseInt(e.target.value) })}
              className="w-full mt-1 accent-primary"
            />
          </div>
          <div>
            <label className="text-xs text-gray-500 dark:text-gray-400 flex justify-between">
              <span>Lange Pause (Minuten)</span>
              <span className="font-bold">{settings.longBreakMinutes}min</span>
            </label>
            <input
              type="range"
              min="10"
              max="30"
              step="5"
              value={settings.longBreakMinutes}
              onChange={(e) => updateSettings({ longBreakMinutes: parseInt(e.target.value) })}
              className="w-full mt-1 accent-primary"
            />
          </div>
          <div>
            <label className="text-xs text-gray-500 dark:text-gray-400 flex justify-between">
              <span>Blöcke vor langer Pause</span>
              <span className="font-bold">{settings.blocksBeforeLongBreak}</span>
            </label>
            <input
              type="range"
              min="2"
              max="6"
              step="1"
              value={settings.blocksBeforeLongBreak}
              onChange={(e) => updateSettings({ blocksBeforeLongBreak: parseInt(e.target.value) })}
              className="w-full mt-1 accent-primary"
            />
          </div>
        </div>
      </section>

      {/* Appearance Section */}
      <section className="card p-4">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Erscheinungsbild</h2>
        <button
          onClick={toggleDarkMode}
          className="btn-secondary flex items-center gap-3 w-full justify-start"
        >
          {settings.darkMode ? <Moon size={20} /> : <Sun size={20} />}
          <span>{settings.darkMode ? 'Dark Mode aktiv' : 'Light Mode aktiv'}</span>
          <span className={`ml-auto w-12 h-6 rounded-full relative transition-colors ${settings.darkMode ? 'bg-primary' : 'bg-gray-300'}`}>
            <span className={`absolute top-0.5 w-5 h-5 bg-white rounded-full transition-transform ${settings.darkMode ? 'translate-x-6' : 'translate-x-0.5'}`} />
          </span>
        </button>
      </section>

      {/* Data Section */}
      <section className="card p-4">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Daten</h2>
        <div className="flex flex-col gap-2">
          <span className="text-xs text-gray-500 dark:text-gray-400">
            {openCount} offene Tasks, {doneCount} erledigte Tasks
          </span>
          <button onClick={handleExport} className="btn-secondary flex items-center gap-2">
            <Download size={16} />
            Export (JSON)
          </button>
          <button onClick={() => fileInputRef.current?.click()} className="btn-secondary flex items-center gap-2">
            <Upload size={16} />
            Import (JSON)
          </button>
          <input ref={fileInputRef} type="file" accept=".json" onChange={handleImport} className="hidden" />
          <button onClick={handleReset} className="btn-danger flex items-center gap-2 mt-2">
            <Trash2 size={16} />
            Alle Daten löschen
          </button>
        </div>
      </section>

      {/* About Section */}
      <section className="card p-4">
        <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-2">
          <Info size={16} />
          Über
        </h2>
        <div className="text-xs text-gray-500 dark:text-gray-400 space-y-1">
          <p>TaskManager v1.0.0</p>
          <p>React + Vite + TypeScript + Tailwind CSS</p>
          <a
            href="https://github.com/openclaw/openclaw"
            target="_blank"
            rel="noopener noreferrer"
            className="text-primary hover:underline"
          >
            GitHub Repository
          </a>
        </div>
      </section>
    </div>
  );
}