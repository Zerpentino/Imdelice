-- AlterTable
ALTER TABLE `Order` ADD COLUMN `platformMarkupPct` DECIMAL(5, 2) NULL;

-- CreateTable
CREATE TABLE `ChannelConfig` (
    `id` INTEGER NOT NULL AUTO_INCREMENT,
    `source` ENUM('POS', 'UBER', 'DIDI', 'RAPPI') NOT NULL,
    `markupPct` DECIMAL(5, 2) NOT NULL,
    `createdAt` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    `updatedAt` DATETIME(3) NOT NULL,

    UNIQUE INDEX `ChannelConfig_source_key`(`source`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
