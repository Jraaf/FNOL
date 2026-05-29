import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snack = inject(MatSnackBar);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const message = extractMessage(err);
      snack.open(message, 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
      return throwError(() => err);
    })
  );
};

function extractMessage(err: HttpErrorResponse): string {
  if (err.error && typeof err.error === 'object') {
    if ('detail' in err.error && err.error.detail) return String(err.error.detail);
    if ('title' in err.error && err.error.title) return String(err.error.title);
    if ('extras' in err.error && err.error.extras?.errors) {
      const first = Object.values(err.error.extras.errors)[0] as string[];
      if (first?.length) return first[0];
    }
  }
  if (err.status === 0) return 'Cannot reach API. Is the backend running?';
  return err.statusText || `Request failed (${err.status})`;
}
