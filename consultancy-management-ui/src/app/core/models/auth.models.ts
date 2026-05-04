export interface LoginRequest {
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
}

export interface CurrentUser {
  userId: string;
  employeeId: string;
  email: string;
  fullName: string;
  roles: string[];
}
