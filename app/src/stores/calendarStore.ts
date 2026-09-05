import { create } from 'zustand';
import type { CalendarEvent } from '../types';
import { loadFromStorage, saveToStorage } from '../lib/storage';

interface CalendarState {
  events: CalendarEvent[];
  addEvent: (event: Omit<CalendarEvent, 'id'>) => void;
  updateEvent: (id: string, updates: Partial<CalendarEvent>) => void;
  deleteEvent: (id: string) => void;
  loadEvents: () => void;
}

const STORAGE_KEY = '***';

function generateId(): string {
  return `evt_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
}

export const useCalendarStore = create<CalendarState>((set) => ({
  events: [],

  addEvent: (eventData) => {
    const event: CalendarEvent = { ...eventData, id: generateId() };
    set((state) => {
      const events = [...state.events, event];
      saveToStorage(STORAGE_KEY, events);
      return { events };
    });
  },

  updateEvent: (id, updates) => {
    set((state) => {
      const events = state.events.map((e) =>
        e.id === id ? { ...e, ...updates } : e
      );
      saveToStorage(STORAGE_KEY, events);
      return { events };
    });
  },

  deleteEvent: (id) => {
    set((state) => {
      const events = state.events.filter((e) => e.id !== id);
      saveToStorage(STORAGE_KEY, events);
      return { events };
    });
  },

  loadEvents: () => {
    const events = loadFromStorage<CalendarEvent[]>(STORAGE_KEY, []);
    set({ events });
  },
}));