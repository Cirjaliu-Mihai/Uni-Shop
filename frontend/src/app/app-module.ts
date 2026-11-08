import {
  inject,
  NgModule,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { BrowserModule, provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { MatSnackBarModule } from '@angular/material/snack-bar';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { ErrorInterceptor } from './core/auth/error.interceptor';
import { AuthService } from './core/auth/auth.service';
import { JwtInterceptor } from './core/auth/jwt.interceptor';
import { CoreModule } from './core/core.module';

@NgModule({
  declarations: [App],
  imports: [BrowserModule, HttpClientModule, MatSnackBarModule, AppRoutingModule, CoreModule],
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideClientHydration(withEventReplay()),
    provideAppInitializer(() => inject(AuthService).initializeSession()),
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
  ],
  bootstrap: [App],
})
export class AppModule {}
