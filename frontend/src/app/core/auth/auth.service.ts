import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { BehaviorSubject, Observable, firstValueFrom, tap, timeout } from 'rxjs';
import { AuthResponse, AuthUser, LoginRequest, RegisterRequest } from './auth.models';

const apiUrl = 'http://localhost:5000/api/auth';
const tokenStorageKey = 'unishop.token';
const userStorageKey = 'unishop.user';
const expiresAtStorageKey = 'unishop.expiresAt';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly inBrowser: boolean;
  private readonly currentUserSubject = new BehaviorSubject<AuthUser | null>(null);
  readonly currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private readonly http: HttpClient,
    @Inject(PLATFORM_ID) platformId: object,
  ) {
    this.inBrowser = isPlatformBrowser(platformId);
    this.restoreSession();
  }

  get currentUser(): AuthUser | null {
    return this.currentUserSubject.value;
  }

  get isAuthenticated(): boolean {
    return Boolean(this.getToken() && this.currentUser);
  }

  get isAdmin(): boolean {
    return this.currentUser?.roles.includes('Admin') ?? false;
  }

  getToken(): string | null {
    return this.inBrowser ? localStorage.getItem(tokenStorageKey) : null;
  }

  async initializeSession(): Promise<void> {
    if (!this.inBrowser || !this.getToken()) {
      return;
    }

    try {
      const user = await firstValueFrom(
        this.http.get<AuthUser>(`${apiUrl}/me`).pipe(timeout({ first: 5000 })),
      );
      if (!this.isAuthUser(user)) {
        throw new Error('Invalid authenticated user response.');
      }
      localStorage.setItem(userStorageKey, JSON.stringify(user));
      this.currentUserSubject.next(user);
    } catch {
      this.logout();
    }
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${apiUrl}/login`, request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${apiUrl}/register`, request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  logout(): void {
    if (this.inBrowser) {
      localStorage.removeItem(tokenStorageKey);
      localStorage.removeItem(userStorageKey);
      localStorage.removeItem(expiresAtStorageKey);
    }
    this.currentUserSubject.next(null);
  }

  private storeSession(response: AuthResponse): void {
    if (!this.inBrowser) {
      return;
    }
    localStorage.setItem(tokenStorageKey, response.token);
    localStorage.setItem(userStorageKey, JSON.stringify(response.user));
    localStorage.setItem(expiresAtStorageKey, response.expiresAt);
    this.currentUserSubject.next(response.user);
  }

  private restoreSession(): void {
    if (!this.inBrowser) {
      return;
    }

    const token = localStorage.getItem(tokenStorageKey);
    const expiresAt = localStorage.getItem(expiresAtStorageKey);
    if (!token || !expiresAt || Date.parse(expiresAt) <= Date.now()) {
      this.logout();
    }
  }

  private isAuthUser(value: unknown): value is AuthUser {
    if (typeof value !== 'object' || value === null) {
      return false;
    }
    const user = value as Partial<AuthUser>;
    return (
      typeof user.id === 'string' &&
      typeof user.email === 'string' &&
      typeof user.fullName === 'string' &&
      Array.isArray(user.roles) &&
      user.roles.every((role) => typeof role === 'string')
    );
  }
}
