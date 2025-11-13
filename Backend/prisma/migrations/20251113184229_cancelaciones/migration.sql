-- AlterTable
ALTER TABLE `Order` ADD COLUMN `refundedAt` DATETIME(3) NULL,
    MODIFY `status` ENUM('DRAFT', 'OPEN', 'HOLD', 'CLOSED', 'CANCELED', 'REFUNDED') NOT NULL DEFAULT 'OPEN';

-- CreateTable
CREATE TABLE `OrderRefundLog` (
    `id` INTEGER NOT NULL AUTO_INCREMENT,
    `orderId` INTEGER NOT NULL,
    `adminUserId` INTEGER NOT NULL,
    `amountCents` INTEGER NOT NULL,
    `tipCents` INTEGER NOT NULL DEFAULT 0,
    `reason` VARCHAR(191) NULL,
    `createdAt` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),

    INDEX `OrderRefundLog_orderId_idx`(`orderId`),
    INDEX `OrderRefundLog_adminUserId_idx`(`adminUserId`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- AddForeignKey
ALTER TABLE `OrderRefundLog` ADD CONSTRAINT `OrderRefundLog_orderId_fkey` FOREIGN KEY (`orderId`) REFERENCES `Order`(`id`) ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `OrderRefundLog` ADD CONSTRAINT `OrderRefundLog_adminUserId_fkey` FOREIGN KEY (`adminUserId`) REFERENCES `User`(`id`) ON DELETE RESTRICT ON UPDATE CASCADE;
