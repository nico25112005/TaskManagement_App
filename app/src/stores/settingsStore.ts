import { create } from 'zustand';
import type { Settings } from '../types';
import { loadFromStorage, saveToStorage } from '../lib/storage';

const STORAGE_KEY = '***';

const defaultSettings: Settings = {
  maxHoursPerDay: 4,
  maxPlanableDays: 14,
  workStartHour: 8,
  workEndHour: 18,
  darkMode: false,
  focusBlockMinutes: 25,
  shortBreakMinutes: 5,
  longBreakMinutes: 15,
  blocksBeforeLongBreak: 4,
};

interface SettingsState {
  settings: Settings;
  updateSettings: (updates: Partial<Settings>) => void;
  toggleDarkMode: () => void;
  loadSettings: () => void;
  resetSettings: () => void;
}

export const useSettingsStore = create<SettingsState>((set) => ({
  settings: defaultSettings,

  updateSettings: (updates) => {
    set((state) => {
      const settings = { ...state.settings, ...updates };
      saveToStorage(STORAGE_KEY, settings);
      applyDarkMode(settings.darkMode);
      return { settings };
    });
  },

  toggleDarkMode: () => {
    set((state) => {
      const settings = { ...state.settings, darkMode: !state.settings.darkMode };
      saveToStorage(STORAGE_KEY, settings);
      applyDarkMode(settings.darkMode);
      return { settings };
    });
  },

  loadSettings: () => {
    const settings = loadFromStorage<Settings>(STORAGE_KEY, defaultSettings);
    applyDarkMode(settings.darkMode);
    set({ settings });
  },

  resetSettings: () => {
    saveToStorage(STORAGE_KEY, defaultSettings);
    applyDarkMode(defaultSettings.darkMode);
    set({ settings: defaultSettings });
  },
}));

function applyDarkMode(enabled: boolean) {
  if (enabled) {
    document.documentElement.classList.add('dark');
  } else {
    document.documentElement.classList.remove('dark');
  }
}