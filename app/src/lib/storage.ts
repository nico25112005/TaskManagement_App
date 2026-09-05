/**
 * Load a value from localStorage with JSON parsing and fallback.
 */
export function loadFromStorage<T>(key: string, fallback: T): T {
  try {
    const raw = localStorage.getItem(key);
    if (raw === null) return fallback;
    return JSON.parse(raw) as T;
  } catch {
    return fallback;
  }
}

/**
 * Save a value to localStorage as JSON.
 */
export function saveToStorage<T>(key: string, value: T): void {
  try {
    localStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Storage might be full or unavailable — fail silently
  }
}

/**
 * Remove a key from localStorage.
 */
export function removeFromStorage(key: string): void {
  try {
    localStorage.removeItem(key);
  } catch {
    // ignore
  }
}

/**
 * Export all app data as a single JSON object.
 */
export function exportAllData(): Record<string, unknown> {
  const keys = [
    '***',
    '***',
    '***',
  ];
  const data: Record<string, unknown> = {};
  for (const key of keys) {
    const raw = localStorage.getItem(key);
    if (raw !== null) {
      data[key] = JSON.parse(raw);
    }
  }
  return data;
}

/**
 * Import app data from a JSON object.
 */
export function importAllData(data: Record<string, unknown>): void {
  for (const [key, value] of Object.entries(data)) {
    saveToStorage(key, value);
  }
}

/**
 * Clear all app data from localStorage.
 */
export function clearAllData(): void {
  const keys = [
    '***',
    '***',
    '***',
  ];
  for (const key of keys) {
    removeFromStorage(key);
  }
}