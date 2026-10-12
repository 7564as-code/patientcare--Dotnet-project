import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';

// ה-Backend לא מגדיר CORS, לכן בפיתוח הקריאות עוברות דרך proxy של Vite.
// כתובת היעד נקבעת ב-VITE_DEV_PROXY_TARGET (ברירת מחדל: פרופיל http של ה-API).
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const target = env.VITE_DEV_PROXY_TARGET || 'http://localhost:5229';
  return {
    plugins: [react()],
    resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
    server: {
      port: 5173,
      proxy: mode === 'development' ? { '/api': { target, changeOrigin: true, secure: false } } : undefined,
    },
  };
});
