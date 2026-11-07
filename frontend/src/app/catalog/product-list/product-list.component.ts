import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { PageEvent } from '@angular/material/paginator';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import {
  CategoryListItem,
  PagedResult,
  ProductListItem,
  ProductQuery,
  ProductSort,
} from '../models/catalog.models';
import { CategoryService } from '../services/category.service';
import { ProductService } from '../services/product.service';

@Component({
  selector: 'app-product-list',
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss',
  standalone: false,
})
export class ProductListComponent implements OnInit {
  readonly filtersForm;
  readonly sortControl;
  categories: CategoryListItem[] = [];
  products: ProductListItem[] = [];
  page = 1;
  pageSize = 12;
  totalCount = 0;
  loading = true;
  errorMessage = '';
  categoryError = false;

  private readonly destroyRef: DestroyRef;

  constructor(
    formBuilder: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly productService: ProductService,
    private readonly categoryService: CategoryService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    destroyRef: DestroyRef,
  ) {
    this.destroyRef = destroyRef;
    this.filtersForm = formBuilder.group({
      search: formBuilder.nonNullable.control(''),
      categoryId: formBuilder.control<number | null>(null),
      minPrice: formBuilder.control<number | null>(null),
      maxPrice: formBuilder.control<number | null>(null),
      inStock: formBuilder.control<boolean | null>(null),
    });
    this.sortControl = formBuilder.nonNullable.control<ProductSort>('newest');
  }

  ngOnInit(): void {
    this.categoryService
      .getCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (categories) => {
          this.categories = categories;
          this.categoryError = false;
          this.changeDetectorRef.markForCheck();
        },
        error: () => {
          this.categoryError = true;
          this.changeDetectorRef.markForCheck();
        },
      });

    this.watchFilterChanges();
    this.route.queryParamMap
      .pipe(
        map((params) => this.parseQuery(params)),
        tap((query) => this.applyQuery(query)),
        switchMap((query) => {
          this.loading = true;
          this.errorMessage = '';
          return this.productService.getProducts(query).pipe(
            catchError(() => {
              this.loading = false;
              this.errorMessage =
                'The catalog could not be loaded. Check your connection and try again.';
              this.changeDetectorRef.markForCheck();
              return of<PagedResult<ProductListItem>>({
                items: [],
                page: query.page,
                pageSize: query.pageSize,
                totalCount: 0,
                totalPages: 0,
              });
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.products = result.items;
        this.page = result.page;
        this.pageSize = result.pageSize;
        this.totalCount = result.totalCount;
        this.loading = false;
        this.changeDetectorRef.markForCheck();
      });
  }

  selectCategory(categoryId: number | null): void {
    this.filtersForm.controls.categoryId.setValue(categoryId);
  }

  setInStock(checked: boolean): void {
    this.filtersForm.controls.inStock.setValue(checked ? true : null);
  }

  clearFilters(): void {
    this.filtersForm.reset({
      search: '',
      categoryId: null,
      minPrice: null,
      maxPrice: null,
      inStock: null,
    });
    this.sortControl.setValue('newest');
    this.updateUrl(1);
  }

  changePage(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.updateUrl(event.pageIndex + 1);
  }

  reload(): void {
    this.updateUrl(this.page);
  }

  private watchFilterChanges(): void {
    this.filtersForm.controls.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateUrl(1));

    this.filtersForm.controls.categoryId.valueChanges
      .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateUrl(1));

    this.filtersForm.controls.minPrice.valueChanges
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateUrl(1));

    this.filtersForm.controls.maxPrice.valueChanges
      .pipe(debounceTime(250), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateUrl(1));

    this.filtersForm.controls.inStock.valueChanges
      .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateUrl(1));

    this.sortControl.valueChanges
      .pipe(distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.updateUrl(1));
  }

  private parseQuery(params: import('@angular/router').ParamMap): ProductQuery {
    const sort = params.get('sort');
    const inStockParam = params.get('inStock');
    return {
      search: params.get('search') ?? '',
      categoryId: this.parseNumber(params.get('categoryId')),
      minPrice: this.parseNumber(params.get('minPrice')),
      maxPrice: this.parseNumber(params.get('maxPrice')),
      inStock: inStockParam === 'true' ? true : inStockParam === 'false' ? false : null,
      sort: sort === 'price_asc' || sort === 'price_desc' || sort === 'name' ? sort : 'newest',
      page: Math.max(1, this.parseNumber(params.get('page')) ?? 1),
      pageSize: Math.min(50, Math.max(1, this.parseNumber(params.get('pageSize')) ?? 12)),
    };
  }

  private applyQuery(query: ProductQuery): void {
    this.page = query.page;
    this.pageSize = query.pageSize;
    this.filtersForm.patchValue(
      {
        search: query.search,
        categoryId: query.categoryId,
        minPrice: query.minPrice,
        maxPrice: query.maxPrice,
        inStock: query.inStock,
      },
      { emitEvent: false },
    );
    this.sortControl.setValue(query.sort, { emitEvent: false });
  }

  private updateUrl(page: number): void {
    const values = this.filtersForm.getRawValue();
    const queryParams: Params = {
      search: values.search.trim() || null,
      categoryId: values.categoryId,
      minPrice: values.minPrice,
      maxPrice: values.maxPrice,
      inStock: values.inStock,
      sort: this.sortControl.value === 'newest' ? null : this.sortControl.value,
      page: page === 1 ? null : page,
      pageSize: this.pageSize === 12 ? null : this.pageSize,
    };
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      replaceUrl: true,
    });
  }

  private parseNumber(value: string | null): number | null {
    if (value === null || value.trim() === '') return null;
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }
}
