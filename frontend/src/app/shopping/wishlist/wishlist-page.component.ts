import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CartService } from '../services/cart.service';
import { WishlistService } from '../services/wishlist.service';
import { WishlistEntry } from '../models/shopping.models';

@Component({
  selector: 'app-wishlist-page',
  templateUrl: './wishlist-page.component.html',
  styleUrl: './wishlist-page.component.scss',
  standalone: false,
})
export class WishlistPageComponent {
  readonly items$;
  readonly loading$;
  busyProductId: number | null = null;

  constructor(
    private readonly wishlist: WishlistService,
    private readonly cart: CartService,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
  ) {
    this.items$ = wishlist.items$;
    this.loading$ = wishlist.loading$;
  }

  remove(item: WishlistEntry): void {
    this.busyProductId = item.productId;
    this.wishlist.remove(item.productId).subscribe({
      next: () => {
        this.busyProductId = null;
        this.snackBar.open('Removed from wishlist.', 'Dismiss', { duration: 2200 });
        this.changeDetectorRef.markForCheck();
      },
      error: () => {
        this.busyProductId = null;
        this.snackBar.open('Could not remove this item.', 'Dismiss', { duration: 3500 });
        this.changeDetectorRef.markForCheck();
      },
    });
  }

  moveToCart(item: WishlistEntry): void {
    this.busyProductId = item.productId;
    this.wishlist.moveToCart(item.productId).subscribe({
      next: () =>
        this.cart.refresh().subscribe({
          next: () => {
            this.busyProductId = null;
            this.snackBar.open('Moved to cart.', 'View cart', { duration: 3000 });
            this.changeDetectorRef.markForCheck();
          },
          error: () => {
            this.busyProductId = null;
            this.snackBar.open('Moved to cart, but the cart could not refresh.', 'Dismiss', {
              duration: 4000,
            });
            this.changeDetectorRef.markForCheck();
          },
        }),
      error: (error) => {
        this.busyProductId = null;
        const detail =
          error instanceof HttpErrorResponse && typeof error.error?.detail === 'string'
            ? error.error.detail
            : 'Could not move this item to cart.';
        this.snackBar.open(detail, 'Dismiss', { duration: 4000 });
        this.changeDetectorRef.markForCheck();
      },
    });
  }
}
