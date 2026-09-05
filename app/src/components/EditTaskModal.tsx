import { useState, useEffect } from 'react';
import { X } from 'lucide-react';
import type { Task } from '../types';
import { useTaskStore } from '../stores/taskStore';

interface EditTaskModalProps {
  task: Task | null;
  onClose: () => void;
}

export function EditTaskModal({ task, onClose }: EditTaskModalProps) {
  const updateTask = useTaskStore((s) => s.updateTask);
  const [description, setDescription] = useState('');
  const [hours, setHours] = useState('1');
  const [delivery, setDelivery] = useState('');
  const [importance, setImportance] = useState<1 | 2 | 3>(2);

  useEffect(() => {
    if (task) {
      setDescription(task.description);
      setHours(String(task.hours));
      setDelivery(task.delivery);
      setImportance(task.importance);
    }
  }, [task]);

  if (!task) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!description.trim() || !delivery || parseFloat(hours) <= 0) return;
    updateTask(task.id, {
      description: description.trim(),
      hours: parseFloat(hours),
      delivery,
      importance,
    });
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40" onClick={onClose}>
      <div
        className="card p-6 w-full max-w-md mx-4 animate-slide-up"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-bold text-gray-900 dark:text-dark-text">Task bearbeiten</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600" aria-label="Schließen">
            <X size={20} />
          </button>
        </div>
        <form onSubmit={handleSubmit} className="flex flex-col gap-3">
          <input
            type="text"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Task Beschreibung..."
            className="input"
            autoFocus
          />
          <div className="flex gap-2">
            <input
              type="number"
              value={hours}
              onChange={(e) => setHours(e.target.value)}
              step="0.5"
              min="0.5"
              className="input w-24"
              placeholder="Stunden"
            />
            <input
              type="date"
              value={delivery}
              onChange={(e) => setDelivery(e.target.value)}
              className="input flex-1"
            />
            <select
              value={importance}
              onChange={(e) => setImportance(Number(e.target.value) as 1 | 2 | 3)}
              className="input"
            >
              <option value={1}>Hoch</option>
              <option value={2}>Mittel</option>
              <option value={3}>Niedrig</option>
            </select>
          </div>
          <div className="flex gap-2 justify-end">
            <button type="button" onClick={onClose} className="btn-secondary">
              Abbrechen
            </button>
            <button type="submit" className="btn-primary">
              Speichern
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}