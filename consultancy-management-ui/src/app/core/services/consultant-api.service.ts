import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface DailyActivitySuggestions {
  jobsAppliedCount: number;
  vendorReachedOutCount: number;
  vendorResponseCount: number;
  submissionsCount: number;
  interviewCallsCount: number;
}

export interface ConsultantVendorReachOutRow {
  id: number;
  reachedDate: string;
  vendorName: string;
  contactPerson?: string | null;
  contactEmail?: string | null;
  vendorResponseNotes?: string | null;
  notes?: string | null;
}

export interface ConsultantInterviewRow {
  id: number;
  interviewCode: string;
  submissionCode: string;
  jobTitle: string;
  interviewDate: string;
  interviewEndDate?: string | null;
  interviewMode?: string | null;
  round?: string | null;
  status: string;
  feedback?: string | null;
  notes?: string | null;
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
  notes?: string | null;
  consultantCommunication?: string | null;
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

  patchDailyNotes(id: number, body: { notes: string | null }) {
    return this.http.patch<{ message: string }>(`${this.base}/daily-activities/${id}/notes`, body);
  }

  jobApplications(consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.get<unknown[]>(`${this.base}/job-applications${q}`);
  }

  saveJob(body: Record<string, unknown>, consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.post(`${this.base}/job-applications${q}`, body);
  }

  updateJob(id: number, body: Record<string, unknown>, consultantId?: number) {
    let params = new HttpParams();
    if (consultantId != null) params = params.set('consultantId', String(consultantId));
    return this.http.put<{ message: string }>(`${this.base}/job-applications/${id}`, body, { params });
  }

  submissions(consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.get<ConsultantSubmissionRow[]>(`${this.base}/submissions${q}`);
  }

  interviews(consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.get<ConsultantInterviewRow[]>(`${this.base}/interviews${q}`);
  }

  updateInterview(
    id: number,
    body: {
      interviewDate: string;
      interviewEndDate: string | null;
      interviewMode: string | null;
      round: string | null;
      status: string;
      feedback: string | null;
      notes: string | null;
    }
  ) {
    return this.http.put<{ message: string }>(`${this.base}/interviews/${id}`, body);
  }

  updateSubmissionCommunication(id: number, body: { consultantCommunication: string | null }) {
    return this.http.put<{ message: string }>(`${this.base}/submissions/${id}/consultant-communication`, body);
  }

  vendorReachOuts(consultantId?: number) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.get<ConsultantVendorReachOutRow[]>(`${this.base}/vendor-reach-outs${q}`);
  }

  createVendorReachOut(
    body: {
      reachedDate: string;
      vendorName: string;
      contactPerson?: string | null;
      contactEmail?: string | null;
      vendorResponseNotes?: string | null;
      notes?: string | null;
    },
    consultantId?: number
  ) {
    const q = consultantId != null ? `?consultantId=${consultantId}` : '';
    return this.http.post(`${this.base}/vendor-reach-outs${q}`, body);
  }

  updateVendorReachOut(
    id: number,
    body: {
      reachedDate: string;
      vendorName: string;
      contactPerson?: string | null;
      contactEmail?: string | null;
      vendorResponseNotes?: string | null;
      notes?: string | null;
    },
    consultantId?: number
  ) {
    let params = new HttpParams();
    if (consultantId != null) params = params.set('consultantId', String(consultantId));
    return this.http.put<{ message: string }>(`${this.base}/vendor-reach-outs/${id}`, body, { params });
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
