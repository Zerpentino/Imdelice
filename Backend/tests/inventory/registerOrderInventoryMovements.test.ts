import assert from "node:assert/strict";
import { InventoryMovementType } from "@prisma/client";
import { RegisterOrderInventoryMovements } from "../../src/core/usecases/inventory/RegisterOrderInventoryMovements";
import type {
  CreateInventoryMovementInput,
  IInventoryRepository,
} from "../../src/core/domain/repositories/IInventoryRepository";

class FakeInventoryRepository implements IInventoryRepository {
  public created: CreateInventoryMovementInput[] = [];
  private registered = new Set<string>();

  record(orderId: number, type: InventoryMovementType) {
    this.registered.add(`${orderId}:${type}`);
  }

  async getDefaultLocation() {
    return null;
  }
  async ensureDefaultLocation() {
    throw new Error("not implemented");
  }
  async findItem() {
    return null;
  }
  async listItems() {
    return [];
  }
  async listMovements() {
    return [];
  }

  async createMovement(input: CreateInventoryMovementInput) {
    this.created.push(input);
    if (input.relatedOrderId) {
      this.record(input.relatedOrderId, input.type);
    }
    return {} as any;
  }

  async hasOrderMovement(orderId: number, type: InventoryMovementType): Promise<boolean> {
    return this.registered.has(`${orderId}:${type}`);
  }
}

async function testRegistersLeafItemsOnly() {
  const repo = new FakeInventoryRepository();
  const uc = new RegisterOrderInventoryMovements(repo);

  await uc.exec({
    order: {
      id: 100,
      items: [
        {
          id: 1,
          productId: 10,
          variantId: null,
          quantity: 1,
          parentItemId: null,
          childItems: [
            { id: 2, productId: 20, variantId: 200, quantity: 2, parentItemId: 1 },
            { id: 3, productId: 21, variantId: null, quantity: 1, parentItemId: 1 },
          ],
        },
      ],
    },
    movementType: InventoryMovementType.SALE,
    userId: 7,
  });

  assert.equal(repo.created.length, 2, "should create one movement per leaf component");
  assert.deepEqual(
    repo.created.map((m) => ({ productId: m.productId, quantity: m.quantity })),
    [
      { productId: 20, quantity: -2 },
      { productId: 21, quantity: -1 },
    ],
  );
}

async function testSkipsIfAlreadyRegistered() {
  const repo = new FakeInventoryRepository();
  repo.record(101, InventoryMovementType.SALE);
  const uc = new RegisterOrderInventoryMovements(repo);

  await uc.exec({
    order: {
      id: 101,
      items: [{ id: 4, productId: 30, variantId: null, quantity: 1, parentItemId: null }],
    },
    movementType: InventoryMovementType.SALE,
    userId: 1,
  });

  assert.equal(repo.created.length, 0, "should not duplicate movements for same order+type");
}

async function testSkipsSaleReturnWithoutSale() {
  const repo = new FakeInventoryRepository();
  const uc = new RegisterOrderInventoryMovements(repo);

  await uc.exec({
    order: {
      id: 200,
      items: [{ id: 6, productId: 40, variantId: null, quantity: 1, parentItemId: null }],
    },
    movementType: InventoryMovementType.SALE_RETURN,
    options: { requireSaleToExist: true },
  });

  assert.equal(repo.created.length, 0, "should skip sale return when sale never recorded");
}

async function testForceAllowsRepostingSale() {
  const repo = new FakeInventoryRepository();
  repo.record(300, InventoryMovementType.SALE);
  const uc = new RegisterOrderInventoryMovements(repo);

  await uc.exec({
    order: {
      id: 300,
      items: [{ id: 7, productId: 50, variantId: null, quantity: 1, parentItemId: null }],
    },
    movementType: InventoryMovementType.SALE,
    options: { force: true },
  });

  assert.equal(repo.created.length, 1, "force flag should allow re-registering sale");
}

async function run() {
  await testRegistersLeafItemsOnly();
  await testSkipsIfAlreadyRegistered();
  await testSkipsSaleReturnWithoutSale();
  await testForceAllowsRepostingSale();
  console.log("RegisterOrderInventoryMovements tests passed ✅");
}

run().catch((err) => {
  console.error(err);
  process.exit(1);
});
