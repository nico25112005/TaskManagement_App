import { Home, CheckSquare, Calendar, Columns, Settings as SettingsIcon } from 'lucide-react';
import type { PageId } from '../types';

interface BottomNavProps {
  activePage: PageId;
  onNavigate: (page: PageId) => void;
}

const navItems: { id: PageId; label: string; icon: typeof Home }[] = [
  { id: 'home', label: 'Home', icon: Home },
  { id: 'todo', label: 'Todo', icon: CheckSquare },
  { id: 'plan', label: 'Plan', icon: Calendar },
  { id: 'week', label: 'Week', icon: Columns },
  { id: 'settings', label: 'Settings', icon: SettingsIcon },
];

export function BottomNav({ activePage, onNavigate }: BottomNavProps) {
  return (
    <nav className="h-[56px] bg-panel dark:bg-dark-panel border-t border-border dark:border-dark-border flex items-center justify-around px-1 shrink-0">
      {navItems.map((item) => {
        const Icon = item.icon;
        const isActive = activePage === item.id;
        return (
          <button
            key={item.id}
            onClick={() => onNavigate(item.id)}
            className={`flex flex-col items-center justify-center gap-0.5 px-3 py-1 rounded-lg transition-all min-h-[44px] min-w-[44px] ${
              isActive
                ? 'text-primary'
                : 'text-gray-500 dark:text-gray-400'
            }`}
            aria-label={item.label}
          >
            <Icon size={20} />
            <span className="text-[9px] font-medium">{item.label}</span>
          </button>
        );
      })}
    </nav>
  );
}