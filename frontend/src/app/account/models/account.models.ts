export interface Profile {
  id: string;
  fullName: string;
  email: string;
  phone: string | null;
}

export interface Address {
  id: number;
  label: string;
  fullName: string;
  phone: string;
  street: string;
  city: string;
  county: string;
  postalCode: string;
  country: string;
  isDefault: boolean;
}

export interface AddressWriteRequest {
  label: string;
  fullName: string;
  phone: string;
  street: string;
  city: string;
  county: string;
  postalCode: string;
  country: string;
  isDefault: boolean;
}

export interface UpdateProfileRequest {
  fullName: string;
  phone: string | null;
}
