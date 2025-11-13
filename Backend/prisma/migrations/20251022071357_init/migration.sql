-- CreateTable
CREATE TABLE `ProductVariantModifierGroup` (
    `id` INTEGER NOT NULL AUTO_INCREMENT,
    `variantId` INTEGER NOT NULL,
    `groupId` INTEGER NOT NULL,
    `minSelect` INTEGER NOT NULL DEFAULT 0,
    `maxSelect` INTEGER NULL,
    `isRequired` BOOLEAN NOT NULL DEFAULT false,

    INDEX `ProductVariantModifierGroup_groupId_idx`(`groupId`),
    UNIQUE INDEX `ProductVariantModifierGroup_variantId_groupId_key`(`variantId`, `groupId`),
    PRIMARY KEY (`id`)
) DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- AddForeignKey
ALTER TABLE `ProductVariantModifierGroup` ADD CONSTRAINT `ProductVariantModifierGroup_variantId_fkey` FOREIGN KEY (`variantId`) REFERENCES `ProductVariant`(`id`) ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE `ProductVariantModifierGroup` ADD CONSTRAINT `ProductVariantModifierGroup_groupId_fkey` FOREIGN KEY (`groupId`) REFERENCES `ModifierGroup`(`id`) ON DELETE CASCADE ON UPDATE CASCADE;
