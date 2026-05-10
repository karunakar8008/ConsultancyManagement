export interface LoginRequest {
  organizationSlug: string;
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresIn: number;
  userId: string;
  employeeId: string;
  email: string;
  fullName: string;
  roles: string[];
  organizationId: number;
  organizationSlug: string;
  organizationName: string;
}

export interface CurrentUser {
  userId: string;
  employeeId: string;
  email: string;
  fullName: string;
  roles: string[];
  organizationId: string;
  organizationSlug: string;
}
