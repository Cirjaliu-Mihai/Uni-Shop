import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        const isAuthFlowRequest = ['/login', '/register'].some((path) =>
          request.url.endsWith(path),
        );
        if (error.status === 401 && !isAuthFlowRequest) {
          const returnUrl = this.router.url;
          this.auth.logout();
          void this.router.navigate(['/account/login'], { queryParams: { returnUrl } });
        }
        return throwError(() => error);
      }),
    );
  }
}
