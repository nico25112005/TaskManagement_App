import { create } from 'zustand';

type TimerMode = 'focus' | 'short-break' | 'long-break';
type TimerStatus = 'idle' | 'running' | 'paused' | 'completed';

interface TimerState {
  status: TimerStatus;
  mode: TimerMode;
  remaining: number; // seconds
  total: number; // seconds for current block
  blocksCompleted: number;
  totalBlocksToday: number;
  setMode: (mode: TimerMode) => void;
  start: (duration: number) => void;
  pause: () => void;
  resume: () => void;
  stop: () => void;
  tick: () => void;
  completeBlock: () => void;
  reset: () => void;
}

export const useTimerStore = create<TimerState>((set, get) => ({
  status: 'idle',
  mode: 'focus',
  remaining: 0,
  total: 0,
  blocksCompleted: 0,
  totalBlocksToday: 0,

  setMode: (mode) => set({ mode }),

  start: (duration) => {
    set({
      status: 'running',
      remaining: duration,
      total: duration,
      mode: 'focus',
    });
  },

  pause: () => {
    const { status } = get();
    if (status === 'running') set({ status: 'paused' });
  },

  resume: () => {
    const { status } = get();
    if (status === 'paused') set({ status: 'running' });
  },

  stop: () => {
    set({
      status: 'idle',
      remaining: 0,
      total: 0,
    });
  },

  tick: () => {
    const { status, remaining } = get();
    if (status !== 'running' || remaining <= 0) return;
    const newRemaining = remaining - 1;
    if (newRemaining <= 0) {
      set({ status: 'completed', remaining: 0 });
      get().completeBlock();
    } else {
      set({ remaining: newRemaining });
    }
  },

  completeBlock: () => {
    const { blocksCompleted, mode } = get();
    if (mode === 'focus') {
      set({
        blocksCompleted: blocksCompleted + 1,
        totalBlocksToday: get().totalBlocksToday + 1,
      });
    }
  },

  reset: () => {
    set({
      status: 'idle',
      remaining: 0,
      total: 0,
      blocksCompleted: 0,
      totalBlocksToday: 0,
    });
  },
}));