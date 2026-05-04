export interface AdminUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  roles: string[];
  isActive: boolean;
  isDeleted?: boolean;
}

export interface AdminUserDetail extends AdminUser {
  createdAt: string;
  updatedAt?: string | null;
  deletedAt?: string | null;
}
