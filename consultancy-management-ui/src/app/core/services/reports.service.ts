import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/reports`;

  dailySummary(date: string) {
    const p = new HttpParams().set('date', date);
    return this.http.get(`${this.base}/daily-summary`, { params: p });
  }
  dailySummaryCsv(date: string): Observable<Blob> {
    const p = new HttpParams().set('date', date);
    return this.http.get(`${this.base}/daily-summary/csv`, { params: p, responseType: 'blob' });
  }
  weeklySummary(start: string, end: string) {
    let p = new HttpParams().set('startDate', start).set('endDate', end);
    return this.http.get(`${this.base}/weekly-summary`, { params: p });
  }
  weeklySummaryCsv(start: string, end: string): Observable<Blob> {
    const p = new HttpParams().set('startDate', start).set('endDate', end);
    return this.http.get(`${this.base}/weekly-summary/csv`, { params: p, responseType: 'blob' });
  }
  consultantPerformance() {
    return this.http.get(`${this.base}/consultant-performance`);
  }
  consultantPerformanceCsv(): Observable<Blob> {
    return this.http.get(`${this.base}/consultant-performance/csv`, { responseType: 'blob' });
  }
  salesPerformance() {
    return this.http.get(`${this.base}/sales-performance`);
  }
  salesPerformanceCsv(): Observable<Blob> {
    return this.http.get(`${this.base}/sales-performance/csv`, { responseType: 'blob' });
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
