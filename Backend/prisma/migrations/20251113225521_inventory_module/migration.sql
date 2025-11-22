/*
  Warnings:

  - A unique constraint covering the columns `[barcode]` on the table `Product` will be added. If there are existing duplicate values, this will fail.
  - A unique constraint covering the columns `[barcode]` on the table `ProductVariant` will be added. If there are existing duplicate values, this will fail.

*/
-- AlterTable
ALTER TABLE `Product` ADD COLUMN `barcode` VARCHAR(191) NULL;

-- AlterTable
ALTER TABLE `ProductVariant` ADD COLUMN `barcode` VARCHAR(191) NULL;

-- CreateTable
CREATE TABLE `InventoryLocation` (
    `id` INTEGER NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(191) NOT NULL,
    `code` VARCHAR(191) NULL,
    `type` ENUM('GENERAL', 'KITCHEN', 'BAR', 'STORAGE') NOT NULL DEFAULT 'GENERAL',
    `isDefault` BOOLEAN NOT NULL DEFAULT false,
    `isActive` BOOLEAN NOT NULL DEFAULT true,
    `createdAt` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `updatedAt` DATETIME(3) NOT NULL,

    UNIQUE INDEX `InventoryLocation_code_key`(`code`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- CreateTable
CREATE TABLE `InventoryItem` (
    `id` INTEGER NOT NULL AUTO_INCREMENT,
    `productId` INTEGER NOT NULL,
    `variantId` INTEGER NULL,
    `locationId` INTEGER NOT NULL,
    `currentQuantity` DECIMAL(12, 3) NOT NULL DEFAULT 0,
    `unit` ENUM('UNIT', 'PORTION', 'GRAM', 'KILOGRAM', 'LITER', 'MILLILITER') NOT NULL DEFAULT 'UNIT',
    `minThreshold` DECIMAL(12, 3) NULL,
    `maxThreshold` DECIMAL(12, 3) NULL,
    `lastMovementAt` DATETIME(3) NULL,
    `createdAt` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `updatedAt` DATETIME(3) NOT NULL,

    INDEX `InventoryItem_locationId_idx`(`locationId`),
    INDEX `InventoryItem_productId_variantId_idx`(`productId`, `variantId`),
    UNIQUE INDEX `InventoryItem_productId_variantId_locationId_key`(`productId`, `variantId`, `locationId`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- CreateTable
CREATE TABLE `InventoryMovement` (
    `id` INTEGER NOT NULL AUTO_INCREMENT,
    `productId` INTEGER NOT NULL,
    `variantId` INTEGER NULL,
    `locationId` INTEGER NULL,
    `type` ENUM('PURCHASE', 'SALE', 'ADJUSTMENT', 'WASTE', 'TRANSFER', 'SALE_RETURN') NOT NULL,
    `quantity` DECIMAL(12, 3) NOT NULL,
    `unit` ENUM('UNIT', 'PORTION', 'GRAM', 'KILOGRAM', 'LITER', 'MILLILITER') NOT NULL DEFAULT 'UNIT',
    `reason` VARCHAR(191) NULL,
    `relatedOrderId` INTEGER NULL,
    `performedByUserId` INTEGER NULL,
    `metadata` JSON NULL,
    `createdAt` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),

    INDEX `InventoryMovement_productId_idx`(`productId`),
    INDEX `InventoryMovement_variantId_idx`(`variantId`),
    INDEX `InventoryMovement_locationId_idx`(`locationId`),
    INDEX `InventoryMovement_relatedOrderId_idx`(`relatedOrderId`),
    INDEX `InventoryMovement_performedByUserId_idx`(`performedByUserId`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- CreateIndex
CREATE UNIQUE INDEX `Product_barcode_key` ON `Product`(`barcode`);

-- CreateIndex
CREATE UNIQUE INDEX `ProductVariant_barcode_key` ON `ProductVariant`(`barcode`);

-- AddForeignKey
ALTER TABLE `InventoryItem` ADD CONSTRAINT `InventoryItem_productId_fkey` FOREIGN KEY (`productId`) REFERENCES `Product`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `InventoryItem` ADD CONSTRAINT `InventoryItem_variantId_fkey` FOREIGN KEY (`variantId`) REFERENCES `ProductVariant`(`id`) ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `InventoryItem` ADD CONSTRAINT `InventoryItem_locationId_fkey` FOREIGN KEY (`locationId`) REFERENCES `InventoryLocation`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `InventoryMovement` ADD CONSTRAINT `InventoryMovement_productId_fkey` FOREIGN KEY (`productId`) REFERENCES `Product`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `InventoryMovement` ADD CONSTRAINT `InventoryMovement_variantId_fkey` FOREIGN KEY (`variantId`) REFERENCES `ProductVariant`(`id`) ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `InventoryMovement` ADD CONSTRAINT `InventoryMovement_locationId_fkey` FOREIGN KEY (`locationId`) REFERENCES `InventoryLocation`(`id`) ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `InventoryMovement` ADD CONSTRAINT `InventoryMovement_relatedOrderId_fkey` FOREIGN KEY (`relatedOrderId`) REFERENCES `Order`(`id`) ON DELETE SET NULL ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `InventoryMovement` ADD CONSTRAINT `InventoryMovement_performedByUserId_fkey` FOREIGN KEY (`performedByUserId`) REFERENCES `User`(`id`) ON DELETE SET NULL ON UPDATE CASCADE;
