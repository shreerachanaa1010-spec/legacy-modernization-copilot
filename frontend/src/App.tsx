import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { HomePage } from './pages/HomePage';
import { ResultsPage } from './pages/ResultsPage';
import { IssueDetailPage } from './pages/IssueDetailPage';

const basename = import.meta.env.DEV ? '/' : '/legacy-modernization-copilot';

export default function App() {
  return (
    <BrowserRouter basename={basename}>
      <Routes>
        <Route element={<Layout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/results" element={<ResultsPage />} />
          <Route path="/issue/:index" element={<IssueDetailPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
