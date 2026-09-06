import type { Task, CalendarEvent, Settings, WeekDay, UndistributedTask } from '../types';
import { calculateWeighting } from './weighting';
import { toLocalISODate } from './dateUtils';

function toISODate(date: Date): string {
  return toLocalISODate(date);
}

function getWorkHoursForDay(date: Date, events: CalendarEvent[]): number {
  const dayStart = new Date(date);
  dayStart.setHours(0, 0, 0, 0);
  const dayEnd = new Date(date);
  dayEnd.setHours(23, 59, 59, 999);

  let workHours = 0;
  for (const event of events) {
    const eventStart = new Date(event.start);
    const eventEnd = new Date(event.end);
    if (eventStart > dayEnd || eventEnd < dayStart) continue;
    if (event.type === 'WorkHours') {
      const overlapStart = eventStart < dayStart ? dayStart : eventStart;
      const overlapEnd = eventEnd > dayEnd ? dayEnd : eventEnd;
      workHours += (overlapEnd.getTime() - overlapStart.getTime()) / (1000 * 60 * 60);
    }
  }
  return workHours;
}

function getAvailableHoursForDay(date: Date, events: CalendarEvent[]): number {
  const workHours = getWorkHoursForDay(date, events);
  // Only use work-hours events. If none exist for this day, availability is 0.
  // This means all tasks go to 'undistributed' until the user plans work hours in the Plan tab.
  return workHours;
}

export interface DistributionResult {
  days: WeekDay[];
  undistributed: UndistributedTask[];
}

export function distributeTasks(
  tasks: Task[],
  events: CalendarEvent[],
  settings: Settings
): DistributionResult {
  const pendingTasks = tasks.filter((t) => !t.done);
  const sorted = [...pendingTasks].sort(
    (a, b) =>
      calculateWeighting(b, pendingTasks, settings) -
      calculateWeighting(a, pendingTasks, settings)
  );

  const days: WeekDay[] = [];
  const dayAvailability: number[] = [];
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  for (let i = 0; i < settings.maxPlanableDays; i++) {
    const date = new Date(today);
    date.setDate(date.getDate() + i);
    const isoDate = toISODate(date);
    const available = getAvailableHoursForDay(date, events);
    const workHours = getWorkHoursForDay(date, events);

    days.push({
      date: isoDate,
      tasks: [],
      plannedHours: 0,
      availableHours: workHours,
    });
    dayAvailability.push(available);
  }

  const undistributed: UndistributedTask[] = [];

  for (const task of sorted) {
    let remainingHours = task.hours;
    let partNumber = 0;
    const totalParts = Math.ceil(task.hours / Math.max(...dayAvailability.filter(a => a > 0), 1));

    for (let i = 0; i < days.length && remainingHours > 0; i++) {
      const available = dayAvailability[i] - days[i].plannedHours;
      if (available <= 0) continue;

      // Round to 5-minute increments (1/12 of an hour)
      const hoursToPlace = Math.min(available, remainingHours);
      const roundedHours = Math.round(hoursToPlace * 12) / 12;
      if (roundedHours < 5/60 && remainingHours > roundedHours + 5/60) continue;

      partNumber++;
      const taskPart: Task = {
        ...task,
        hours: roundedHours,
        description: totalParts > 1 ? `${task.description} [part ${partNumber}/${totalParts}]` : task.description,
      };

      days[i].tasks.push(taskPart);
      days[i].plannedHours += roundedHours;
      remainingHours -= roundedHours;
    }

    if (remainingHours > 0.01) {
      undistributed.push({
        task,
        remainingHours: Math.round(remainingHours * 12) / 12,
        reason: 'Nicht genügend Arbeitszeit verfügbar',
      });
    }
  }

  return { days, undistributed };
}

export function getTasksForDate(
  tasks: Task[],
  events: CalendarEvent[],
  settings: Settings,
  date: string
): WeekDay | undefined {
  const { days } = distributeTasks(tasks, events, settings);
  return days.find((d) => d.date === date);
}