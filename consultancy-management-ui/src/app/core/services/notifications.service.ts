import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface AppNotification {
  id: number;
  title: string;
  message: string;
  kind: string;
  createdAt: string;
  isRead: boolean;
  relatedDocumentId: number | null;
  relatedOnboardingTaskId: number | null;
}

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/notifications`;

  list(take = 50): Observable<AppNotification[]> {
    const p = new HttpParams().set('take', String(take));
    return this.http.get<AppNotification[]>(this.base, { params: p });
  }

  unreadCount(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${this.base}/unread-count`);
  }

  markRead(id: number): Observable<void> {
    return this.http.post(`${this.base}/${id}/read`, {}).pipe(map(() => undefined));
  }

  markAllRead(): Observable<void> {
    return this.http.post(`${this.base}/read-all`, {}).pipe(map(() => undefined));
  }
}
