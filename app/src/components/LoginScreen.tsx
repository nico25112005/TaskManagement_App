import { useState } from 'react';
import { useUserStore } from '../stores/userStore';

export function LoginScreen() {
  const login = useUserStore((s) => s.login);
  const [username, setUsername] = useState('');
  const [error, setError] = useState('');

  const handleLogin = (e: React.FormEvent) => {
    e.preventDefault();
    if (!username.trim()) {
      setError('Bitte einen Namen eingeben');
      return;
    }
    login(username.trim());
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary/10 via-bg to-highlight/10 p-4">
      <div className="card p-8 max-w-sm w-full">
        <div className="text-center mb-6">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-primary text-white rounded-2xl text-2xl font-bold mb-3">
            TM
          </div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Task Manager</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Plane deine Tasks. Fokussiere deine Zeit.
          </p>
        </div>

        <form onSubmit={handleLogin} className="space-y-4">
          <div>
            <label className="text-xs text-gray-500 dark:text-gray-400 block mb-1">
              Dein Name
            </label>
            <input
              type="text"
              value={username}
              onChange={(e) => {
                setUsername(e.target.value);
                setError('');
              }}
              placeholder="z.B. Nico"
              className="input w-full"
              autoFocus
            />
            {error && <p className="text-xs text-danger mt-1">{error}</p>}
          </div>

          <button type="submit" className="btn-primary w-full py-3">
            Loslegen
          </button>
        </form>

        <p className="text-[11px] text-gray-400 text-center mt-4">
          Deine Daten werden lokal in deinem Browser gespeichert.
        </p>
      </div>
    </div>
  );
}