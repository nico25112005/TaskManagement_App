import { Search, ArrowUpDown } from 'lucide-react';
import type { SortOption } from '../types';

interface FilterBarProps {
  filter: string;
  setFilter: (value: string) => void;
  sort: SortOption;
  setSort: (value: SortOption) => void;
}

const sortOptions: { value: SortOption; label: string }[] = [
  { value: 'due', label: 'Fälligkeit' },
  { value: 'importance', label: 'Wichtigkeit' },
  { value: 'hours', label: 'Stunden' },
  { value: 'description', label: 'Beschreibung' },
];

export function FilterBar({ filter, setFilter, sort, setSort }: FilterBarProps) {
  return (
    <div className="flex items-center gap-2 flex-wrap">
      <div className="relative flex-1 min-w-[200px]">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
        <input
          type="text"
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          placeholder="Tasks durchsuchen..."
          className="input w-full pl-9"
          aria-label="Tasks filtern"
        />
      </div>
      <div className="relative">
        <ArrowUpDown size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none" />
        <select
          value={sort}
          onChange={(e) => setSort(e.target.value as SortOption)}
          className="input pl-9 appearance-none cursor-pointer"
          aria-label="Sortieren nach"
        >
          {sortOptions.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}