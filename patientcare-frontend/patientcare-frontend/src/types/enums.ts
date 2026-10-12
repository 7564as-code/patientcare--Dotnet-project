// ה-API מחזיר ושולח enums כמספרים (אין JsonStringEnumConverter ב-Program.cs).
export const Gender = { Male: 0, Female: 1 } as const;
export type Gender = (typeof Gender)[keyof typeof Gender];

export const GenderRequirement = { Male: 0, Female: 1, Any: 2 } as const;
export type GenderRequirement = (typeof GenderRequirement)[keyof typeof GenderRequirement];

export const AgeCategory = { Child: 0, Adult: 1, Senior: 2 } as const;
export type AgeCategory = (typeof AgeCategory)[keyof typeof AgeCategory];

export const SwapRequestStatus = { Pending: 0, Approved: 1, Rejected: 2, Cancelled: 3 } as const;
export type SwapRequestStatus = (typeof SwapRequestStatus)[keyof typeof SwapRequestStatus];
