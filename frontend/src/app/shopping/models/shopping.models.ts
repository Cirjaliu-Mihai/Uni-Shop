export interface WishlistEntry {
  productId: number;
  name: string;
  price: number;
  imageUrl: string | null;
  stock: number;
  categoryId: number;
  categoryName: string;
  addedAt: string;
}

export interface GuestCartItem {
  productId: number;
  quantity: number;
}

export interface CartLine {
  productId: number;
  name: string;
  imageUrl: string | null;
  price: number;
  stock: number;
  quantity: number;
  lineTotal: number;
  exceedsStock: boolean;
  categoryName?: string;
}

export interface CartSnapshot {
  items: CartLine[];
  total: number;
  hasStockWarnings: boolean;
}

export const emptyCart = (): CartSnapshot => ({ items: [], total: 0, hasStockWarnings: false });
