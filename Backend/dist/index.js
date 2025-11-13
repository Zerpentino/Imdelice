"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
require("dotenv/config");
const express_1 = __importDefault(require("express"));
const cors_1 = __importDefault(require("cors"));
const users_routes_1 = __importDefault(require("./routes/users.routes"));
const errorHandler_1 = require("./presentation/middlewares/errorHandler");
//roles
const roles_routes_1 = __importDefault(require("./routes/roles.routes"));
const auth_routes_1 = __importDefault(require("./routes/auth.routes"));
//Productos
const categories_routes_1 = __importDefault(require("./routes/categories.routes"));
const products_routes_1 = __importDefault(require("./routes/products.routes"));
const modifiers_routes_1 = __importDefault(require("./routes/modifiers.routes"));
const channelConfig_routes_1 = __importDefault(require("./routes/channelConfig.routes"));
const reports_routes_1 = __importDefault(require("./routes/reports.routes"));
//Menu
const menu_routes_1 = __importDefault(require("./routes/menu.routes"));
//pedido
const orders_routes_1 = __importDefault(require("./routes/orders.routes"));
//mesas
const tables_routes_1 = __importDefault(require("./routes/tables.routes"));
const app = (0, express_1.default)();
app.use((0, cors_1.default)());
app.use(express_1.default.json());
app.get('/', (_req, res) => {
    res.send('API OK mandando respuesta');
});
app.use('/api/users', users_routes_1.default);
app.use("/api/roles", roles_routes_1.default);
app.use('/api/auth', auth_routes_1.default);
app.use('/api/categories', categories_routes_1.default);
app.use('/api/products', products_routes_1.default);
app.use('/api/modifiers', modifiers_routes_1.default);
app.use('/api/tables', tables_routes_1.default);
//MENU
app.use('/api/menus', menu_routes_1.default);
//pedido
app.use("/api/orders", orders_routes_1.default);
app.use('/api/channel-config', channelConfig_routes_1.default);
app.use('/api/reports', reports_routes_1.default);
// Middleware global de errores al final
app.use(errorHandler_1.errorHandler);
const port = Number(process.env.PORT) || 3000;
const host = process.env.HOST || '0.0.0.0';
app.listen(port, host, () => {
    console.log(`API up on http://${host === '0.0.0.0' ? 'localhost' : host}:${port}`);
});
//# sourceMappingURL=index.js.map