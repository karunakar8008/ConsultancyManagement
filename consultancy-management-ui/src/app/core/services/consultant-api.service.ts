import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface DailyActivitySuggestions {
  jobsAppliedCount: number;
  vendorReachedOutCount: number;
  submissionsCount: number;
  interviewCallsCount: number;
}

export interface ConsultantVendorReachOutRow {
  id: number;
  reachedDate: string;
  vendorName: string;
  notes?: string | null;
}

export interface ConsultantInterviewRow {
  id: number;
  interviewCode: string;
  submissionCode: string;
  jobTitle: string;
  interviewDate: string;
  interviewMode?: string | null;
  status: string;
  hasInviteProof?: boolean;
}

export interface ConsultantSubmissionRow {
  id: number;
  submissionCode: string;
  jobTitle: string;
  clientName?: string | null;
  vendorName: string;
  salesRecruiterName: string;
  submissionDate: string;
  status: string;
  hasProof?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ConsultantApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/consultant`;

  dashboard() {
    return this.http.get<Record<string, number>>(`${this.base}/dashboard`);
  }

  profile() {
    return this.http.get<unknown>(`${this.base}/profile`);
  }

  updateProfileContact(body: { email: string; phoneNumber: string }) {
    return this.http.put<{ message: string }>(`${this.base}/profile/contact`, body);
  }

  dailyActivities(consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.get<unknown[]>(`${this.base}/daily-activities${q}`);
  }

  dailyActivitySuggestions(activityDate: string, consultantId?: number) {
    let params = new HttpParams().set('activityDate', activityDate);
    if (consultantId != null) params = params.set('consultantId', String(consultantId));
    return this.http.get<DailyActivitySuggestions>(`${this.base}/daily-activity-suggestions`, { params });
  }

  saveDaily(body: Record<string, unknown>, consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.post(`${this.base}/daily-activities${q}`, body);
  }

  updateDaily(id: number, body: Record<string, unknown>) {
    return this.http.put(`${this.base}/daily-activities/${id}`, body);
  }

  jobApplications(consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.get<unknown[]>(`${this.base}/job-applications${q}`);
  }

  saveJob(body: Record<string, unknown>, consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.post(`${this.base}/job-applications${q}`, body);
  }

  submissions(consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.get<ConsultantSubmissionRow[]>(`${this.base}/submissions${q}`);
  }

  interviews(consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.get<ConsultantInterviewRow[]>(`${this.base}/interviews${q}`);
  }

  vendorReachOuts(consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.get<ConsultantVendorReachOutRow[]>(`${this.base}/vendor-reach-outs${q}`);
  }

  createVendorReachOut(
    body: { reachedDate: string; vendorName: string; notes?: string | null },
    consultantId?: number
  ) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.post(`${this.base}/vendor-reach-outs${q}`, body);
  }

  documents(consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.get<unknown[]>(`${this.base}/documents${q}`);
  }

  uploadDocument(formData: FormData, consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.post<{ message: string; document: unknown }>(`${this.base}/documents${q}`, formData);
  }

  downloadDocument(id: number, inline = false) {
    let params = new HttpParams();
    if (inline) params = params.set('inline', 'true');
    return this.http.get(`${this.base}/documents/${id}/download`, { params, responseType: 'blob' });
  }

  /** kind: submission | interview — own records only. */
  downloadProof(kind: string, id: number, inline = false) {
    let params = new HttpParams().set('kind', kind).set('id', String(id));
    if (inline) params = params.set('inline', 'true');
    return this.http.get(`${this.base}/proofs/download`, { params, responseType: 'blob' });
  }
}
