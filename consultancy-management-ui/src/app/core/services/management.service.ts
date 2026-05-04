import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface ManagementFileCatalogItem {
  kind: string;
  id: number;
  title: string;
  subtitle?: string | null;
  consultantName?: string | null;
  salesRecruiterName?: string | null;
  vendorName?: string | null;
  fileName: string;
  hasFile: boolean;
  at: string;
}

@Injectable({ providedIn: 'root' })
export class ManagementService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/management`;

  dashboard() {
    return this.http.get<Record<string, number>>(`${this.base}/dashboard`);
  }
  consultants() {
    return this.http.get<unknown[]>(`${this.base}/consultants`);
  }
  submissions() {
    return this.http.get<unknown[]>(`${this.base}/submissions`);
  }
  interviews() {
    return this.http.get<unknown[]>(`${this.base}/interviews`);
  }
  onboarding() {
    return this.http.get<unknown[]>(`${this.base}/onboarding`);
  }
  documents() {
    return this.http.get<unknown[]>(`${this.base}/documents`);
  }
  createTask(body: Record<string, unknown>) {
    return this.http.post(`${this.base}/onboarding/tasks`, body);
  }
  reviewDocument(id: number, body: { status: string }) {
    return this.http.put(`${this.base}/documents/${id}/review`, body);
  }

  uploadConsultantDocument(consultantId: number, formData: FormData) {
    return this.http.post<{ message: string; document: unknown }>(
      `${this.base}/consultants/${consultantId}/documents`,
      formData
    );
  }

  /** Consultant docs + vendor contact + submission + interview invite proofs (newest first). */
  fileCatalog() {
    return this.http.get<ManagementFileCatalogItem[]>(`${this.base}/file-catalog`);
  }

  downloadCatalogFile(kind: string, id: number, inline = false) {
    let params = new HttpParams().set('kind', kind).set('id', String(id));
    if (inline) params = params.set('inline', 'true');
    return this.http.get(`${this.base}/file-catalog/download`, { params, responseType: 'blob' });
  }

  /** Consultant document row (same API as consultant portal; Admin/Management authorized). */
  downloadConsultantPortalDocument(documentId: number, inline = false) {
    const cBase = `${environment.apiBaseUrl}/consultant`;
    let params = new HttpParams();
    if (inline) params = params.set('inline', 'true');
    return this.http.get(`${cBase}/documents/${documentId}/download`, { params, responseType: 'blob' });
  }
}
