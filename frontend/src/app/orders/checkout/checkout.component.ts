import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { UntypedFormBuilder, UntypedFormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, forkJoin, switchMap } from 'rxjs';
import { Address, AddressWriteRequest, Profile } from '../../account/models/account.models';
import { AddressService } from '../../account/services/address.service';
import { ProfileService } from '../../account/services/profile.service';
import { CartService } from '../../shopping/services/cart.service';
import { CartSnapshot } from '../../shopping/models/shopping.models';
import { CreateOrderRequest, DeliveryOption, Order, PaymentMethod } from '../models/order.models';
import { OrderService } from '../services/order.service';

@Component({
  selector: 'app-checkout',
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss',
  standalone: false,
})
export class CheckoutComponent implements OnInit {
  readonly contactForm: UntypedFormGroup;
  readonly newAddressForm: UntypedFormGroup;
  readonly deliveryForm: UntypedFormGroup;
  addresses: Address[] = [];
  deliveryOptions: DeliveryOption[] = [];
  cart: CartSnapshot | null = null;
  profile: Profile | null = null;
  loading = true;
  loadingDelivery = false;
  saving = false;
  errorMessage = '';
  stockIssues: string[] = [];

  constructor(
    formBuilder: UntypedFormBuilder,
    private readonly profileService: ProfileService,
    private readonly addressService: AddressService,
    private readonly orderService: OrderService,
    private readonly cartService: CartService,
    private readonly router: Router,
    private readonly snackBar: MatSnackBar,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
  ) {
    this.contactForm = formBuilder.group({
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
      phone: ['', [Validators.required, Validators.pattern(/^[+0-9()\-\s]{7,40}$/)]],
      useNewAddress: [false],
      addressId: [null],
    });
    this.newAddressForm = formBuilder.group({
      label: ['Home', [Validators.required, Validators.maxLength(80)]],
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      phone: ['', [Validators.required, Validators.pattern(/^[+0-9()\-\s]{7,40}$/)]],
      street: ['', [Validators.required, Validators.maxLength(300)]],
      city: ['', [Validators.required, Validators.maxLength(120)]],
      county: ['', [Validators.required, Validators.maxLength(120)]],
      postalCode: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
      country: ['Romania', [Validators.required, Validators.maxLength(100)]],
      isDefault: [false],
    });
    this.deliveryForm = formBuilder.group({
      deliveryMethodId: [null, Validators.required],
      paymentMethod: ['card' as PaymentMethod, Validators.required],
      notes: ['', Validators.maxLength(1000)],
    });
  }

  ngOnInit(): void {
    forkJoin({
      profile: this.profileService.getProfile(),
      addresses: this.addressService.getAddresses(),
      cart: this.cartService.refresh(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.profile = result.profile;
          this.addresses = result.addresses;
          this.cart = result.cart;
          this.contactForm.patchValue({
            fullName: result.profile.fullName,
            email: result.profile.email,
            phone: result.profile.phone ?? '',
            useNewAddress: result.addresses.length === 0,
            addressId:
              result.addresses.find((address) => address.isDefault)?.id ??
              result.addresses[0]?.id ??
              null,
          });
          if (result.addresses.length === 0) {
            this.newAddressForm.patchValue({
              fullName: result.profile.fullName,
              phone: result.profile.phone ?? '',
              isDefault: true,
            });
          }
          this.loading = false;
          this.changeDetectorRef.markForCheck();
          if (result.cart.items.length === 0) {
            void this.router.navigate(['/cart']);
          }
        },
        error: (error) => {
          this.loading = false;
          this.errorMessage = this.readError(error, 'Could not load checkout information.');
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  continueToDelivery(stepper: { next: () => void }): void {
    this.errorMessage = '';
    this.contactForm.markAllAsTouched();
    const useNewAddress = this.contactForm.get('useNewAddress')?.value as boolean;
    if (useNewAddress) {
      this.newAddressForm.markAllAsTouched();
    }
    const noSavedAddress = !useNewAddress && !this.contactForm.get('addressId')?.value;
    if (
      this.contactForm.invalid ||
      (useNewAddress && this.newAddressForm.invalid) ||
      noSavedAddress
    ) {
      if (noSavedAddress) this.errorMessage = 'Select a saved address or enter a new one.';
      return;
    }

    this.loadingDelivery = true;
    this.orderService
      .getDeliveryOptions()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (options) => {
          this.deliveryOptions = options;
          if (options.length > 0) {
            this.deliveryForm.patchValue({ deliveryMethodId: options[0].id });
            stepper.next();
          } else {
            this.errorMessage = 'No delivery method is available for every product in your cart.';
          }
          this.loadingDelivery = false;
          this.changeDetectorRef.markForCheck();
        },
        error: (error) => {
          this.loadingDelivery = false;
          this.errorMessage = this.readError(error, 'Could not load delivery options.');
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  submitOrder(): void {
    if (this.deliveryForm.invalid || this.saving) {
      this.deliveryForm.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.stockIssues = [];
    const orderRequest = this.contactForm.get('useNewAddress')?.value
      ? this.addressService
          .createAddress(this.newAddressForm.getRawValue() as AddressWriteRequest)
          .pipe(switchMap((address) => this.placeOrder(address.id)))
      : this.createOrderForSelectedAddress();

    orderRequest.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (order) => {
        this.saving = false;
        this.cartService.refresh().subscribe();
        void this.router.navigate(['/checkout/confirmation', order.id]);
        this.changeDetectorRef.markForCheck();
      },
      error: (error) => {
        this.saving = false;
        this.errorMessage = this.readError(error, 'Could not place your order.');
        if (error instanceof HttpErrorResponse && error.status === 409) {
          const match = this.errorMessage.match(/insufficient stock for (.*?)(?:;|$)/i);
          this.stockIssues = match?.[1] ? [match[1]] : [this.errorMessage];
        }
        this.changeDetectorRef.markForCheck();
      },
    });
  }

  selectedDelivery(): DeliveryOption | undefined {
    return this.deliveryOptions.find(
      (option) => option.id === Number(this.deliveryForm.get('deliveryMethodId')?.value),
    );
  }

  selectedAddress(): Address | undefined {
    return this.addresses.find(
      (address) => address.id === Number(this.contactForm.get('addressId')?.value),
    );
  }

  goToCart(): void {
    void this.router.navigate(['/cart']);
  }

  private placeOrder(addressId: number) {
    const contact = this.contactForm.getRawValue();
    const delivery = this.deliveryForm.getRawValue();
    const request: CreateOrderRequest = {
      addressId,
      contact: {
        fullName: String(contact.fullName).trim(),
        email: String(contact.email).trim(),
        phone: String(contact.phone).trim(),
      },
      deliveryMethodId: Number(delivery.deliveryMethodId),
      paymentMethod: delivery.paymentMethod as PaymentMethod,
      notes: String(delivery.notes ?? '').trim() || null,
    };
    return this.orderService.createOrder(request);
  }

  private createOrderForSelectedAddress(): Observable<Order> {
    const address = this.selectedAddress();
    if (!address) {
      this.saving = false;
      this.errorMessage = 'Select a saved address or enter a new one.';
      this.changeDetectorRef.markForCheck();
      return new Observable<Order>((subscriber) => subscriber.error(new Error(this.errorMessage)));
    }
    return this.placeOrder(address.id);
  }

  private readError(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.detail === 'string') {
      return error.error.detail;
    }
    return error instanceof Error ? error.message : fallback;
  }
}
