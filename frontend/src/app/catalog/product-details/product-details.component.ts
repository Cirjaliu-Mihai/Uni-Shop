import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import { ProductDetails } from '../models/catalog.models';
import { ProductService } from '../services/product.service';
import { CartService } from '../../shopping/services/cart.service';

@Component({
  selector: 'app-product-details',
  templateUrl: './product-details.component.html',
  styleUrl: './product-details.component.scss',
  standalone: false,
})
export class ProductDetailsComponent implements OnInit {
  product: ProductDetails | null = null;
  loading = true;
  errorMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly productService: ProductService,
    private readonly cart: CartService,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
  ) {}

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        map((params) => Number(params.get('id'))),
        distinctUntilChanged(),
        switchMap((id) => {
          this.loading = true;
          this.errorMessage = '';
          if (!Number.isInteger(id) || id < 1) {
            this.errorMessage = 'This product could not be found.';
            this.loading = false;
            this.changeDetectorRef.markForCheck();
            return of(null);
          }
          return this.productService.getProduct(id).pipe(
            catchError(() => {
              this.errorMessage = 'This product could not be found or loaded.';
              this.changeDetectorRef.markForCheck();
              return of(null);
            }),
            tap(() => (this.loading = false)),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((product) => {
        this.product = product;
        this.loading = false;
        this.changeDetectorRef.markForCheck();
      });
  }

  addToCart(product: ProductDetails): void {
    if (product.stock < 1) return;
    this.cart.addProduct(product.id).subscribe({
      next: () => this.snackBar.open('Added to cart.', 'Dismiss', { duration: 2500 }),
      error: (error) =>
        this.snackBar.open(
          error?.error?.detail ?? error?.message ?? 'Could not add this product.',
          'Dismiss',
          { duration: 4000 },
        ),
    });
  }
}
