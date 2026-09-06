import { useState, useEffect, useCallback } from 'react';
import { Sidebar } from './components/Sidebar';
import { BottomNav } from './components/BottomNav';
import { StatusBar } from './components/StatusBar';
import { Toast, useToasts } from './components/Toast';
import { QuickCapture } from './components/QuickCapture';
import { Home } from './pages/Home';
import { Todo } from './pages/Todo';
import { Plan } from './pages/Plan';
import { Week } from './pages/Week';
import { Settings } from './pages/Settings';
import { LoginScreen } from './components/LoginScreen';
import { useTaskStore } from './stores/taskStore';
import { useCalendarStore } from './stores/calendarStore';
import { useSettingsStore } from './stores/settingsStore';
import { useUserStore } from './stores/userStore';
import { useTimerStore } from './stores/timerStore';
import { createSampleTasks } from './lib/sampleData';
import type { PageId } from './types';

export function App() {
  const [activePage, setActivePage] = useState<PageId>('home');
  const [isMobile, setIsMobile] = useState(false);
  const [quickCaptureOpen, setQuickCaptureOpen] = useState(false);
  const { toasts, addToast, dismissToast } = useToasts();

  const loadTasks = useTaskStore((s) => s.loadTasks);
  const addTask = useTaskStore((s) => s.addTask);
  const loadEvents = useCalendarStore((s) => s.loadEvents);
  const loadSettings = useSettingsStore((s) => s.loadSettings);
  const user = useUserStore((s) => s.user);
  const loadUser = useUserStore((s) => s.loadUser);
  const tick = useTimerStore((s) => s.tick);

  // Load persisted data on mount
  useEffect(() => {
    loadUser();
    loadTasks();
    loadEvents();
    loadSettings();
  }, [loadTasks, loadEvents, loadSettings, loadUser]);

  // Seed sample tasks on first login if no tasks exist
  useEffect(() => {
    if (user && Object.keys(useTaskStore.getState().tasks).length === 0) {
      const samples = createSampleTasks();
      samples.forEach((s) => addTask(s));
    }
  }, [user, addTask]);

  // Timer tick
  useEffect(() => {
    const interval = setInterval(() => { tick(); }, 1000);
    return () => clearInterval(interval);
  }, [tick]);

  // Responsive
  useEffect(() => {
    const checkMobile = () => setIsMobile(window.innerWidth < 768);
    checkMobile();
    window.addEventListener('resize', checkMobile);
    return () => window.removeEventListener('resize', checkMobile);
  }, []);

  // Keyboard shortcuts
  useEffect(() => {
    const handleKey = (e: KeyboardEvent) => {
      if (e.ctrlKey && !e.shiftKey) {
        if (e.key === '1') { e.preventDefault(); setActivePage('home'); }
        else if (e.key === '2') { e.preventDefault(); setActivePage('todo'); }
        else if (e.key === '3') { e.preventDefault(); setActivePage('plan'); }
        else if (e.key === '4') { e.preventDefault(); setActivePage('week'); }
        else if (e.key === '5') { e.preventDefault(); setActivePage('settings'); }
        else if (e.key === 'n') { e.preventDefault(); setActivePage('todo'); }
      }
      if (e.ctrlKey && e.shiftKey && e.key === 'N') {
        e.preventDefault();
        setQuickCaptureOpen(true);
      }
    };
    window.addEventListener('keydown', handleKey);
    return () => window.removeEventListener('keydown', handleKey);
  }, []);

  const handleNavigate = useCallback((page: PageId) => {
    setActivePage(page);
  }, []);

  // Show login screen if no user
  if (!user) {
    return <LoginScreen />;
  }

  return (
    <div className="flex flex-col h-screen bg-bg dark:bg-dark-bg">
      <div className="flex flex-1 overflow-hidden">
        {!isMobile && <Sidebar activePage={activePage} onNavigate={handleNavigate} />}

        <main className="flex-1 overflow-y-auto">
          {activePage === 'home' && <Home onNavigate={handleNavigate} />}
          {activePage === 'todo' && <Todo onToast={addToast} />}
          {activePage === 'plan' && <Plan />}
          {activePage === 'week' && <Week onToast={addToast} />}
          {activePage === 'settings' && <Settings onToast={addToast} />}
        </main>
      </div>

      {!isMobile && <StatusBar />}
      {isMobile && <BottomNav activePage={activePage} onNavigate={handleNavigate} />}

      <Toast toasts={toasts} onDismiss={dismissToast} />
      <QuickCapture open={quickCaptureOpen} onClose={() => setQuickCaptureOpen(false)} />
    </div>
  );
}