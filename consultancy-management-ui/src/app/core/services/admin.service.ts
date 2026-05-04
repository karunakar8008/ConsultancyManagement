import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AdminUser, AdminUserDetail } from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/admin`;

  dashboard() {
    return this.http.get<Record<string, number>>(`${this.base}/dashboard`);
  }
  users() {
    return this.http.get<AdminUser[]>(`${this.base}/users`);
  }
  nextEmployeeId(role: string) {
    return this.http.get<{ employeeId: string }>(`${this.base}/users/next-employee-id`, {
      params: { role }
    });
  }
  userById(employeeId: string) {
    return this.http.get<AdminUserDetail>(`${this.base}/users/${encodeURIComponent(employeeId)}`);
  }
  roles() {
    return this.http.get<{ name: string }[]>(`${this.base}/roles`);
  }
  consultants() {
    return this.http.get<unknown[]>(`${this.base}/consultants`);
  }
  salesRecruiters() {
    return this.http.get<unknown[]>(`${this.base}/sales-recruiters`);
  }
  managementUsers() {
    return this.http.get<unknown[]>(`${this.base}/management-users`);
  }
  assignments() {
    return this.http.get<unknown[]>(`${this.base}/assignments`);
  }
  createUser(body: Record<string, unknown>) {
    return this.http.post(`${this.base}/users`, body);
  }
  deleteUser(employeeId: string) {
    return this.http.delete(`${this.base}/users/${encodeURIComponent(employeeId)}`);
  }
  updateUser(employeeId: string, body: Record<string, unknown>) {
    return this.http.put(`${this.base}/users/${encodeURIComponent(employeeId)}`, body);
  }
  createConsultant(body: Record<string, unknown>) {
    return this.http.post(`${this.base}/consultants`, body);
  }
  updateConsultant(employeeId: string, body: Record<string, unknown>) {
    return this.http.put(`${this.base}/consultants/${encodeURIComponent(employeeId)}`, body);
  }
  createSales(body: Record<string, unknown>) {
    return this.http.post(`${this.base}/sales-recruiters`, body);
  }
  updateSales(employeeId: string, body: Record<string, unknown>) {
    return this.http.put(`${this.base}/sales-recruiters/${encodeURIComponent(employeeId)}`, body);
  }
  createAssignment(body: Record<string, unknown>) {
    return this.http.post(`${this.base}/assignments`, body);
  }
  updateAssignment(id: number, body: Record<string, unknown>) {
    return this.http.put(`${this.base}/assignments/${id}`, body);
  }
  createManagementUser(body: Record<string, unknown>) {
    return this.http.post(`${this.base}/management-users`, body);
  }
  updateManagement(employeeId: string, body: Record<string, unknown>) {
    return this.http.put(`${this.base}/management-users/${encodeURIComponent(employeeId)}`, body);
  }
  salesManagementAssignments() {
    return this.http.get<unknown[]>(`${this.base}/sales-management-assignments`);
  }
  createSalesManagementAssignment(body: Record<string, unknown>) {
    return this.http.post(`${this.base}/sales-management-assignments`, body);
  }
  updateSalesManagementAssignment(id: number, body: Record<string, unknown>) {
    return this.http.put(`${this.base}/sales-management-assignments/${id}`, body);
  }
}
