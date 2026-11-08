import { Component, Inject } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Address, AddressWriteRequest } from '../models/account.models';

export interface AddressDialogData {
  address?: Address;
}

@Component({
  selector: 'app-address-dialog',
  templateUrl: './address-dialog.component.html',
  styleUrl: './address-dialog.component.scss',
  standalone: false,
})
export class AddressDialogComponent {
  readonly form: UntypedFormGroup;

  constructor(
    formBuilder: UntypedFormBuilder,
    @Inject(MAT_DIALOG_DATA) readonly data: AddressDialogData,
    private readonly dialogRef: MatDialogRef<AddressDialogComponent, AddressWriteRequest>,
  ) {
    const address = data.address;
    this.form = formBuilder.group({
      label: [address?.label ?? '', [Validators.required, Validators.maxLength(80)]],
      fullName: [address?.fullName ?? '', [Validators.required, Validators.maxLength(200)]],
      phone: [
        address?.phone ?? '',
        [Validators.required, Validators.pattern(/^[+0-9()\-\s]{7,40}$/)],
      ],
      street: [address?.street ?? '', [Validators.required, Validators.maxLength(300)]],
      city: [address?.city ?? '', [Validators.required, Validators.maxLength(120)]],
      county: [address?.county ?? '', [Validators.required, Validators.maxLength(120)]],
      postalCode: [address?.postalCode ?? '', [Validators.required, Validators.pattern(/^\d{6}$/)]],
      country: [address?.country ?? 'Romania', [Validators.required, Validators.maxLength(100)]],
      isDefault: [address?.isDefault ?? false],
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.dialogRef.close(this.form.getRawValue() as AddressWriteRequest);
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
