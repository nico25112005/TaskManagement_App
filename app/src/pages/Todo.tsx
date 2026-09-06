import { useState, useMemo, useEffect } from 'react';
import { Plus, ChevronDown, ChevronRight, Undo, Trash2 } from 'lucide-react';
import { useTaskStore } from '../stores/taskStore';
import type { Task, SortOption } from '../types';
import { FilterBar } from '../components/FilterBar';
import { TaskCard } from '../components/TaskCard';
import { EmptyState } from '../components/EmptyState';
import { EditTaskModal } from '../components/EditTaskModal';
import { TaskDetail } from '../components/TaskDetail';

interface TodoProps {
  onToast: (message: string, type?: 'success' | 'error' | 'info') => void;
}

export function Todo({ onToast }: TodoProps) {
  const { tasks, done, addTask, deleteTask, updateTask, markDone, deleteDoneTask } = useTaskStore();
  const [filter, setFilter] = useState('');
  const [sort, setSort] = useState<SortOption>('due');
  const [showCreate, setShowCreate] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | null>(null);
  const [detailTask, setDetailTask] = useState<Task | null>(null);
  const [showDone, setShowDone] = useState(false);

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

  const handleDeleteDone = (id: string) => {
    deleteDoneTask(id);
    onToast('Task endgültig gelöscht', 'info');
  };

  const handleUndoDone = (id: string) => {
    useTaskStore.getState().markUndone(id);
    onToast('Task wieder geöffnet', 'info');
  };

  const handleToggleDone = (id: string) => {
    if (tasks[id]) {
      markDone(id);
      onToast('Task erledigt! 🎉', 'success');
    } else {
      useTaskStore.getState().markUndone(id);
      onToast('Task wieder geöffnet', 'info');
    }
  };

  const doneList = useMemo(() => Object.values(done), [done]);

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
              onClick={(t) => setDetailTask(t)}
            />
          ))}
        </div>
      )}

      {/* Done Tasks Section */}
      {doneList.length > 0 && (
        <div className="mt-6">
          <button
            onClick={() => setShowDone(!showDone)}
            className="flex items-center gap-2 text-sm font-medium text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 transition-colors mb-2"
          >
            {showDone ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
            <span>Erledigte Tasks ({doneList.length})</span>
            <span className="text-xs text-gray-400">
              {showDone ? 'ausblenden' : 'anzeigen'}
            </span>
          </button>

          {showDone && (
            <div className="space-y-1.5 animate-fade-in">
              {doneList.map((task) => (
                <div
                  key={task.id}
                  className="card p-2.5 flex items-center gap-3 opacity-60 hover:opacity-90 transition-opacity"
                >
                  <div className="flex-1 min-w-0">
                    <p className="text-sm text-gray-500 dark:text-gray-500 line-through truncate">
                      {task.description}
                    </p>
                    <div className="flex items-center gap-2 mt-0.5 text-[11px] text-gray-400">
                      <span>{task.hours}h</span>
                      <span>•</span>
                      <span>{new Date(task.delivery).toLocaleDateString('de-DE')}</span>
                      {task.doneAt && (
                        <>
                          <span>•</span>
                          <span>Erledigt: {new Date(task.doneAt).toLocaleDateString('de-DE')}</span>
                        </>
                      )}
                    </div>
                  </div>
                  <button
                    onClick={() => handleUndoDone(task.id)}
                    className="text-gray-400 hover:text-primary p-1.5 rounded-lg transition-colors"
                    aria-label="Wieder öffnen"
                    title="Wieder öffnen"
                  >
                    <Undo size={16} />
                  </button>
                  <button
                    onClick={() => handleDeleteDone(task.id)}
                    className="text-gray-400 hover:text-danger p-1.5 rounded-lg transition-colors"
                    aria-label="Endgültig löschen"
                    title="Endgültig löschen"
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      <EditTaskModal task={editingTask} onClose={() => setEditingTask(null)} />

      {detailTask && (
        <TaskDetail
          task={detailTask}
          onClose={() => setDetailTask(null)}
          onSave={(id, updates) => {
            updateTask(id, updates);
            onToast('Task aktualisiert', 'success');
          }}
          onDelete={handleDelete}
          onMarkDone={(id) => {
            markDone(id);
            onToast('Task erledigt! 🎉', 'success');
          }}
        />
      )}
    </div>
  );
}