import type {
  IOrderRepository,
  PaymentsReportFilters,
  PaymentsReportResult,
} from "../../domain/repositories/IOrderRepository";

export class GetPaymentsReport {
  constructor(private readonly repo: IOrderRepository) {}

  exec(filters: PaymentsReportFilters): Promise<PaymentsReportResult> {
    return this.repo.getPaymentsReport(filters);
  }
}
