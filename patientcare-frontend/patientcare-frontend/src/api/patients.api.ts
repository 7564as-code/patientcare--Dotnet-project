import { api } from './client';
import type {
  ChangePatientManagerRequest,
  CreatePatientRequest,
  InviteCodeResponse,
  PagedResult,
  PatientMemberResponse,
  PatientResponse,
} from '@/types/dto';

export interface PatientPageParams {
  page?: number;
  pageSize?: number;
  search?: string;
  department?: string;
  /** 0 = שם, 1 = מקום טיפול, 2 = גיל */
  sortBy?: number;
  descending?: boolean;
}

const MAX_PAGE_SIZE = 200;

export const patientsApi = {
  /** עמוד אחד מהשרת (עימוד, חיפוש וסינון מתבצעים בשרת). */
  page: (params: PatientPageParams = {}) =>
    api.get<PagedResult<PatientResponse>>('/Patients', { params }).then((r) => r.data),

  /** כל החולים שלי: עובר על כל העמודים (למסכים שצריכים את הרשימה המלאה). */
  list: async (): Promise<PatientResponse[]> => {
    const all: PatientResponse[] = [];
    let page = 1;
    for (;;) {
      const res = await patientsApi.page({ page, pageSize: MAX_PAGE_SIZE });
      all.push(...res.items);
      if (page >= res.totalPages) return all;
      page += 1;
    }
  },

  departments: () => api.get<string[]>('/Patients/departments').then((r) => r.data),
  get: (id: number) => api.get<PatientResponse>(`/Patients/${id}`).then((r) => r.data),
  create: (body: CreatePatientRequest) => api.post<PatientResponse>('/Patients', body).then((r) => r.data),
  changeManager: (id: number, body: ChangePatientManagerRequest) =>
    api.put<PatientResponse>(`/Patients/${id}/manager`, body).then((r) => r.data),
  join: (inviteCode: string) => api.post<PatientResponse>('/Patients/join', { inviteCode }).then((r) => r.data),
  members: (id: number) => api.get<PatientMemberResponse[]>(`/Patients/${id}/members`).then((r) => r.data),
  inviteCode: (id: number) => api.get<InviteCodeResponse>(`/Patients/${id}/invite-code`).then((r) => r.data),
  resetInviteCode: (id: number) => api.post<InviteCodeResponse>(`/Patients/${id}/invite-code/reset`).then((r) => r.data),
};
