import { AddressWriteRequest } from '../../account/models/account.models';

export interface DeliveryOption {
  id: number;
  name: string;
  price: number;
  estimatedDays: number;
}

export type OrderStatus = 'processing' | 'shipped' | 'delivered' | 'cancelled';
export type PaymentMethod = 'card' | 'cashOnDelivery';
export type PaymentStatus = 'pending' | 'paid' | 'failed' | 'refunded';

export interface OrderItem {
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  id: number;
  createdAt: string;
  status: OrderStatus;
  contactName: string;
  contactEmail: string;
  contactPhone: string;
  shippingAddressLabel: string;
  shippingFullName: string;
  shippingPhone: string;
  shippingStreet: string;
  shippingCity: string;
  shippingCounty: string;
  shippingPostalCode: string;
  shippingCountry: string;
  deliveryMethodName: string;
  deliveryMethodPrice: number;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  subtotal: number;
  deliveryCost: number;
  total: number;
  notes: string | null;
  items: OrderItem[];
}

export interface OrderListItem {
  id: number;
  contactEmail: string;
  createdAt: string;
  status: OrderStatus;
  itemCount: number;
  total: number;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
}

export interface OrderPage {
  items: OrderListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CreateOrderRequest {
  addressId: number;
  contact: {
    fullName: string;
    email: string;
    phone: string;
  };
  deliveryMethodId: number;
  paymentMethod: PaymentMethod;
  notes: string | null;
}

export interface NewAddressCheckoutRequest extends AddressWriteRequest {}
