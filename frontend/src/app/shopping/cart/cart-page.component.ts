import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { CartService } from '../services/cart.service';
import { CartLine } from '../models/shopping.models';

@Component({
  selector: 'app-cart-page',
  templateUrl: './cart-page.component.html',
  styleUrl: './cart-page.component.scss',
  standalone: false,
})
export class CartPageComponent {
  readonly cart$;
  loadingProductId: number | null = null;

  constructor(
    readonly cart: CartService,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
  ) {
    this.cart$ = cart.cart$;
  }

  changeQuantity(item: CartLine, quantity: number): void {
    if (quantity < 1 || quantity === item.quantity) return;
    this.loadingProductId = item.productId;
    this.cart.updateQuantity(item.productId, quantity).subscribe({
      next: () => {
        this.loadingProductId = null;
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.loadingProductId = null;
        this.snackBar.open(this.errorMessage(error), 'Dismiss', { duration: 4000 });
        this.changeDetectorRef.markForCheck();
      },
    });
  }

  remove(item: CartLine): void {
    this.cart.removeItem(item.productId).subscribe({
      next: () => this.snackBar.open('Item removed from cart.', 'Dismiss', { duration: 2200 }),
      error: () => this.snackBar.open('Could not remove this item.', 'Dismiss', { duration: 3500 }),
    });
  }

  clear(): void {
    this.cart.clear().subscribe({
      next: () => this.snackBar.open('Cart cleared.', 'Dismiss', { duration: 2200 }),
      error: () => this.snackBar.open('Could not clear the cart.', 'Dismiss', { duration: 3500 }),
    });
  }

  checkout(): void {
    if (this.cart.isGuest) {
      void this.router.navigate(['/account/login'], {
        queryParams: { returnUrl: this.router.url },
      });
      return;
    }
    this.snackBar.open('Checkout will be available in the next step.', 'Dismiss', {
      duration: 3500,
    });
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.detail === 'string') {
      return error.error.detail;
    }
    return error instanceof Error ? error.message : 'Could not update quantity.';
  }
}
