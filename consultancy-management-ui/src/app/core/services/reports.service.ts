import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/reports`;

  dailySummary(date: string, consultantId?: number | null, salesRecruiterId?: number | null) {
    let p = new HttpParams().set('date', date);
    if (consultantId != null && consultantId > 0) p = p.set('consultantId', String(consultantId));
    if (salesRecruiterId != null && salesRecruiterId > 0) p = p.set('salesRecruiterId', String(salesRecruiterId));
    return this.http.get(`${this.base}/daily-summary`, { params: p });
  }
  dailySummaryCsv(date: string, consultantId?: number | null, salesRecruiterId?: number | null): Observable<Blob> {
    let p = new HttpParams().set('date', date);
    if (consultantId != null && consultantId > 0) p = p.set('consultantId', String(consultantId));
    if (salesRecruiterId != null && salesRecruiterId > 0) p = p.set('salesRecruiterId', String(salesRecruiterId));
    return this.http.get(`${this.base}/daily-summary/csv`, { params: p, responseType: 'blob' });
  }
  weeklySummary(start: string, end: string, consultantId?: number | null, salesRecruiterId?: number | null) {
    let p = new HttpParams().set('startDate', start).set('endDate', end);
    if (consultantId != null && consultantId > 0) p = p.set('consultantId', String(consultantId));
    if (salesRecruiterId != null && salesRecruiterId > 0) p = p.set('salesRecruiterId', String(salesRecruiterId));
    return this.http.get(`${this.base}/weekly-summary`, { params: p });
  }
  weeklySummaryCsv(start: string, end: string, consultantId?: number | null, salesRecruiterId?: number | null): Observable<Blob> {
    let p = new HttpParams().set('startDate', start).set('endDate', end);
    if (consultantId != null && consultantId > 0) p = p.set('consultantId', String(consultantId));
    if (salesRecruiterId != null && salesRecruiterId > 0) p = p.set('salesRecruiterId', String(salesRecruiterId));
    return this.http.get(`${this.base}/weekly-summary/csv`, { params: p, responseType: 'blob' });
  }
  consultantPerformance(consultantId?: number | null) {
    let p = new HttpParams();
    if (consultantId != null && consultantId > 0) p = p.set('consultantId', String(consultantId));
    return this.http.get(`${this.base}/consultant-performance`, { params: p });
  }
  consultantPerformanceCsv(consultantId?: number | null): Observable<Blob> {
    let p = new HttpParams();
    if (consultantId != null && consultantId > 0) p = p.set('consultantId', String(consultantId));
    return this.http.get(`${this.base}/consultant-performance/csv`, { params: p, responseType: 'blob' });
  }
  salesPerformance(salesRecruiterId?: number | null) {
    let p = new HttpParams();
    if (salesRecruiterId != null && salesRecruiterId > 0) p = p.set('salesRecruiterId', String(salesRecruiterId));
    return this.http.get(`${this.base}/sales-performance`, { params: p });
  }
  salesPerformanceCsv(salesRecruiterId?: number | null): Observable<Blob> {
    let p = new HttpParams();
    if (salesRecruiterId != null && salesRecruiterId > 0) p = p.set('salesRecruiterId', String(salesRecruiterId));
    return this.http.get(`${this.base}/sales-performance/csv`, { params: p, responseType: 'blob' });
  }
  submissions() {
    return this.http.get(`${this.base}/submissions`);
  }
  submissionsCsv(): Observable<Blob> {
    return this.http.get(`${this.base}/submissions/csv`, { responseType: 'blob' });
  }
  interviews() {
    return this.http.get(`${this.base}/interviews`);
  }
  interviewsCsv(): Observable<Blob> {
    return this.http.get(`${this.base}/interviews/csv`, { responseType: 'blob' });
  }
  onboardingStatus() {
    return this.http.get(`${this.base}/onboarding-status`);
  }
  onboardingStatusCsv(): Observable<Blob> {
    return this.http.get(`${this.base}/onboarding-status/csv`, { responseType: 'blob' });
  }
}
