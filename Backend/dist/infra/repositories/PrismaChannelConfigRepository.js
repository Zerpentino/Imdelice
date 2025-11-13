"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PrismaChannelConfigRepository = void 0;
const prisma_1 = require("../../lib/prisma");
class PrismaChannelConfigRepository {
    list() {
        return prisma_1.prisma.channelConfig.findMany({ orderBy: { source: "asc" } });
    }
    async upsert(source, markupPct) {
        return prisma_1.prisma.channelConfig.upsert({
            where: { source },
            update: { markupPct },
            create: { source, markupPct },
        });
    }
}
exports.PrismaChannelConfigRepository = PrismaChannelConfigRepository;
//# sourceMappingURL=PrismaChannelConfigRepository.js.map