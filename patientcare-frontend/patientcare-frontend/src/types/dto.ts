import type { AgeCategory, Gender, GenderRequirement, SwapRequestStatus } from './enums';

// --- Auth (AuthDtos.cs) ---
export interface LoginRequest {
  email: string;
  password: string;
}
export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  caregiverGender: Gender;
  dateOfBirth: string;
}
export interface AuthResponse {
  token: string;
  expiresAt: string;
  fullName: string;
  /** 'User' או 'Admin'. */
  role?: string;
}

/** תשובת עימוד מהשרת (PagedResult). */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// --- Patients (PatientDtos.cs) ---
export interface PatientResponse {
  id: number;
  medicalCode: string;
  patientGender: Gender;
  dateOfBirth: string;
  department: string;
  age: number;
  ageCategory: AgeCategory;
  managerCaregiverId: number | null;
  isManager: boolean;
}
export interface PatientMemberResponse {
  caregiverId: number;
  fullName: string;
  isManager: boolean;
}
export interface InviteCodeResponse {
  inviteCode: string;
}
export interface JoinPatientRequest {
  inviteCode: string;
}
export interface CreatePatientRequest {
  medicalCode: string;
  patientGender: Gender;
  dateOfBirth: string;
  department: string;
}
export interface ChangePatientManagerRequest {
  newManagerCaregiverId: number;
}

// --- Care shifts (CareShiftDtos.cs) ---
export interface CreateCareShiftRequest {
  patientId: number;
  startTime: string;
  endTime: string;
  requiredGender: GenderRequirement;
  capacity: number;
  tags?: string[];
}

export interface CareShiftResponse {
  id: number;
  patientId: number;
  patientMedicalCode: string;
  startTime: string;
  endTime: string;
  requiredGender: GenderRequirement;
  capacity: number;
  currentCount: number;
  isFull: boolean;
  tags: string[];
  caregivers: ShiftCaregiver[];
}
export interface ShiftCaregiver {
  caregiverId: number;
  fullName: string;
}

// --- Swap requests (SwapRequestDtos.cs) ---
// שים לב: אין targetCaregiverId ביצירה — ה-Backend מחשב אותו מתוך משמרת היעד.
export interface CreateSwapRequestDto {
  careShiftId: number;
  targetCareShiftId: number;
  reason?: string;
}
export interface SwapRequestResponse {
  id: number;
  requestorId: number;
  requestorName: string;
  targetCaregiverId: number | null;
  targetCaregiverName: string | null;
  careShiftId: number;
  status: SwapRequestStatus;
  reason: string | null;
  requestedAt: string;
  resolvedAt: string | null;
  targetCareShiftId: number;
  responseReason: string | null;
}

export interface ApiErrorBody {
  message?: string;
  title?: string;
}
