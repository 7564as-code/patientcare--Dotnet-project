import { api } from './client';
import type { CareShiftResponse, CreateCareShiftRequest, PagedResult } from '@/types/dto';

export interface ShiftPageParams {
  page?: number;
  pageSize?: number;
  patientId?: number;
  from?: string;
  to?: string;
  onlyWithOpenSlots?: boolean;
}

const MAX_PAGE_SIZE = 200;

export const shiftsApi = {
  /** עמוד אחד מהשרת. */
  page: (params: ShiftPageParams = {}) =>
    api.get<PagedResult<CareShiftResponse>>('/CareShifts', { params }).then((r) => r.data),

  /** כל המשמרות שלי: עובר על כל העמודים (לוח השבוע צריך את הרשימה המלאה). */
  list: async (): Promise<CareShiftResponse[]> => {
    const all: CareShiftResponse[] = [];
    let page = 1;
    for (;;) {
      const res = await shiftsApi.page({ page, pageSize: MAX_PAGE_SIZE });
      all.push(...res.items);
      if (page >= res.totalPages) return all;
      page += 1;
    }
  },

  get: (id: number) => api.get<CareShiftResponse>(`/CareShifts/${id}`).then((r) => r.data),
  create: (body: CreateCareShiftRequest) => api.post<CareShiftResponse>('/CareShifts', body).then((r) => r.data),
  register: (id: number) => api.post<CareShiftResponse>(`/CareShifts/${id}/register`).then((r) => r.data),
};
