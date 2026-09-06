import type { Task } from '../types';
import { toLocalISODate } from './dateUtils';

/**
 * Creates 8 sample tasks for demo/debug purposes.
 * Returns tasks with varied hours, deadlines, and importance.
 */
export function createSampleTasks(): Omit<Task, 'id' | 'done' | 'doneAt' | 'dependentTasks'>[] {
  const today = new Date();
  const iso = (daysFromNow: number) => {
    const d = new Date(today);
    d.setDate(d.getDate() + daysFromNow);
    return toLocalISODate(d);
  };

  return [
    { description: 'React Komponenten refactoren', hours: 3, delivery: iso(2), importance: 1 },
    { description: 'Doku für API schreiben', hours: 2, delivery: iso(5), importance: 2 },
    { description: 'Bug im Login-Fixen', hours: 1.5, delivery: iso(1), importance: 1 },
    { description: 'Datenbank Schema designen', hours: 4, delivery: iso(7), importance: 2 },
    { description: 'Unit Tests schreiben', hours: 2.5, delivery: iso(4), importance: 3 },
    { description: 'Meeting vorbereiten', hours: 1, delivery: iso(0), importance: 1 },
    { description: 'Code Review PR #42', hours: 1.5, delivery: iso(3), importance: 2 },
    { description: 'CI/CD Pipeline einrichten', hours: 3.5, delivery: iso(10), importance: 3 },
  ];
}