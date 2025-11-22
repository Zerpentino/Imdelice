-- CreateTable
CREATE TABLE `Expense` (
    `id` INTEGER NOT NULL AUTO_INCREMENT,
    `concept` VARCHAR(191) NOT NULL,
    `category` ENUM('SUPPLIES', 'MAINTENANCE', 'LOSS', 'OTHER') NOT NULL DEFAULT 'OTHER',
    `amountCents` INTEGER NOT NULL,
    `paymentMethod` ENUM('CASH', 'CARD', 'TRANSFER', 'OTHER') NULL,
    `notes` VARCHAR(191) NULL,
    `incurredAt` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `recordedAt` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `recordedByUserId` INTEGER NOT NULL,
    `inventoryMovementId` INTEGER NULL,
    `metadata` JSON NULL,

    UNIQUE INDEX `Expense_inventoryMovementId_key`(`inventoryMovementId`),
    INDEX `Expense_category_idx`(`category`),
    INDEX `Expense_incurredAt_idx`(`incurredAt`),
    INDEX `Expense_recordedByUserId_idx`(`recordedByUserId`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- AddForeignKey
ALTER TABLE `Expense` ADD CONSTRAINT `Expense_recordedByUserId_fkey` FOREIGN KEY (`recordedByUserId`) REFERENCES `User`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `Expense` ADD CONSTRAINT `Expense_inventoryMovementId_fkey` FOREIGN KEY (`inventoryMovementId`) REFERENCES `InventoryMovement`(`id`) ON DELETE SET NULL ON UPDATE CASCADE;
