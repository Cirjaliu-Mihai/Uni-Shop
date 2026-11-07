import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Observable, forkJoin, switchMap } from 'rxjs';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { AdminCategory, AdminDeliveryMethod, AdminSupplier } from '../models/admin.models';
import { AdminReferenceDataService } from '../services/admin-reference-data.service';
import {
  ReferenceDataDialogComponent,
  ReferenceDataEntity,
  ReferenceDataKind,
  ReferenceDataWriteRequest,
} from './reference-data-dialog.component';

@Component({
  selector: 'app-reference-data-admin',
  templateUrl: './reference-data-admin.component.html',
  styleUrl: './reference-data-admin.component.scss',
  standalone: false,
})
export class ReferenceDataAdminComponent implements OnInit {
  readonly categoryColumns = ['name', 'description', 'productCount', 'actions'];
  readonly supplierColumns = ['name', 'contactEmail', 'phone', 'productCount', 'actions'];
  readonly deliveryColumns = ['name', 'price', 'estimatedDays', 'productCount', 'actions'];
  categories: AdminCategory[] = [];
  suppliers: AdminSupplier[] = [];
  deliveryMethods: AdminDeliveryMethod[] = [];
  loading = true;
  errorMessage = '';

  constructor(
    private readonly referenceService: AdminReferenceDataService,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
  ) {}

  ngOnInit(): void {
    this.loadReferenceData();
  }

  loadReferenceData(): void {
    this.loading = true;
    this.errorMessage = '';
    forkJoin({
      categories: this.referenceService.getCategories(),
      suppliers: this.referenceService.getSuppliers(),
      deliveryMethods: this.referenceService.getDeliveryMethods(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.categories = result.categories;
          this.suppliers = result.suppliers;
          this.deliveryMethods = result.deliveryMethods;
          this.loading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: (error) => {
          this.loading = false;
          this.errorMessage = this.readError(error, 'Reference data could not be loaded.');
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  add(kind: ReferenceDataKind): void {
    this.openEditor(kind);
  }

  edit(kind: ReferenceDataKind, entity: ReferenceDataEntity): void {
    this.openEditor(kind, entity);
  }

  delete(kind: ReferenceDataKind, entity: ReferenceDataEntity): void {
    const name = entity.name;
    const noun = kind === 'delivery-method' ? 'delivery method' : kind;
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: `Delete ${noun}?`,
          message: `Delete “${name}”? Items that still use it will prevent deletion.`,
          confirmLabel: `Delete ${noun}`,
        },
      })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.deleteRequest(kind, entity.id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.snackBar.open(`${this.label(kind)} deleted.`, 'Dismiss', { duration: 3000 });
              this.loadReferenceData();
            },
            error: (error) =>
              this.snackBar.open(
                this.readError(error, `Could not delete the ${noun}.`),
                'Dismiss',
                { duration: 5000 },
              ),
          });
      });
  }

  label(kind: ReferenceDataKind): string {
    return kind === 'delivery-method' ? 'Delivery method' : kind[0].toUpperCase() + kind.slice(1);
  }

  private openEditor(kind: ReferenceDataKind, entity?: ReferenceDataEntity): void {
    this.dialog
      .open(ReferenceDataDialogComponent, {
        data: { kind, entity },
        maxWidth: 'calc(100vw - 32px)',
      })
      .afterClosed()
      .pipe(
        switchMap((request: ReferenceDataWriteRequest | undefined): Observable<unknown> => {
          if (!request) return EMPTY;
          return this.saveRequest(kind, request, entity?.id);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.snackBar.open(`${this.label(kind)} ${entity ? 'updated' : 'created'}.`, 'Dismiss', {
            duration: 3000,
          });
          this.loadReferenceData();
        },
        error: (error) =>
          this.snackBar.open(this.readError(error, `Could not save the ${kind}.`), 'Dismiss', {
            duration: 5000,
          }),
      });
  }

  private saveRequest(
    kind: ReferenceDataKind,
    request: ReferenceDataWriteRequest,
    id?: number,
  ): Observable<unknown> {
    if (kind === 'category') {
      return this.referenceService.saveCategory(
        request as Parameters<AdminReferenceDataService['saveCategory']>[0],
        id,
      );
    }
    if (kind === 'supplier') {
      return this.referenceService.saveSupplier(
        request as Parameters<AdminReferenceDataService['saveSupplier']>[0],
        id,
      );
    }
    return this.referenceService.saveDeliveryMethod(
      request as Parameters<AdminReferenceDataService['saveDeliveryMethod']>[0],
      id,
    );
  }

  private deleteRequest(kind: ReferenceDataKind, id: number): Observable<void> {
    if (kind === 'category') return this.referenceService.deleteCategory(id);
    if (kind === 'supplier') return this.referenceService.deleteSupplier(id);
    return this.referenceService.deleteDeliveryMethod(id);
  }

  private readError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.detail === 'string') {
      return error.error.detail;
    }
    return fallback;
  }
}
