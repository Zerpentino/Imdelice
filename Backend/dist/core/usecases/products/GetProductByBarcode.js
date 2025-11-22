"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.GetProductByBarcode = void 0;
class GetProductByBarcode {
    constructor(repo) {
        this.repo = repo;
    }
    exec(barcode) {
        return this.repo.findByBarcode(barcode);
    }
}
exports.GetProductByBarcode = GetProductByBarcode;
//# sourceMappingURL=GetProductByBarcode.js.map