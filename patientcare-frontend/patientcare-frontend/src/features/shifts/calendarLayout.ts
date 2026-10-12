import type { CareShiftResponse } from '@/types/dto';
import { addDays, parseApiDate, startOfDay } from '@/utils/dates';

export interface Segment {
  shift: CareShiftResponse;
  startMin: number; // דקות מתחילת היום
  endMin: number;
  continuesFromPrev: boolean;
  continuesToNext: boolean;
  lane: number;
  lanes: number;
}

/** כל המשמרות שחופפות ליום נתון, חתוכות לגבולות היום (משמרת לילה מופיעה בשני ימים). */
export function segmentsForDay(shifts: CareShiftResponse[], day: Date): Segment[] {
  const dayStart = startOfDay(day).getTime();
  const dayEnd = addDays(startOfDay(day), 1).getTime();

  const items = shifts
    .map((shift) => ({ shift, s: parseApiDate(shift.startTime).getTime(), e: parseApiDate(shift.endTime).getTime() }))
    .filter(({ s, e }) => s < dayEnd && e > dayStart)
    .sort((a, b) => a.s - b.s || b.e - a.e)
    .map(({ shift, s, e }) => ({
      shift,
      startMin: (Math.max(s, dayStart) - dayStart) / 60000,
      endMin: (Math.min(e, dayEnd) - dayStart) / 60000,
      continuesFromPrev: s < dayStart,
      continuesToNext: e > dayEnd,
      lane: 0,
      lanes: 1,
    }));

  // חלוקה ל"אשכולות" חופפים ושיבוץ כל משמרת ל-lane פנוי, כדי שמשמרות מקבילות יוצגו זו לצד זו.
  let cluster: Segment[] = [];
  let clusterEnd = -1;
  const flush = () => {
    const laneEnds: number[] = [];
    for (const seg of cluster) {
      let lane = laneEnds.findIndex((end) => end <= seg.startMin);
      if (lane === -1) lane = laneEnds.length;
      laneEnds[lane] = seg.endMin;
      seg.lane = lane;
    }
    cluster.forEach((seg) => (seg.lanes = laneEnds.length));
    cluster = [];
  };
  for (const seg of items) {
    if (cluster.length && seg.startMin >= clusterEnd) flush();
    cluster.push(seg);
    clusterEnd = Math.max(clusterEnd, seg.endMin);
  }
  if (cluster.length) flush();
  return items;
}

/** טווח השעות להצגה בשבוע: מהמשמרת המוקדמת עד המאוחרת, עם מרווח, ולפחות 8 שעות. */
export function visibleHourRange(perDay: Segment[][]): [number, number] {
  const all = perDay.flat();
  if (!all.length) return [7, 19];
  let from = Math.floor(Math.min(...all.map((s) => s.startMin)) / 60) - 1;
  let to = Math.ceil(Math.max(...all.map((s) => s.endMin)) / 60) + 1;
  from = Math.max(0, from);
  to = Math.min(24, to);
  if (to - from < 8) to = Math.min(24, from + 8);
  if (to - from < 8) from = Math.max(0, to - 8);
  return [from, to];
}
