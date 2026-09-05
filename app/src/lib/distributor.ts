import type { Task, CalendarEvent, Settings, WeekDay } from '../types';
import { calculateWeighting } from './weighting';

/**
 * Get the available hours for a given day based on calendar events.
 * Only WorkHours events allow task placement; FixedAppointment, FreeTime, Sleep block tasks.
 */
function getAvailableHoursForDay(
  date: Date,
  events: CalendarEvent[],
  settings: Settings
): number {
  const dayStart = new Date(date);
  dayStart.setHours(0, 0, 0, 0);
  const dayEnd = new Date(date);
  dayEnd.setHours(23, 59, 59, 999);

  let workHours = 0;

  for (const event of events) {
    const eventStart = new Date(event.start);
    const eventEnd = new Date(event.end);

    // Check if event is on this day
    if (eventStart > dayEnd || eventEnd < dayStart) continue;

    if (event.type === 'WorkHours') {
      const overlapStart = eventStart < dayStart ? dayStart : eventStart;
      const overlapEnd = eventEnd > dayEnd ? dayEnd : eventEnd;
      workHours += (overlapEnd.getTime() - overlapStart.getTime()) / (1000 * 60 * 60);
    }
  }

  // If no work hours events defined, use settings default
  const defaultHours = settings.workEndHour - settings.workStartHour;
  const availableFromEvents = workHours > 0 ? workHours : defaultHours;

  return Math.min(availableFromEvents, settings.maxHoursPerDay);
}

/**
 * Format a date as ISO date string (YYYY-MM-DD).
 */
function toISODate(date: Date): string {
  return date.toISOString().split('T')[0];
}

/**
 * Distribute tasks across available days using first-fit-decreasing with split.
 * Tasks are sorted by weighting (descending), then placed in the first available slot.
 * Large tasks are split across multiple days if needed.
 */
export function distributeTasks(
  tasks: Task[],
  events: CalendarEvent[],
  settings: Settings
): WeekDay[] {
  // Only distribute undone tasks
  const pendingTasks = tasks.filter((t) => !t.done);
  const sorted = [...pendingTasks].sort(
    (a, b) =>
      calculateWeighting(b, pendingTasks, settings) -
      calculateWeighting(a, pendingTasks, settings)
  );

  // Build day availability map
  const days: WeekDay[] = [];
  const dayAvailability: number[] = [];

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  for (let i = 0; i < settings.maxPlanableDays; i++) {
    const date = new Date(today);
    date.setDate(date.getDate() + i);
    const isoDate = toISODate(date);
    const available = getAvailableHoursForDay(date, events, settings);

    days.push({ date: isoDate, tasks: [], plannedHours: 0 });
    dayAvailability.push(available);
  }

  // First-fit-decreasing with split
  for (const task of sorted) {
    let remainingHours = task.hours;
    let partNumber = 0;

    for (let i = 0; i < days.length && remainingHours > 0; i++) {
      const available = dayAvailability[i] - days[i].plannedHours;
      if (available <= 0) continue;

      const hoursToPlace = Math.min(available, remainingHours);
      if (hoursToPlace < 0.5 && remainingHours > hoursToPlace + 0.5) {
        // Too small to split — skip unless it's the last bit
        continue;
      }

      partNumber++;
      const taskPart: Task = {
        ...task,
        hours: hoursToPlace,
        description:
          remainingHours < task.hours
            ? `${task.description} [part ${partNumber}]`
            : task.description,
      };

      days[i].tasks.push(taskPart);
      days[i].plannedHours += hoursToPlace;
      remainingHours -= hoursToPlace;
    }

    // If task couldn't be fully placed, put remaining in the last day with space
    if (remainingHours > 0) {
      for (let i = days.length - 1; i >= 0; i--) {
        const available = settings.maxHoursPerDay - days[i].plannedHours;
        if (available > 0) {
          const hoursToPlace = Math.min(available, remainingHours);
          partNumber++;
          days[i].tasks.push({
            ...task,
            hours: hoursToPlace,
            description: `${task.description} [part ${partNumber}]`,
          });
          days[i].plannedHours += hoursToPlace;
          remainingHours -= hoursToPlace;
          if (remainingHours <= 0) break;
        }
      }
    }
  }

  return days;
}

/**
 * Get tasks distributed for a specific date.
 */
export function getTasksForDate(
  tasks: Task[],
  events: CalendarEvent[],
  settings: Settings,
  date: string
): WeekDay | undefined {
  const distributed = distributeTasks(tasks, events, settings);
  return distributed.find((d) => d.date === date);
}