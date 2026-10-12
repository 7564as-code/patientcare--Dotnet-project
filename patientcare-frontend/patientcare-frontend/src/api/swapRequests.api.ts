import { api } from './client';
import type { CreateSwapRequestDto, SwapRequestResponse } from '@/types/dto';

export const swapRequestsApi = {
  create: (body: CreateSwapRequestDto) => api.post<SwapRequestResponse>('/SwapRequests', body).then((r) => r.data),
  approve: (id: number) => api.post<SwapRequestResponse>(`/SwapRequests/${id}/approve`).then((r) => r.data),
  reject: (id: number, reason?: string) =>
    api.post<SwapRequestResponse>(`/SwapRequests/${id}/reject`, { reason: reason?.trim() || null }).then((r) => r.data),
  /** ביטול בקשה שלי שעדיין ממתינה (DELETE, מחזיר 204). */
  cancel: (id: number) => api.delete(`/SwapRequests/${id}`).then(() => undefined),
  incoming: () => api.get<SwapRequestResponse[]>('/SwapRequests/incoming').then((r) => r.data),
  mine: () => api.get<SwapRequestResponse[]>('/SwapRequests/mine').then((r) => r.data),
  pendingForMe: () => api.get<SwapRequestResponse[]>('/SwapRequests/pending-for-me').then((r) => r.data),
};
