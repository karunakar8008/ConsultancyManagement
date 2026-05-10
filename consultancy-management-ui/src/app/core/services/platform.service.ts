import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface OrganizationListItem {
  id: number;
  name: string;
  slug: string;
  isActive: boolean;
}

export interface CreateOrganizationRequest {
  name: string;
  slug: string;
}

export interface BootstrapOrgAdminRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

@Injectable({ providedIn: 'root' })
export class PlatformService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/platform`;

  listOrganizations() {
    return this.http.get<OrganizationListItem[]>(`${this.base}/organizations`);
  }

  createOrganization(body: CreateOrganizationRequest) {
    return this.http.post<{ id: number }>(`${this.base}/organizations`, body);
  }

  bootstrapOrganizationAdmin(organizationId: number, body: BootstrapOrgAdminRequest) {
    return this.http.post<{ message: string }>(
      `${this.base}/organizations/${organizationId}/bootstrap-admin`,
      body
    );
  }
}
