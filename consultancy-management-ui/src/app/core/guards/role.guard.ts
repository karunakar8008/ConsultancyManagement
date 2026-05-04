import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const allowed = (route.data['roles'] as string[] | undefined) ?? [];
  if (allowed.length === 0) return true;
  if (allowed.some((r) => auth.hasRole(r))) return true;
  router.navigate(['/login']);
  return false;
};
