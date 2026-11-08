import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Address, AddressWriteRequest } from '../models/account.models';
import { AddressService } from '../services/address.service';
import { AddressDialogComponent } from './address-dialog.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-address-list',
  templateUrl: './address-list.component.html',
  styleUrl: './address-list.component.scss',
  standalone: false,
})
export class AddressListComponent implements OnInit {
  addresses: Address[] = [];
  loading = true;
  errorMessage = '';

  constructor(
    private readonly addressService: AddressService,
    private readonly dialog: MatDialog,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadAddresses();
  }

  loadAddresses(): void {
    this.loading = true;
    this.addressService.getAddresses().subscribe({
      next: (addresses) => {
        this.addresses = addresses;
        this.loading = false;
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = this.readError(error, 'Could not load addresses.');
        this.changeDetectorRef.markForCheck();
      },
    });
  }

  add(): void {
    this.openEditor();
  }

  edit(address: Address): void {
    this.openEditor(address);
  }

  setDefault(address: Address): void {
    this.addressService.setDefault(address.id).subscribe({
      next: () => {
        this.snackBar.open('Default address updated.', 'Dismiss', { duration: 2500 });
        this.loadAddresses();
      },
      error: (error) =>
        this.snackBar.open(
          this.readError(error, 'Could not update the default address.'),
          'Dismiss',
          { duration: 4000 },
        ),
    });
  }

  remove(address: Address): void {
    this.dialog
      .open(ConfirmDialogComponent, {
        data: {
          title: 'Delete address?',
          message: `Remove the ${address.label} address?`,
          confirmLabel: 'Delete address',
        },
      })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.addressService.deleteAddress(address.id).subscribe({
          next: () => {
            this.snackBar.open('Address deleted.', 'Dismiss', { duration: 2500 });
            this.loadAddresses();
          },
          error: (error) =>
            this.snackBar.open(this.readError(error, 'Could not delete the address.'), 'Dismiss', {
              duration: 4000,
            }),
        });
      });
  }

  private openEditor(address?: Address): void {
    this.dialog
      .open(AddressDialogComponent, { data: { address }, maxWidth: 'calc(100vw - 32px)' })
      .afterClosed()
      .subscribe((request: AddressWriteRequest | undefined) => {
        if (!request) return;
        const save = address
          ? this.addressService.updateAddress(address.id, request)
          : this.addressService.createAddress(request);
        save.subscribe({
          next: () => {
            this.snackBar.open(address ? 'Address updated.' : 'Address added.', 'Dismiss', {
              duration: 2500,
            });
            this.loadAddresses();
          },
          error: (error) =>
            this.snackBar.open(this.readError(error, 'Could not save the address.'), 'Dismiss', {
              duration: 4000,
            }),
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
