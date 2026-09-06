import { useState, useEffect } from 'react';
import { X, Check, Trash2, Circle } from 'lucide-react';
import type { Task } from '../types';

interface TaskDetailProps {
  task: Task;
  onClose: () => void;
  onSave: (id: string, updates: Partial<Task>) => void;
  onDelete: (id: string) => void;
  onMarkDone: (id: string) => void;
}

export function TaskDetail({ task, onClose, onSave, onDelete, onMarkDone }: TaskDetailProps) {
  const [description, setDescription] = useState(task.description);
  const [hours, setHours] = useState(String(task.hours));
  const [delivery, setDelivery] = useState(task.delivery);
  const [importance, setImportance] = useState<1 | 2 | 3>(task.importance);

  useEffect(() => {
    setDescription(task.description);
    setHours(String(task.hours));
    setDelivery(task.delivery);
    setImportance(task.importance);
  }, [task.id, task.description, task.hours, task.delivery, task.importance]);

  const handleSave = () => {
    if (!description.trim() || !delivery || parseFloat(hours) <= 0) return;
    onSave(task.id, {
      description: description.trim(),
      hours: parseFloat(hours),
      delivery,
      importance,
    });
    onClose();
  };

  const handleDelete = () => {
    onDelete(task.id);
    onClose();
  };

  const handleMarkDone = () => {
    onMarkDone(task.id);
    onClose();
  };

  const createdDate = task.id.includes('_') ? null : task.id;

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-40 bg-black/30 transition-opacity duration-200"
        onClick={onClose}
      />

      {/* Slide-in Panel */}
      <div className="fixed top-0 right-0 bottom-0 z-50 w-full max-w-md bg-white dark:bg-dark-panel shadow-xl overflow-y-auto transition-transform duration-300 animate-slide-in-right md:rounded-l-2xl md:max-w-md max-w-full">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-dark-border sticky top-0 bg-white dark:bg-dark-panel z-10">
          <h2 className="text-lg font-bold text-gray-900 dark:text-dark-text">Task Details</h2>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 p-1 rounded-lg transition-colors"
            aria-label="Schließen"
          >
            <X size={22} />
          </button>
        </div>

        {/* Body */}
        <div className="p-4 space-y-4">
          {/* Description */}
          <div>
            <label className="text-xs font-medium text-gray-500 dark:text-gray-400 block mb-1">
              Beschreibung
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="input w-full min-h-[80px] resize-y"
              rows={3}
            />
          </div>

          {/* Hours */}
          <div>
            <label className="text-xs font-medium text-gray-500 dark:text-gray-400 block mb-1">
              Stunden
            </label>
            <input
              type="number"
              value={hours}
              onChange={(e) => setHours(e.target.value)}
              step="0.5"
              min="0.5"
              className="input w-full"
            />
          </div>

          {/* Delivery Date */}
          <div>
            <label className="text-xs font-medium text-gray-500 dark:text-gray-400 block mb-1">
              Lieferdatum
            </label>
            <input
              type="date"
              value={delivery}
              onChange={(e) => setDelivery(e.target.value)}
              className="input w-full"
            />
          </div>

          {/* Importance */}
          <div>
            <label className="text-xs font-medium text-gray-500 dark:text-gray-400 block mb-1">
              Wichtigkeit
            </label>
            <select
              value={importance}
              onChange={(e) => setImportance(Number(e.target.value) as 1 | 2 | 3)}
              className="input w-full"
            >
              <option value={1}>Hoch</option>
              <option value={2}>Mittel</option>
              <option value={3}>Niedrig</option>
            </select>
          </div>

          {/* Created date (read-only) */}
          {createdDate && (
            <div>
              <label className="text-xs font-medium text-gray-500 dark:text-gray-400 block mb-1">
                Erstellt
              </label>
              <p className="text-sm text-gray-400 dark:text-gray-500 italic">
                {new Date(parseInt(task.id.split('_')[1] || '0')).toLocaleDateString('de-DE') || 'Unbekannt'}
              </p>
            </div>
          )}

          {/* Dependent tasks info */}
          {task.dependentTasks.length > 0 && (
            <div>
              <label className="text-xs font-medium text-gray-500 dark:text-gray-400 block mb-1">
                Abhängige Tasks
              </label>
              <p className="text-sm text-gray-400 dark:text-gray-500">
                {task.dependentTasks.length} Abhängigkeit(en)
              </p>
            </div>
          )}
        </div>

        {/* Actions */}
        <div className="p-4 border-t border-gray-200 dark:border-dark-border space-y-2 sticky bottom-0 bg-white dark:bg-dark-panel">
          <div className="flex gap-2">
            <button
              onClick={handleSave}
              className="btn-primary flex-1 flex items-center justify-center gap-2"
            >
              <Check size={18} />
              Speichern
            </button>
            <button
              onClick={onClose}
              className="btn-secondary flex-1"
            >
              Abbrechen
            </button>
          </div>
          <div className="flex gap-2">
            <button
              onClick={handleMarkDone}
              className="btn-secondary flex-1 flex items-center justify-center gap-2 text-success"
            >
              <Circle size={16} className="fill-success" />
              Als erledigt markieren
            </button>
            <button
              onClick={handleDelete}
              className="btn-danger flex-1 flex items-center justify-center gap-2"
            >
              <Trash2 size={16} />
              Löschen
            </button>
          </div>
        </div>
      </div>
    </>
  );
}