import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface DirectoryUser {
  employeeId: string;
  firstName: string;
  lastName: string;
  email: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class DirectoryService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/directory`;

  visibleUsers() {
    return this.http.get<DirectoryUser[]>(`${this.base}/users`);
  }
}
