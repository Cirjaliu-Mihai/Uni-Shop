import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AdminOrderFilters, AdminOrderService } from '../services/admin-order.service';
import { AdminOrderListComponent } from './admin-order-list.component';

describe('AdminOrderListComponent', () => {
  it('filters by inclusive local dates and clears the range', async () => {
    const requests: AdminOrderFilters[] = [];
    await TestBed.configureTestingModule({
      declarations: [AdminOrderListComponent],
      providers: [
        {
          provide: AdminOrderService,
          useValue: {
            getOrders: (filters: AdminOrderFilters) => {
              requests.push(filters);
              return of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
            },
          },
        },
      ],
    })
      .overrideComponent(AdminOrderListComponent, { set: { template: '' } })
      .compileComponents();

    const fixture = TestBed.createComponent(AdminOrderListComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.page = 3;
    component.dateRange.setValue({ start: new Date(2026, 1, 3), end: new Date(2026, 1, 5) });
    component.changeDateRange();

    expect(requests.at(-1)).toMatchObject({
      from: new Date(2026, 1, 3, 0, 0, 0, 0).toISOString(),
      to: new Date(2026, 1, 5, 23, 59, 59, 999).toISOString(),
      page: 1,
    });

    component.clearDateRange();
    expect(requests.at(-1)).toMatchObject({ from: '', to: '', page: 1 });
    expect(component.dateRange.value).toEqual({ start: null, end: null });
  });
});
