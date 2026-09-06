import { create } from 'zustand';
import type { User } from '../types';
import { loadFromStorage, saveToStorage } from '../lib/storage';

interface UserState {
  user: User | null;
  login: (username: string) => void;
  logout: () => void;
  loadUser: () => void;
}

const STORAGE_KEY = '***';

export const useUserStore = create<UserState>((set) => ({
  user: null,

  login: (username: string) => {
    const user: User = {
      id: `user_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`,
      username,
      createdAt: new Date().toISOString(),
    };
    saveToStorage(STORAGE_KEY, user);
    set({ user });
  },

  logout: () => {
    localStorage.removeItem(STORAGE_KEY);
    set({ user: null });
  },

  loadUser: () => {
    const user = loadFromStorage<User | null>(STORAGE_KEY, null);
    set({ user: user as User | null });
  },
}));