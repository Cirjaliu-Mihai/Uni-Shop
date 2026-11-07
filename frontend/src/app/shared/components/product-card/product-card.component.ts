import { Component, Input } from '@angular/core';
import { ProductListItem } from '../../../catalog/models/catalog.models';

@Component({
  selector: 'app-product-card',
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.scss',
  standalone: false,
})
export class ProductCardComponent {
  @Input({ required: true }) product!: ProductListItem;
}
