import { AgeCategory, Gender, GenderRequirement, SwapRequestStatus } from '@/types/enums';

export const genderLabel = (g: Gender) => (g === Gender.Male ? 'זכר' : 'נקבה');

export const genderRequirementLabel = (g: GenderRequirement) =>
  g === GenderRequirement.Male ? 'גברים בלבד' : g === GenderRequirement.Female ? 'נשים בלבד' : 'ללא הגבלה';

export const ageCategoryLabel = (c: AgeCategory) =>
  c === AgeCategory.Child ? 'ילד/ה' : c === AgeCategory.Adult ? 'בוגר/ת' : 'מבוגר/ת (65+)';

export type Tone = 'neutral' | 'brand' | 'indigo' | 'amber' | 'rose' | 'green';

export const swapStatusMeta: Record<SwapRequestStatus, { label: string; tone: Tone }> = {
  [SwapRequestStatus.Pending]: { label: 'ממתינה', tone: 'amber' },
  [SwapRequestStatus.Approved]: { label: 'אושרה', tone: 'green' },
  [SwapRequestStatus.Rejected]: { label: 'נדחתה', tone: 'rose' },
  [SwapRequestStatus.Cancelled]: { label: 'בוטלה', tone: 'neutral' },
};

export const initials = (name: string) =>
  name.trim().split(/\s+/).slice(0, 2).map((p) => p[0]).join('');
