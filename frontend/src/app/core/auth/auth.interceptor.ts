import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const user = auth.current();
  const cloned = req.clone({
    setHeaders: {
      Authorization: `Bearer ${user.mockJwt}`,
      'X-Mock-Role': user.role,
      'X-Mock-UserId': user.userId,
      'X-Mock-UserName': user.userName
    }
  });
  return next(cloned);
};
