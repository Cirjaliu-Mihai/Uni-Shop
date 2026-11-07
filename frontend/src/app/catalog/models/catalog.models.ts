export interface CategoryListItem {
  id: number;
  name: string;
  description: string | null;
  productCount: number;
}

export interface ProductListItem {
  id: number;
  name: string;
  price: number;
  stock: number;
  imageUrl: string | null;
  categoryId: number;
  categoryName: string;
  createdAt: string;
}

export interface DeliveryMethod {
  id: number;
  name: string;
  price: number;
  estimatedDays: number;
}

export interface ProductDetails extends ProductListItem {
  description: string;
  specifications: Record<string, string>;
  supplierName: string;
  deliveryMethods: DeliveryMethod[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type ProductSort = 'newest' | 'price_asc' | 'price_desc' | 'name';

export interface ProductQuery {
  search: string;
  categoryId: number | null;
  minPrice: number | null;
  maxPrice: number | null;
  inStock: boolean | null;
  sort: ProductSort;
  page: number;
  pageSize: number;
}
