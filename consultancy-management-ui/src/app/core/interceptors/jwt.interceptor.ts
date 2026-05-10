import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

/** Do not attach JWT to anonymous auth calls — a stale/invalid token breaks JwtBearer before [AllowAnonymous] runs. */
function isAnonymousAuthPath(url: string): boolean {
  const path = url.split('?')[0].toLowerCase();
  return (
    path.endsWith('/auth/login') ||
    path.endsWith('/auth/forgot-password') ||
    path.endsWith('/auth/reset-password')
  );
}

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.getToken();
  const skipAuthHeader = isAnonymousAuthPath(req.url);
  const authReq =
    token && !skipAuthHeader ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((err: unknown) => {
      // Wrong password on login also returns 401 — do not treat that like an expired session.
      const urlFor401 = err instanceof HttpErrorResponse ? err.url ?? req.url : req.url;
      if (err instanceof HttpErrorResponse && err.status === 401 && !isAnonymousAuthPath(urlFor401)) {
        auth.logout();
        router.navigate(['/login']);
      }
      return throwError(() => err);
    })
  );
};
