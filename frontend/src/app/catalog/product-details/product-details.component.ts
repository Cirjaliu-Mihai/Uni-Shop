import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import { ProductDetails } from '../models/catalog.models';
import { ProductService } from '../services/product.service';

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
}
