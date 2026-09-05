import { create } from 'zustand';
import type { Task } from '../types';
import { loadFromStorage, saveToStorage } from '../lib/storage';

interface TaskState {
  tasks: Record<string, Task>;
  done: Record<string, Task>;
  addTask: (task: Omit<Task, 'id' | 'done' | 'doneAt' | 'dependentTasks'>) => void;
  updateTask: (id: string, updates: Partial<Task>) => void;
  deleteTask: (id: string) => void;
  markDone: (id: string) => void;
  markUndone: (id: string) => void;
  loadTasks: () => void;
}

const STORAGE_KEY = '***';
const DONE_KEY = '***';

function generateId(): string {
  return `task_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
}

export const useTaskStore = create<TaskState>((set) => ({
  tasks: {},
  done: {},

  addTask: (taskData) => {
    const task: Task = {
      ...taskData,
      id: generateId(),
      done: false,
      doneAt: null,
      dependentTasks: [],
    };
    set((state) => {
      const tasks = { ...state.tasks, [task.id]: task };
      saveToStorage(STORAGE_KEY, tasks);
      return { tasks };
    });
  },

  updateTask: (id, updates) => {
    set((state) => {
      if (!state.tasks[id]) return state;
      const tasks = {
        ...state.tasks,
        [id]: { ...state.tasks[id], ...updates },
      };
      saveToStorage(STORAGE_KEY, tasks);
      return { tasks };
    });
  },

  deleteTask: (id) => {
    set((state) => {
      const tasks = { ...state.tasks };
      delete tasks[id];
      saveToStorage(STORAGE_KEY, tasks);
      return { tasks };
    });
  },

  markDone: (id) => {
    set((state) => {
      const task = state.tasks[id];
      if (!task) return state;
      const doneTask: Task = { ...task, done: true, doneAt: new Date().toISOString() };
      const tasks = { ...state.tasks };
      delete tasks[id];
      const done = { ...state.done, [id]: doneTask };
      saveToStorage(STORAGE_KEY, tasks);
      saveToStorage(DONE_KEY, done);
      return { tasks, done };
    });
  },

  markUndone: (id) => {
    set((state) => {
      const task = state.done[id];
      if (!task) return state;
      const undoneTask: Task = { ...task, done: false, doneAt: null };
      const done = { ...state.done };
      delete done[id];
      const tasks = { ...state.tasks, [id]: undoneTask };
      saveToStorage(STORAGE_KEY, tasks);
      saveToStorage(DONE_KEY, done);
      return { tasks, done };
    });
  },

  loadTasks: () => {
    const tasks = loadFromStorage<Record<string, Task>>(STORAGE_KEY, {});
    const done = loadFromStorage<Record<string, Task>>(DONE_KEY, {});
    set({ tasks, done });
  },
}));