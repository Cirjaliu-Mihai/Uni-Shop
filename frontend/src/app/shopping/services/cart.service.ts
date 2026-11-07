import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { BehaviorSubject, Observable, map, of, switchMap, throwError } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { ProductService } from '../../catalog/services/product.service';
import { CartSnapshot, GuestCartItem, emptyCart } from '../models/shopping.models';

const apiUrl = 'http://localhost:5000/api/cart';
const guestCartStorageKey = 'uni-shop.cart';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly inBrowser: boolean;
  private readonly cartSubject = new BehaviorSubject<CartSnapshot>(emptyCart());
  readonly cart$ = this.cartSubject.asObservable();
  readonly itemCount$ = this.cart$.pipe(
    map((cart) => cart.items.reduce((total, item) => total + item.quantity, 0)),
  );

  constructor(
    private readonly http: HttpClient,
    private readonly products: ProductService,
    private readonly auth: AuthService,
    @Inject(PLATFORM_ID) platformId: object,
  ) {
    this.inBrowser = isPlatformBrowser(platformId);
    auth.currentUser$
      .pipe(switchMap((user) => (user ? this.mergeGuestCart() : this.loadGuestCart())))
      .subscribe({
        next: (snapshot) => this.cartSubject.next(snapshot),
        error: () => this.cartSubject.next(emptyCart()),
      });
  }

  get itemCount(): number {
    return this.cartSubject.value.items.reduce((total, item) => total + item.quantity, 0);
  }

  get isGuest(): boolean {
    return !this.auth.currentUser;
  }

  getCurrentCart(): CartSnapshot {
    return this.cartSubject.value;
  }

  addProduct(productId: number, quantity = 1): Observable<CartSnapshot> {
    const currentItem = this.cartSubject.value.items.find((item) => item.productId === productId);
    const nextQuantity = (currentItem?.quantity ?? 0) + quantity;
    if (this.isBrowserGuest()) {
      return this.products.getProduct(productId).pipe(
        switchMap((product) => {
          if (nextQuantity > product.stock)
            return throwError(() => new Error(`Only ${product.stock} units are available.`));
          this.writeGuestItem(productId, nextQuantity);
          return this.loadGuestCart();
        }),
      );
    }
    return this.http
      .put<CartSnapshot>(`${apiUrl}/items/${productId}`, { quantity: nextQuantity })
      .pipe(
        switchMap((snapshot) => {
          this.cartSubject.next(snapshot);
          return of(snapshot);
        }),
      );
  }

  updateQuantity(productId: number, quantity: number): Observable<CartSnapshot> {
    if (this.isBrowserGuest()) {
      const item = this.cartSubject.value.items.find(
        (cartItem) => cartItem.productId === productId,
      );
      if (!item) return of(this.cartSubject.value);
      if (quantity < 1) return throwError(() => new Error('Quantity must be at least one.'));
      if (quantity > item.stock)
        return throwError(() => new Error(`Only ${item.stock} units are available.`));
      this.writeGuestItem(productId, quantity);
      return this.loadGuestCart();
    }
    return this.http.put<CartSnapshot>(`${apiUrl}/items/${productId}`, { quantity }).pipe(
      switchMap((snapshot) => {
        this.cartSubject.next(snapshot);
        return of(snapshot);
      }),
    );
  }

  removeItem(productId: number): Observable<CartSnapshot> {
    if (this.isBrowserGuest()) {
      this.writeGuestCart(this.readGuestCart().filter((item) => item.productId !== productId));
      return this.loadGuestCart();
    }
    return this.http
      .delete<void>(`${apiUrl}/items/${productId}`)
      .pipe(switchMap(() => this.reloadServerCart()));
  }

  clear(): Observable<CartSnapshot> {
    if (this.isBrowserGuest()) {
      this.writeGuestCart([]);
      this.cartSubject.next(emptyCart());
      return of(this.cartSubject.value);
    }
    return this.http.delete<void>(apiUrl).pipe(switchMap(() => this.reloadServerCart()));
  }

  refresh(): Observable<CartSnapshot> {
    return this.isBrowserGuest() ? this.loadGuestCart() : this.reloadServerCart();
  }

  mergeGuestCart(): Observable<CartSnapshot> {
    if (!this.inBrowser) {
      return this.reloadServerCart();
    }

    const items = this.readGuestCart();
    if (items.length === 0) {
      return this.reloadServerCart();
    }

    return this.http.post<CartSnapshot>(`${apiUrl}/merge`, { items }).pipe(
      switchMap((snapshot) => {
        this.writeGuestCart([]);
        this.cartSubject.next(snapshot);
        return of(snapshot);
      }),
    );
  }

  private reloadServerCart(): Observable<CartSnapshot> {
    return this.http.get<CartSnapshot>(apiUrl).pipe(
      switchMap((snapshot) => {
        this.cartSubject.next(snapshot);
        return of(snapshot);
      }),
    );
  }

  private loadGuestCart(): Observable<CartSnapshot> {
    if (!this.isBrowserGuest()) return this.reloadServerCart();
    const items = this.readGuestCart();
    if (items.length === 0) {
      this.cartSubject.next(emptyCart());
      return of(this.cartSubject.value);
    }

    return this.products.getProductsByIds(items.map((item) => item.productId)).pipe(
      switchMap((products) => {
        const productsById = new Map(products.map((product) => [product.id, product]));
        const cartItems = items.flatMap((item) => {
          const product = productsById.get(item.productId);
          if (!product) return [];
          return [
            {
              productId: product.id,
              name: product.name,
              imageUrl: product.imageUrl,
              price: product.price,
              stock: product.stock,
              quantity: item.quantity,
              lineTotal: product.price * item.quantity,
              exceedsStock: item.quantity > product.stock,
              categoryName: product.categoryName,
            },
          ];
        });
        const snapshot: CartSnapshot = {
          items: cartItems,
          total: cartItems.reduce((sum, item) => sum + item.lineTotal, 0),
          hasStockWarnings: cartItems.some((item) => item.exceedsStock),
        };
        this.cartSubject.next(snapshot);
        return of(snapshot);
      }),
    );
  }

  private isBrowserGuest(): boolean {
    return this.inBrowser && !this.getStoredToken();
  }

  private getStoredToken(): string | null {
    return this.inBrowser ? localStorage.getItem('unishop.token') : null;
  }

  private readGuestCart(): GuestCartItem[] {
    if (!this.inBrowser) return [];
    try {
      const saved = localStorage.getItem(guestCartStorageKey);
      if (!saved) return [];
      const parsed = JSON.parse(saved) as GuestCartItem[];
      return Array.isArray(parsed)
        ? parsed.filter(
            (item) =>
              Number.isInteger(item.productId) &&
              item.productId > 0 &&
              Number.isInteger(item.quantity) &&
              item.quantity > 0,
          )
        : [];
    } catch {
      return [];
    }
  }

  private writeGuestItem(productId: number, quantity: number): void {
    const items = this.readGuestCart().filter((item) => item.productId !== productId);
    items.push({ productId, quantity });
    this.writeGuestCart(items);
  }

  private writeGuestCart(items: GuestCartItem[]): void {
    if (this.inBrowser) localStorage.setItem(guestCartStorageKey, JSON.stringify(items));
  }
}
