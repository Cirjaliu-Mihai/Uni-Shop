import { Component, Input } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { ProductListItem } from '../../../catalog/models/catalog.models';
import { CartService } from '../../../shopping/services/cart.service';

@Component({
  selector: 'app-product-card',
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.scss',
  standalone: false,
})
export class ProductCardComponent {
  @Input({ required: true }) product!: ProductListItem;

  constructor(
    private readonly cart: CartService,
    private readonly snackBar: MatSnackBar,
    private readonly router: Router,
  ) {}

  addToCart(): void {
    this.cart.addProduct(this.product.id).subscribe({
      next: () =>
        this.snackBar
          .open('Added to cart.', 'View cart', { duration: 3200 })
          .onAction()
          .subscribe(() => {
            void this.router.navigate(['/cart']);
          }),
      error: (error) =>
        this.snackBar.open(
          error?.error?.detail ?? error?.message ?? 'Could not add this product.',
          'Dismiss',
          { duration: 4000 },
        ),
    });
  }
}
