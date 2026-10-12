import { api } from './client';
import type { AuthResponse, LoginRequest, RegisterRequest } from '@/types/dto';

export const authApi = {
  register: (body: RegisterRequest) => api.post<AuthResponse>('/Auth/register', body).then((r) => r.data),
  login: (body: LoginRequest) => api.post<AuthResponse>('/Auth/login', body).then((r) => r.data),
};
