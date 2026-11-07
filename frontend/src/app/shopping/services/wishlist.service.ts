import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, catchError, of, switchMap, tap } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { WishlistEntry } from '../models/shopping.models';

const apiUrl = 'http://localhost:5000/api/wishlist';

@Injectable({ providedIn: 'root' })
export class WishlistService {
  private readonly itemsSubject = new BehaviorSubject<WishlistEntry[]>([]);
  private readonly wishedIdsSubject = new BehaviorSubject<ReadonlySet<number>>(new Set());
  private readonly loadingSubject = new BehaviorSubject(true);
  readonly items$ = this.itemsSubject.asObservable();
  readonly wishedIds$ = this.wishedIdsSubject.asObservable();
  readonly loading$ = this.loadingSubject.asObservable();

  constructor(
    private readonly http: HttpClient,
    auth: AuthService,
  ) {
    auth.currentUser$
      .pipe(
        switchMap((user) => {
          if (!user) {
            this.loadingSubject.next(false);
            return of([]);
          }
          this.loadingSubject.next(true);
          return this.http.get<WishlistEntry[]>(apiUrl).pipe(
            tap(() => this.loadingSubject.next(false)),
            catchError(() => {
              this.loadingSubject.next(false);
              return of([]);
            }),
          );
        }),
      )
      .subscribe((items) => this.setItems(items));
  }

  add(productId: number): Observable<WishlistEntry> {
    return this.http.put<WishlistEntry>(`${apiUrl}/${productId}`, {}).pipe(
      switchMap((item) => {
        this.setItems([
          ...this.itemsSubject.value.filter((existing) => existing.productId !== productId),
          item,
        ]);
        return of(item);
      }),
    );
  }

  remove(productId: number): Observable<void> {
    return this.http.delete<void>(`${apiUrl}/${productId}`).pipe(
      switchMap(() => {
        this.setItems(this.itemsSubject.value.filter((item) => item.productId !== productId));
        return of(void 0);
      }),
    );
  }

  moveToCart(productId: number): Observable<unknown> {
    return this.http.post(`${apiUrl}/${productId}/move-to-cart`, {}).pipe(
      switchMap(() => {
        this.setItems(this.itemsSubject.value.filter((item) => item.productId !== productId));
        return of(void 0);
      }),
    );
  }

  has(productId: number): boolean {
    return this.wishedIdsSubject.value.has(productId);
  }

  private setItems(items: WishlistEntry[]): void {
    this.itemsSubject.next(items);
    this.wishedIdsSubject.next(new Set(items.map((item) => item.productId)));
  }
}
