import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChartConfiguration } from 'chart.js';
import { forkJoin } from 'rxjs';
import { AdminReportService } from '../services/admin-report.service';
import { AdminSalesGroupBy, PopularProductReport, SalesReportPoint } from '../models/admin.models';

@Component({
  selector: 'app-admin-reports',
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss',
  standalone: false,
})
export class ReportsComponent implements OnInit {
  readonly dateRange = new FormGroup({
    start: new FormControl<Date | null>(null),
    end: new FormControl<Date | null>(null),
  });
  readonly groupBy = new FormControl<AdminSalesGroupBy>('day', { nonNullable: true });
  sales: SalesReportPoint[] = [];
  popularProducts: PopularProductReport[] = [];
  loading = false;
  errorMessage = '';
  readonly salesChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      y: { beginAtZero: true, ticks: { callback: (value) => `RON ${value}` } },
    },
  };
  readonly popularChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    indexAxis: 'y',
    plugins: { legend: { display: false } },
    scales: { x: { beginAtZero: true } },
  };
  salesChartData: ChartConfiguration<'line'>['data'] = { labels: [], datasets: [] };
  popularChartData: ChartConfiguration<'bar'>['data'] = { labels: [], datasets: [] };

  get totalOrders(): number {
    return this.sales.reduce((total, point) => total + point.orderCount, 0);
  }

  get totalRevenue(): number {
    return this.sales.reduce((total, point) => total + point.revenue, 0);
  }

  constructor(
    private readonly reportService: AdminReportService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef,
  ) {}

  ngOnInit(): void {
    const end = new Date();
    const start = new Date();
    start.setDate(start.getDate() - 29);
    this.dateRange.setValue({ start, end }, { emitEvent: false });
    this.loadReports();
  }

  loadReports(): void {
    const { start, end } = this.dateRange.getRawValue();
    if (!start || !end || this.dateRange.invalid) return;
    this.loading = true;
    this.errorMessage = '';
    const from = this.dateBound(start, false);
    const to = this.dateBound(end, true);
    forkJoin({
      sales: this.reportService.getSales(from, to, this.groupBy.value),
      popular: this.reportService.getPopularProducts(from, to, 10),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.sales = result.sales;
          this.popularProducts = result.popular;
          this.salesChartData = {
            labels: result.sales.map((point) => this.formatPeriod(point.periodStart)),
            datasets: [
              {
                data: result.sales.map((point) => point.revenue),
                label: 'Revenue (RON)',
                borderColor: '#236e53',
                backgroundColor: 'rgba(35, 110, 83, 0.13)',
                pointBackgroundColor: '#c95f3a',
                fill: true,
                tension: 0.24,
              },
            ],
          };
          this.popularChartData = {
            labels: result.popular.map((item) => item.productName),
            datasets: [
              {
                data: result.popular.map((item) => item.quantitySold),
                label: 'Units sold',
                backgroundColor: '#d47a52',
                borderRadius: 3,
              },
            ],
          };
          this.loading = false;
          this.changeDetectorRef.markForCheck();
        },
        error: () => {
          this.loading = false;
          this.errorMessage = 'Reports could not be loaded for this date range.';
          this.changeDetectorRef.markForCheck();
        },
      });
  }

  private dateBound(value: Date, endOfDay: boolean): string {
    const date = new Date(value);
    date.setHours(endOfDay ? 23 : 0, endOfDay ? 59 : 0, endOfDay ? 59 : 0, endOfDay ? 999 : 0);
    return date.toISOString();
  }

  private formatPeriod(value: string): string {
    return new Date(value).toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      year: this.groupBy.value === 'month' ? 'numeric' : undefined,
    });
  }
}
