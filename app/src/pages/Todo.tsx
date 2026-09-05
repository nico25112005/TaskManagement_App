import { useState, useMemo, useEffect } from 'react';
import { Plus, Edit } from 'lucide-react';
import { useTaskStore } from '../stores/taskStore';
import type { Task, SortOption } from '../types';
import { FilterBar } from '../components/FilterBar';
import { TaskCard } from '../components/TaskCard';
import { EmptyState } from '../components/EmptyState';
import { EditTaskModal } from '../components/EditTaskModal';

interface TodoProps {
  onToast: (message: string, type?: 'success' | 'error' | 'info') => void;
}

export function Todo({ onToast }: TodoProps) {
  const { tasks, addTask, deleteTask, markDone } = useTaskStore();
  const [filter, setFilter] = useState('');
  const [sort, setSort] = useState<SortOption>('due');
  const [showCreate, setShowCreate] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | null>(null);

  // Create form state
  const [description, setDescription] = useState('');
  const [hours, setHours] = useState('1');
  const [delivery, setDelivery] = useState('');
  const [importance, setImportance] = useState<1 | 2 | 3>(2);

  useEffect(() => {
    setDelivery(new Date().toISOString().split('T')[0]);
  }, []);

  const taskList = useMemo(() => {
    let filtered = Object.values(tasks).filter((t) =>
      t.description.toLowerCase().includes(filter.toLowerCase())
    );

    filtered.sort((a, b) => {
      switch (sort) {
        case 'due':
          return new Date(a.delivery).getTime() - new Date(b.delivery).getTime();
        case 'importance':
          return a.importance - b.importance;
        case 'hours':
          return b.hours - a.hours;
        case 'description':
          return a.description.localeCompare(b.description);
      }
    });

    return filtered;
  }, [tasks, filter, sort]);

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!description.trim() || !delivery || parseFloat(hours) <= 0) return;
    addTask({
      description: description.trim(),
      hours: parseFloat(hours),
      delivery,
      importance,
    });
    setDescription('');
    setHours('1');
    setImportance(2);
    setShowCreate(false);
    onToast('Task hinzugefügt', 'success');
  };

  const handleDelete = (id: string) => {
    deleteTask(id);
    onToast('Task gelöscht', 'info');
  };

  const handleToggleDone = (id: string) => {
    const task = tasks[id];
    if (task?.done) {
      useTaskStore.getState().markUndone(id);
    } else {
      markDone(id);
      onToast('Task erledigt! 🎉', 'success');
    }
  };

  return (
    <div className="p-4 md:p-6 max-w-4xl mx-auto">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-dark-text">Todo</h1>
        <button
          onClick={() => setShowCreate(!showCreate)}
          className="btn-primary flex items-center gap-2"
        >
          <Plus size={18} />
          <span className="hidden sm:inline">Neuer Task</span>
        </button>
      </div>

      {/* Create Form */}
      {showCreate && (
        <form onSubmit={handleCreate} className="card p-4 mb-4 animate-slide-up space-y-3">
          <input
            type="text"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Task Beschreibung..."
            className="input w-full"
            autoFocus
          />
          <div className="flex gap-2 flex-wrap">
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
            <button type="button" onClick={() => setShowCreate(false)} className="btn-secondary">
              Abbrechen
            </button>
            <button type="submit" className="btn-primary">
              Hinzufügen
            </button>
          </div>
        </form>
      )}

      {/* Filter Bar */}
      <div className="mb-4">
        <FilterBar filter={filter} setFilter={setFilter} sort={sort} setSort={setSort} />
      </div>

      {/* Task List */}
      {taskList.length === 0 ? (
        <EmptyState
          icon="📝"
          title="Keine Tasks"
          description="Erstelle deinen ersten Task, um loszulegen!"
          actionLabel="Neuer Task"
          onAction={() => setShowCreate(true)}
        />
      ) : (
        <div className="space-y-2">
          {taskList.map((task) => (
            <TaskCard
              key={task.id}
              task={task}
              onDelete={handleDelete}
              onToggleDone={handleToggleDone}
              onDoubleClick={(t) => setEditingTask(t)}
            />
          ))}
        </div>
      )}

      <EditTaskModal task={editingTask} onClose={() => setEditingTask(null)} />
    </div>
  );
}