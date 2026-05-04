import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../../environments/environment';
import { CurrentUser, LoginRequest, LoginResponse } from '../models/auth.models';

const TOKEN_KEY = 'cms_jwt';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  login(email: string, password: string) {
    const body: LoginRequest = { email, password };
    return this.http.post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, body);
  }

  forgotPassword(email: string) {
    const resetUrlBase = `${window.location.origin}/reset-password`;
    return this.http.post<{ message: string }>(`${environment.apiBaseUrl}/auth/forgot-password`, {
      email,
      resetUrlBase
    });
  }

  resetPassword(email: string, token: string, newPassword: string) {
    return this.http.post<{ message: string }>(`${environment.apiBaseUrl}/auth/reset-password`, {
      email,
      token,
      newPassword
    });
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.router.navigate(['/login']);
  }

  saveToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    const t = this.getToken();
    if (!t) return false;
    try {
      const decoded: { exp?: number } = jwtDecode(t);
      if (!decoded.exp) return true;
      return decoded.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  private parsePayload(): Record<string, unknown> | null {
    const t = this.getToken();
    if (!t) return null;
    const parts = t.split('.');
    if (parts.length < 2) return null;
    try {
      const b64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const json = decodeURIComponent(
        atob(b64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(json) as Record<string, unknown>;
    } catch {
      return null;
    }
  }

  getRoles(): string[] {
    const p = this.parsePayload();
    if (!p) return [];
    const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
    const raw = p[roleClaim] ?? p['role'];
    if (!raw) return [];
    if (Array.isArray(raw)) return raw.map(String);
    return [String(raw)];
  }

  hasRole(role: string): boolean {
    return this.getRoles().some((x) => x.toLowerCase() === role.toLowerCase());
  }

  getCurrentUser(): CurrentUser | null {
    const p = this.parsePayload();
    if (!p) return null;
    return {
      userId: String(p['sub'] ?? p['nameid'] ?? ''),
      employeeId: String(p['employee_id'] ?? ''),
      email: String(p['email'] ?? ''),
      fullName: String(p['name'] ?? p['unique_name'] ?? ''),
      roles: this.getRoles()
    };
  }

  redirectByRole(): void {
    if (this.hasRole('Admin')) {
      this.router.navigate(['/admin/dashboard']);
      return;
    }
    if (this.hasRole('Management')) {
      this.router.navigate(['/management/dashboard']);
      return;
    }
    if (this.hasRole('SalesRecruiter')) {
      this.router.navigate(['/sales/dashboard']);
      return;
    }
    if (this.hasRole('Consultant')) {
      this.router.navigate(['/consultant/dashboard']);
      return;
    }
    this.router.navigate(['/login']);
  }
}
