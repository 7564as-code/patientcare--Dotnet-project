import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Building2, ChevronLeft, ChevronRight, KeyRound, Plus, Search, Users } from 'lucide-react';
import { patientsApi } from '@/api';
import { useAsync } from '@/hooks/useAsync';
import { useToast } from '@/components/ui/Toast';
import { PageHeader } from '@/components/ui/PageHeader';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { EmptyState, ErrorState, Spinner } from '@/components/ui/States';
import { JoinPatientModal } from '@/features/patients/JoinPatientModal';
import { PatientFormModal } from '@/features/patients/PatientForm';
import { ageCategoryLabel, genderLabel } from '@/utils/labels';
import { AgeCategory } from '@/types/enums';
import s from './PatientsPage.module.css';

const ageTone = { [AgeCategory.Child]: 'indigo', [AgeCategory.Adult]: 'brand', [AgeCategory.Senior]: 'amber' } as const;

const PAGE_SIZE = 12;

export function PatientsPage() {
  const [q, setQ] = useState('');
  const [search, setSearch] = useState('');
  const [dept, setDept] = useState('');
  const [page, setPage] = useState(1);
  const [open, setOpen] = useState(false);
  const [joinOpen, setJoinOpen] = useState(false);
  const toast = useToast();
  const navigate = useNavigate();

  // חיפוש נשלח לשרת רק אחרי שהמשתמש מפסיק להקליד.
  useEffect(() => {
    const t = setTimeout(() => {
      setSearch(q.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(t);
  }, [q]);

  // העימוד, החיפוש והסינון מתבצעים בשרת (בבסיס הנתונים), לא בדפדפן.
  const result = useAsync(
    () => patientsApi.page({ page, pageSize: PAGE_SIZE, search: search || undefined, department: dept || undefined }),
    [page, search, dept],
  );
  const departmentsState = useAsync(patientsApi.departments, []);
  const { loading, error, reload } = result;
  const data = result.data;
  const items = data?.items ?? [];
  const departments = departmentsState.data ?? [];
  const hasFilter = !!search || !!dept;

  return (
    <div className="page">
      <PageHeader
        title="החולים שלי"
        subtitle={data ? `${data.totalCount} חולים ${hasFilter ? 'תואמים' : 'במעגלים שלך'}` : undefined}
        actions={
          <>
            <Button variant="secondary" icon={<KeyRound size={18} />} onClick={() => setJoinOpen(true)}>הצטרפות עם קוד</Button>
            <Button icon={<Plus size={18} />} onClick={() => setOpen(true)}>חולה חדש</Button>
          </>
        }
      />

      <div className={s.filters}>
        <div className={s.search}>
          <Search size={18} aria-hidden />
          <input className="input" placeholder="חיפוש לפי שם או מקום" value={q} onChange={(e) => setQ(e.target.value)} aria-label="חיפוש חולים" />
        </div>
        <select className={`input ${s.dept}`} value={dept} onChange={(e) => { setDept(e.target.value); setPage(1); }} aria-label="סינון לפי מקום">
          <option value="">כל המקומות</option>
          {departments.map((d) => <option key={d} value={d}>{d}</option>)}
        </select>
      </div>

      {loading && !data && <Spinner />}
      {error && <ErrorState message={error} onRetry={reload} />}
      {!loading && !error && items.length === 0 && (
        <EmptyState icon={<Users size={22} />} title={hasFilter ? 'לא נמצאו חולים תואמים' : 'עדיין אין חולים'} hint={hasFilter ? 'נסה לשנות את החיפוש או את הסינון.' : 'צרו חולה חדש, או הצטרפו למעגל של חולה קיים עם קוד הזמנה.'} />
      )}

      <div className={s.grid}>
        {items.map((p) => (
          <Link key={p.id} to={`/patients/${p.id}`} className={s.card}>
            <div className={s.top}>
              <span className={s.code}>{p.medicalCode}</span>
              <Badge tone={ageTone[p.ageCategory]}>{ageCategoryLabel(p.ageCategory)}</Badge>
            </div>
            <div className={s.meta}>
              <span><Building2 size={15} aria-hidden /> {p.department}</span>
              <span>{p.isManager ? 'אתה המנהל' : 'בן משפחה במעגל'}</span>
              <span>{genderLabel(p.patientGender)} · גיל <span className="num">{p.age}</span></span>
            </div>
          </Link>
        ))}
      </div>

      {data && data.totalPages > 1 && (
        <nav className={s.pager} aria-label="עימוד">
          <Button variant="secondary" icon={<ChevronRight size={18} />} disabled={page <= 1 || loading} onClick={() => setPage((p) => p - 1)}>הקודם</Button>
          <span>עמוד <span className="num">{data.page}</span> מתוך <span className="num">{data.totalPages}</span></span>
          <Button variant="secondary" icon={<ChevronLeft size={18} />} disabled={page >= data.totalPages || loading} onClick={() => setPage((p) => p + 1)}>הבא</Button>
        </nav>
      )}

      <JoinPatientModal
        open={joinOpen}
        onClose={() => setJoinOpen(false)}
        onJoined={(p) => {
          setJoinOpen(false);
          toast.success('הצטרפת למעגל של החולה');
          navigate(`/patients/${p.id}`);
        }}
      />

      <PatientFormModal
        open={open}
        onClose={() => setOpen(false)}
        onCreated={(p) => {
          setOpen(false);
          toast.success('החולה נוסף בהצלחה');
          navigate(`/patients/${p.id}`);
        }}
      />
    </div>
  );
}
