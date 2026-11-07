import { Component, Inject } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import {
  AdminCategory,
  AdminDeliveryMethod,
  AdminSupplier,
  CategoryWriteRequest,
  DeliveryMethodWriteRequest,
  SupplierWriteRequest,
} from '../models/admin.models';

export type ReferenceDataKind = 'category' | 'supplier' | 'delivery-method';
export type ReferenceDataEntity = AdminCategory | AdminSupplier | AdminDeliveryMethod;
export type ReferenceDataWriteRequest =
  CategoryWriteRequest | SupplierWriteRequest | DeliveryMethodWriteRequest;

export interface ReferenceDataDialogData {
  kind: ReferenceDataKind;
  entity?: ReferenceDataEntity;
}

@Component({
  selector: 'app-reference-data-dialog',
  templateUrl: './reference-data-dialog.component.html',
  styleUrl: './reference-data-dialog.component.scss',
  standalone: false,
})
export class ReferenceDataDialogComponent {
  readonly form: UntypedFormGroup;

  constructor(
    formBuilder: UntypedFormBuilder,
    @Inject(MAT_DIALOG_DATA) readonly data: ReferenceDataDialogData,
    private readonly dialogRef: MatDialogRef<
      ReferenceDataDialogComponent,
      ReferenceDataWriteRequest
    >,
  ) {
    const entity = data.entity as Record<string, unknown> | undefined;
    this.form = formBuilder.group({
      name: [
        entity?.['name'] ?? '',
        [Validators.required, Validators.maxLength(this.maxNameLength)],
      ],
      description: [entity?.['description'] ?? '', Validators.maxLength(500)],
      contactEmail: [entity?.['contactEmail'] ?? ''],
      phone: [entity?.['phone'] ?? ''],
      price: [entity?.['price'] ?? 0],
      estimatedDays: [entity?.['estimatedDays'] ?? 1],
    });

    if (data.kind === 'supplier') {
      this.form
        .get('contactEmail')
        ?.setValidators([Validators.required, Validators.email, Validators.maxLength(254)]);
      this.form.get('phone')?.setValidators([Validators.required, Validators.maxLength(40)]);
      this.form.get('contactEmail')?.updateValueAndValidity();
      this.form.get('phone')?.updateValueAndValidity();
    } else if (data.kind === 'delivery-method') {
      this.form.get('price')?.setValidators([Validators.required, Validators.min(0)]);
      this.form
        .get('estimatedDays')
        ?.setValidators([Validators.required, Validators.min(1), Validators.max(365)]);
      this.form.get('price')?.updateValueAndValidity();
      this.form.get('estimatedDays')?.updateValueAndValidity();
    }
    this.form.updateValueAndValidity();
  }

  get title(): string {
    const label = this.data.kind === 'delivery-method' ? 'delivery method' : this.data.kind;
    return `${this.data.entity ? 'Edit' : 'Add'} ${label}`;
  }

  get maxNameLength(): number {
    return this.data.kind === 'category' || this.data.kind === 'delivery-method' ? 120 : 160;
  }

  cancel(): void {
    this.dialogRef.close();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    if (this.data.kind === 'category') {
      this.dialogRef.close({
        name: String(value.name).trim(),
        description: String(value.description).trim() || null,
      });
    } else if (this.data.kind === 'supplier') {
      this.dialogRef.close({
        name: String(value.name).trim(),
        contactEmail: String(value.contactEmail).trim(),
        phone: String(value.phone).trim(),
      });
    } else {
      this.dialogRef.close({
        name: String(value.name).trim(),
        price: Number(value.price),
        estimatedDays: Number(value.estimatedDays),
      });
    }
  }
}
