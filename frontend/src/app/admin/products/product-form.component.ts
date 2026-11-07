import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { UntypedFormArray, UntypedFormBuilder, UntypedFormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import {
  AdminCategory,
  AdminDeliveryMethod,
  AdminProduct,
  AdminSupplier,
  ProductWriteRequest,
} from '../models/admin.models';
import { AdminProductService } from '../services/admin-product.service';
import { AdminReferenceDataService } from '../services/admin-reference-data.service';

@Component({
  selector: 'app-product-form',
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss',
  standalone: false,
})
export class ProductFormComponent implements OnInit {
  readonly form: UntypedFormGroup;
  private readonly formBuilder: UntypedFormBuilder;
  categories: AdminCategory[] = [];
  suppliers: AdminSupplier[] = [];
  deliveryMethods: AdminDeliveryMethod[] = [];
  productId: number | null = null;
  loading = true;
  saving = false;
  errorMessage = '';

  get specifications(): UntypedFormArray {
    return this.form.get('specifications') as UntypedFormArray;
  }

  constructor(
    formBuilder: UntypedFormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly productService: AdminProductService,
    private readonly referenceDataService: AdminReferenceDataService,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
  ) {
    this.formBuilder = formBuilder;
    this.form = formBuilder.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.required, Validators.maxLength(4000)]],
      price: [null, [Validators.required, Validators.min(0.01)]],
      stock: [0, [Validators.required, Validators.min(0), Validators.max(2147483647)]],
      imageUrl: ['', [Validators.maxLength(2048), Validators.pattern(/^$|^https?:\/\/.+/i)]],
      isActive: [true],
      categoryId: [null, Validators.required],
      supplierId: [null, Validators.required],
      deliveryMethodIds: [[], [Validators.required, Validators.minLength(1)]],
      specifications: formBuilder.array([]),
    });
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.productId = Number.isInteger(id) && id > 0 ? id : null;

    forkJoin({
      categories: this.referenceDataService.getCategories(),
      suppliers: this.referenceDataService.getSuppliers(),
      deliveryMethods: this.referenceDataService.getDeliveryMethods(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (references) => {
          this.categories = references.categories;
          this.suppliers = references.suppliers;
          this.deliveryMethods = references.deliveryMethods;
          if (this.productId !== null) {
            this.loadProduct(this.productId);
          } else {
            this.loading = false;
            this.changeDetectorRef.markForCheck();
          }
        },
        error: (error) => {
          this.loading = false;
          this.errorMessage = this.readError(error, 'Could not load product reference data.');
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  addSpecification(specification = { key: '', value: '' }): void {
    this.specifications.push(this.formBuilderGroup(specification.key, specification.value));
  }

  removeSpecification(index: number): void {
    this.specifications.removeAt(index);
  }

  cancel(): void {
    void this.router.navigate(['/admin']);
  }

  submit(): void {
    if (
      this.form.invalid ||
      this.saving ||
      this.categories.length === 0 ||
      this.suppliers.length === 0
    ) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    const request: ProductWriteRequest = {
      name: String(values.name).trim(),
      description: String(values.description).trim(),
      specifications: (values.specifications as { key: string; value: string }[]).map(
        (specification) => ({ key: specification.key.trim(), value: specification.value.trim() }),
      ),
      price: Number(values.price),
      stock: Number(values.stock),
      imageUrl: String(values.imageUrl ?? '').trim() || null,
      isActive: Boolean(values.isActive),
      categoryId: Number(values.categoryId),
      supplierId: Number(values.supplierId),
      deliveryMethodIds: (values.deliveryMethodIds as number[]).map(Number),
    };

    this.saving = true;
    this.errorMessage = '';
    const saveRequest =
      this.productId === null
        ? this.productService.createProduct(request)
        : this.productService.updateProduct(this.productId, request);

    saveRequest.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (product) => {
        this.saving = false;
        this.snackBar.open(
          this.productId === null ? 'Product created.' : 'Product updated.',
          'Dismiss',
          { duration: 3000 },
        );
        void this.router.navigate(['/admin']);
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.saving = false;
        this.errorMessage = this.readError(
          error,
          'Could not save the product. Review the fields and try again.',
        );
        this.changeDetectorRef.markForCheck();
      },
    });
  }

  private loadProduct(id: number): void {
    this.productService
      .getProduct(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (product) => {
          this.populateForm(product);
          this.loading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: (error) => {
          this.loading = false;
          this.errorMessage = this.readError(error, 'This product could not be loaded.');
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  private populateForm(product: AdminProduct): void {
    this.form.patchValue({
      name: product.name,
      description: product.description,
      price: product.price,
      stock: product.stock,
      imageUrl: product.imageUrl ?? '',
      isActive: product.isActive,
      categoryId: product.categoryId,
      supplierId: product.supplierId,
      deliveryMethodIds: product.deliveryMethodIds,
    });
    this.specifications.clear();
    for (const specification of product.specifications) {
      this.addSpecification(specification);
    }
  }

  private formBuilderGroup(key: string, value: string): UntypedFormGroup {
    return this.formBuilder.group({
      key: [key, [Validators.required, Validators.maxLength(100)]],
      value: [value, [Validators.required, Validators.maxLength(500)]],
    });
  }

  private readError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.detail === 'string') {
      return error.error.detail;
    }
    if (
      error instanceof HttpErrorResponse &&
      error.error?.errors &&
      typeof error.error.errors === 'object'
    ) {
      const firstError = Object.values(error.error.errors as Record<string, string[]>).flat()[0];
      if (firstError) return firstError;
    }
    return fallback;
  }
}
