import { Outlet } from 'react-router-dom';
import { Navbar } from './Navbar';
import { StoreProvider } from '../store/useStore';

export function Layout() {
  return (
    <StoreProvider>
      <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950 text-slate-100">
        <Navbar />
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <Outlet />
        </main>
      </div>
    </StoreProvider>
  );
}
