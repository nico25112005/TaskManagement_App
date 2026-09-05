import type { Task, Settings } from '../types';

const BaseWeight = 1000;
const TimeDecayFactor = 0.05;

/**
 * Calculate the weighting/priority of a task.
 * Higher weight = higher priority.
 *
 * Formula:
 *   BaseWeight / (1 + daysTillDelivery * TimeDecayFactor)
 *   + 300 / importance
 *   + 10 * hours
 */
export function calculateWeighting(
  task: Task,
  _allTasks: Task[],
  _settings: Settings
): number {
  const now = new Date();
  const delivery = new Date(task.delivery);
  const diffMs = delivery.getTime() - now.getTime();
  const daysTillDelivery = Math.max(0, diffMs / (1000 * 60 * 60 * 24));

  const timeFactor = BaseWeight / (1 + daysTillDelivery * TimeDecayFactor);
  const importanceFactor = 300 / task.importance;
  const hoursFactor = 10 * task.hours;

  return timeFactor + importanceFactor + hoursFactor;
}

/**
 * Sort tasks by weighting descending (highest priority first).
 */
export function sortByWeighting(
  tasks: Task[],
  settings: Settings
): Task[] {
  return [...tasks].sort(
    (a, b) =>
      calculateWeighting(b, tasks, settings) -
      calculateWeighting(a, tasks, settings)
  );
}