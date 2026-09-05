export interface Task {
  id: string;
  description: string;
  hours: number;
  delivery: string; // ISO date
  importance: 1 | 2 | 3;
  done: boolean;
  doneAt: string | null;
  dependentTasks: string[];
}

export type CalendarEventType = 'FixedAppointment' | 'WorkHours' | 'FreeTime' | 'Sleep';

export interface CalendarEvent {
  id: string;
  title: string;
  start: string; // ISO datetime
  end: string; // ISO datetime
  type: CalendarEventType;
  recurrence?: string | null;
}

export interface Settings {
  maxHoursPerDay: number;
  maxPlanableDays: number;
  workStartHour: number;
  workEndHour: number;
  darkMode: boolean;
  focusBlockMinutes: number;
  shortBreakMinutes: number;
  longBreakMinutes: number;
  blocksBeforeLongBreak: number;
}

export interface WeekDay {
  date: string; // ISO date
  tasks: Task[];
  plannedHours: number;
}

export type PageId = 'home' | 'todo' | 'plan' | 'week' | 'settings';

export type SortOption = 'due' | 'importance' | 'hours' | 'description';