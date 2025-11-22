import type { Expense, ExpenseCategory, PaymentMethod } from "@prisma/client";

export interface ExpenseFilters {
  from?: Date;
  to?: Date;
  category?: ExpenseCategory;
  recordedByUserId?: number;
  search?: string;
}

export interface ExpenseSummaryResult {
  totalAmountCents: number;
  totalsByCategory: Array<{ category: ExpenseCategory; amountCents: number }>;
}

export interface CreateExpenseInput {
  concept: string;
  category: ExpenseCategory;
  amountCents: number;
  paymentMethod?: PaymentMethod | null;
  notes?: string | null;
  incurredAt?: Date;
  recordedByUserId: number;
  inventoryMovementId?: number | null;
  metadata?: Record<string, any> | null;
}

export interface UpdateExpenseInput {
  concept?: string;
  category?: ExpenseCategory;
  amountCents?: number;
  paymentMethod?: PaymentMethod | null;
  notes?: string | null;
  incurredAt?: Date;
  inventoryMovementId?: number | null;
  metadata?: Record<string, any> | null;
}

export interface IExpenseRepository {
  create(data: CreateExpenseInput): Promise<Expense>;
  list(filters?: ExpenseFilters): Promise<Expense[]>;
  getById(id: number): Promise<Expense | null>;
  update(id: number, data: UpdateExpenseInput): Promise<Expense>;
  delete(id: number): Promise<void>;
  getSummary(filters?: ExpenseFilters): Promise<ExpenseSummaryResult>;
}
