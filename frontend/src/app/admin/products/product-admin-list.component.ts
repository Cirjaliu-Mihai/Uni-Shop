import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { AdminProductListItem } from '../models/admin.models';
import { AdminProductService } from '../services/admin-product.service';

@Component({
  selector: 'app-product-admin-list',
  templateUrl: './product-admin-list.component.html',
  styleUrl: './product-admin-list.component.scss',
  standalone: false,
})
export class ProductAdminListComponent implements OnInit {
  readonly search = new FormControl('', { nonNullable: true });
  readonly displayedColumns = [
    'product',
    'category',
    'supplier',
    'price',
    'stock',
    'status',
    'actions',
  ];
  products: AdminProductListItem[] = [];
  page = 1;
  pageSize = 20;
  totalCount = 0;
  loading = true;
  errorMessage = '';

  constructor(
    private readonly productService: AdminProductService,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
  ) {}

  ngOnInit(): void {
    this.search.valueChanges
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.page = 1;
        this.loadProducts();
      });
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading = true;
    this.errorMessage = '';
    this.productService
      .getProducts(this.search.value, this.page, this.pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.products = result.items;
          this.totalCount = result.totalCount;
          this.page = result.page;
          this.pageSize = result.pageSize;
          this.loading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: (error) => {
          this.loading = false;
          this.errorMessage = this.readError(error, 'The product list could not be loaded.');
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  changePage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadProducts();
  }

  createProduct(): void {
    void this.router.navigate(['products/new'], { relativeTo: this.route });
  }

  editProduct(product: AdminProductListItem): void {
    void this.router.navigate(['products', product.id, 'edit'], { relativeTo: this.route });
  }

  deleteProduct(product: AdminProductListItem): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Delete product?',
          message: `“${product.name}” will be removed from the catalog. This cannot be undone.`,
          confirmLabel: 'Delete product',
        },
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.productService
          .deleteProduct(product.id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.snackBar.open('Product deleted.', 'Dismiss', { duration: 3000 });
              this.loadProducts();
            },
            error: (error) => this.handleDeleteError(product, error),
          });
      });
  }

  saveStock(product: AdminProductListItem, value: string): void {
    const stock = Number(value);
    if (!Number.isInteger(stock) || stock < 0) {
      this.snackBar.open('Stock must be a non-negative whole number.', 'Dismiss', {
        duration: 4000,
      });
      return;
    }
    this.productService
      .updateStock(product.id, stock)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          product.stock = stock;
          this.snackBar.open('Stock updated.', 'Dismiss', { duration: 2500 });
          this.changeDetectorRef.markForCheck();
        },
        error: (error) =>
          this.snackBar.open(this.readError(error, 'Could not update stock.'), 'Dismiss', {
            duration: 5000,
          }),
      });
  }

  private handleDeleteError(product: AdminProductListItem, error: unknown): void {
    if (!(error instanceof HttpErrorResponse) || error.status !== 409) {
      this.snackBar.open(this.readError(error, 'Could not delete the product.'), 'Dismiss', {
        duration: 5000,
      });
      return;
    }
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Product is in order history',
          message: this.readError(error, 'This product cannot be deleted. Deactivate it instead?'),
          confirmLabel: 'Deactivate product',
        },
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.productService
          .deactivateProduct(product.id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.snackBar.open('Product deactivated.', 'Dismiss', { duration: 3000 });
              this.loadProducts();
            },
            error: (deactivateError) =>
              this.snackBar.open(
                this.readError(deactivateError, 'Could not deactivate the product.'),
                'Dismiss',
                { duration: 5000 },
              ),
          });
      });
  }

  private readError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.detail === 'string') {
      return error.error.detail;
    }
    return fallback;
  }
}
