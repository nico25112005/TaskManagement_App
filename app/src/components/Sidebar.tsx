import { Home, CheckSquare, Calendar, Columns, Settings as SettingsIcon } from 'lucide-react';
import type { PageId } from '../types';

interface SidebarProps {
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

export function Sidebar({ activePage, onNavigate }: SidebarProps) {
  return (
    <nav className="w-[72px] bg-panel dark:bg-dark-panel border-r border-border dark:border-dark-border flex flex-col items-center py-4 gap-2 shrink-0">
      {navItems.map((item) => {
        const Icon = item.icon;
        const isActive = activePage === item.id;
        return (
          <button
            key={item.id}
            onClick={() => onNavigate(item.id)}
            className={`flex flex-col items-center gap-1 px-2 py-2 rounded-lg transition-all min-h-[44px] w-full ${
              isActive
                ? 'text-primary border-l-2 border-primary bg-primary/10'
                : 'text-gray-500 dark:text-gray-400 hover:bg-hover hover:text-primary'
            }`}
            aria-label={item.label}
          >
            <Icon size={22} />
            <span className="text-[10px] font-medium">{item.label}</span>
          </button>
        );
      })}
    </nav>
  );
}