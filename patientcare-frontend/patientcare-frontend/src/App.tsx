import { Navigate, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from '@/auth/ProtectedRoute';
import { AppShell } from '@/components/layout/AppShell';
import { LoginPage } from '@/pages/LoginPage';
import { RegisterPage } from '@/pages/RegisterPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { PatientsPage } from '@/pages/PatientsPage';
import { PatientDetailsPage } from '@/pages/PatientDetailsPage';
import { ShiftsPage } from '@/pages/ShiftsPage';
import { SwapRequestsPage } from '@/pages/SwapRequestsPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route index element={<DashboardPage />} />
          <Route path="shifts" element={<ShiftsPage />} />
          <Route path="swaps" element={<SwapRequestsPage />} />
          <Route path="patients" element={<PatientsPage />} />
          <Route path="patients/:id" element={<PatientDetailsPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
