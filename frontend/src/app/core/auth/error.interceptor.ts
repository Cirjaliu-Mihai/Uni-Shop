import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
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
        if (error.status !== 401 || isAuthFlowRequest) {
          const message =
            error.status === 0
              ? 'The UniShop API could not be reached. Check that the services are running.'
              : typeof error.error?.detail === 'string'
                ? error.error.detail
                : typeof error.error?.title === 'string'
                  ? error.error.title
                  : `The request failed (${error.status}).`;
          this.snackBar.open(message, 'Dismiss', {
            duration: 5000,
            panelClass: ['api-error-snackbar'],
          });
        }
        return throwError(() => error);
      }),
    );
  }
}
