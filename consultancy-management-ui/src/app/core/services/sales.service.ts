import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface AssignedConsultantOption {
  id: number;
  firstName: string;
  lastName: string;
  technology?: string | null;
  visaStatus?: string | null;
  currentLocation?: string | null;
  status: string;
}

export interface VendorRow {
  id: number;
  vendorCode: string;
  vendorName: string;
  contactPerson?: string | null;
  email: string;
  phoneNumber: string;
  companyName?: string | null;
  linkedInUrl?: string | null;
  notes?: string | null;
  hasContactProof?: boolean;
}

export interface SalesSubmissionRow {
  id: number;
  submissionCode: string;
  consultantId: number;
  consultantName: string;
  vendorId: number;
  vendorName: string;
  jobTitle: string;
  clientName?: string | null;
  submissionDate: string;
  status: string;
  rate?: number | null;
  notes?: string | null;
  hasProof?: boolean;
}

export interface SubmissionOption {
  id: number;
  submissionCode: string;
  jobTitle: string;
  consultantName: string;
}

export interface InterviewRow {
  id: number;
  interviewCode: string;
  submissionId: number;
  submissionCode: string;
  consultantName: string;
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

export interface VendorFormValues {
  vendorName: string;
  contactPerson: string;
  email: string;
  phoneNumber: string;
  companyName: string;
  linkedInUrl: string;
  notes: string;
}

export interface SubmissionFormValues {
  consultantId: number;
  vendorId: number;
  jobTitle: string;
  clientName: string;
  submissionDate: string;
  status: string;
  rate: number | null;
  notes: string;
}

export interface InterviewFormValues {
  submissionId: number;
  interviewDate: string;
  interviewEndDate: string;
  interviewMode: string;
  round: string;
  status: string;
  feedback: string;
  notes: string;
}

@Injectable({ providedIn: 'root' })
export class SalesService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/sales`;

  dashboard() {
    return this.http.get<Record<string, number>>(`${this.base}/dashboard`);
  }

  assignedConsultants() {
    return this.http.get<AssignedConsultantOption[]>(`${this.base}/assigned-consultants`);
  }

  vendors() {
    return this.http.get<VendorRow[]>(`${this.base}/vendors`);
  }

  createVendor(values: VendorFormValues, contactProof: File | null) {
    return this.http.post<{ message: string; id: number }>(`${this.base}/vendors`, this.toVendorFormData(values, contactProof));
  }

  updateVendor(id: number, values: VendorFormValues, contactProof: File | null) {
    return this.http.put<{ message: string }>(`${this.base}/vendors/${id}`, this.toVendorFormData(values, contactProof));
  }

  submissions() {
    return this.http.get<SalesSubmissionRow[]>(`${this.base}/submissions`);
  }

  submissionOptions() {
    return this.http.get<SubmissionOption[]>(`${this.base}/submission-options`);
  }

  createSubmission(values: SubmissionFormValues, proofFile: File) {
    return this.http.post<{ message: string; id: number }>(
      `${this.base}/submissions`,
      this.toSubmissionFormData(values, proofFile)
    );
  }

  updateSubmission(id: number, values: SubmissionFormValues, proofFile: File | null) {
    return this.http.put<{ message: string }>(
      `${this.base}/submissions/${id}`,
      this.toSubmissionFormData(values, proofFile)
    );
  }

  interviews() {
    return this.http.get<InterviewRow[]>(`${this.base}/interviews`);
  }

  createInterview(values: InterviewFormValues, inviteProof: File) {
    return this.http.post<{ message: string; id: number }>(
      `${this.base}/interviews`,
      this.toInterviewFormData(values, inviteProof)
    );
  }

  updateInterview(id: number, values: InterviewFormValues, inviteProof: File | null) {
    return this.http.put<{ message: string }>(`${this.base}/interviews/${id}`, this.toInterviewFormData(values, inviteProof));
  }

  /** kind: vendor | submission | interview */
  downloadProof(kind: string, id: number, inline = false) {
    let params = new HttpParams().set('kind', kind).set('id', String(id));
    if (inline) params = params.set('inline', 'true');
    return this.http.get(`${this.base}/proofs/download`, { params, responseType: 'blob' });
  }

  private toVendorFormData(v: VendorFormValues, contactProof: File | null): FormData {
    const fd = new FormData();
    fd.append('vendorName', v.vendorName);
    fd.append('contactPerson', v.contactPerson ?? '');
    fd.append('email', v.email);
    fd.append('phoneNumber', v.phoneNumber);
    fd.append('companyName', v.companyName ?? '');
    fd.append('linkedInUrl', v.linkedInUrl ?? '');
    fd.append('notes', v.notes ?? '');
    if (contactProof) fd.append('contactProof', contactProof, contactProof.name);
    return fd;
  }

  private toSubmissionFormData(v: SubmissionFormValues, proofFile: File | null): FormData {
    const fd = new FormData();
    fd.append('consultantId', String(v.consultantId));
    fd.append('vendorId', String(v.vendorId));
    fd.append('jobTitle', v.jobTitle);
    fd.append('clientName', v.clientName ?? '');
    fd.append('submissionDate', new Date(v.submissionDate).toISOString());
    fd.append('status', v.status);
    if (v.rate != null && !Number.isNaN(v.rate)) fd.append('rate', String(v.rate));
    fd.append('notes', v.notes ?? '');
    if (proofFile) fd.append('proofFile', proofFile, proofFile.name);
    return fd;
  }

  private toInterviewFormData(v: InterviewFormValues, inviteProof: File | null): FormData {
    const fd = new FormData();
    fd.append('submissionId', String(v.submissionId));
    fd.append('interviewDate', new Date(v.interviewDate).toISOString());
    if (v.interviewEndDate?.trim()) {
      fd.append('interviewEndDate', new Date(v.interviewEndDate).toISOString());
    }
    fd.append('interviewMode', v.interviewMode ?? '');
    fd.append('round', v.round ?? '');
    fd.append('status', v.status);
    fd.append('feedback', v.feedback ?? '');
    fd.append('notes', v.notes ?? '');
    if (inviteProof) fd.append('inviteProofFile', inviteProof, inviteProof.name);
    return fd;
  }
}
