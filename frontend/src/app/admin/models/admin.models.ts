export interface AdminProductListItem {
  id: number;
  name: string;
  price: number;
  stock: number;
  isActive: boolean;
  categoryName: string;
  supplierName: string;
  updatedAt: string;
}

export interface ProductSpecification {
  key: string;
  value: string;
}

export interface AdminProduct {
  id: number;
  name: string;
  description: string;
  specifications: ProductSpecification[];
  price: number;
  stock: number;
  imageUrl: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  categoryId: number;
  categoryName: string;
  supplierId: number;
  supplierName: string;
  deliveryMethodIds: number[];
}

export interface ProductWriteRequest {
  name: string;
  description: string;
  specifications: ProductSpecification[];
  price: number;
  stock: number;
  imageUrl: string | null;
  isActive: boolean;
  categoryId: number;
  supplierId: number;
  deliveryMethodIds: number[];
}

export interface AdminCategory {
  id: number;
  name: string;
  description: string | null;
  productCount: number;
}

export interface CategoryWriteRequest {
  name: string;
  description: string | null;
}

export interface AdminSupplier {
  id: number;
  name: string;
  contactEmail: string;
  phone: string;
  productCount: number;
}

export interface SupplierWriteRequest {
  name: string;
  contactEmail: string;
  phone: string;
}

export interface AdminDeliveryMethod {
  id: number;
  name: string;
  price: number;
  estimatedDays: number;
  productCount: number;
}

export interface DeliveryMethodWriteRequest {
  name: string;
  price: number;
  estimatedDays: number;
}

export interface AdminPagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminUser {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
  orderCount: number;
  isLocked: boolean;
  createdAt: string;
}

export type AdminSalesGroupBy = 'day' | 'week' | 'month';

export interface SalesReportPoint {
  periodStart: string;
  orderCount: number;
  revenue: number;
}

export interface PopularProductReport {
  productId: number;
  productName: string;
  quantitySold: number;
  revenue: number;
}
