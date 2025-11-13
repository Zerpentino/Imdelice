"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.authController = exports.rolesController = exports.usersController = exports.reportsController = exports.channelConfigController = exports.ordersController = exports.menuController = exports.tablesController = exports.modifiersController = exports.productsController = exports.categoriesController = void 0;
const PrismaUserRepository_1 = require("../infra/repositories/PrismaUserRepository");
const PrismaRoleRepository_1 = require("../infra/repositories/PrismaRoleRepository");
const PrismaPermissionRepository_1 = require("../infra/repositories/PrismaPermissionRepository");
const GetPermissionsByRole_1 = require("../core/usecases/roles/GetPermissionsByRole");
const SetRolePermissions_1 = require("../core/usecases/roles/SetRolePermissions");
// Users usecases...
const CreateUser_1 = require("../core/usecases/users/CreateUser");
const UpdateUser_1 = require("../core/usecases/users/UpdateUser");
const DeleteUser_1 = require("../core/usecases/users/DeleteUser");
const GetUserById_1 = require("../core/usecases/users/GetUserById");
const ListUsers_1 = require("../core/usecases/users/ListUsers");
const UsersController_1 = require("../presentation/controllers/UsersController");
// Roles usecases...
const CreateRole_1 = require("../core/usecases/roles/CreateRole");
const UpdateRole_1 = require("../core/usecases/roles/UpdateRole");
const DeleteRole_1 = require("../core/usecases/roles/DeleteRole");
const GetRoleById_1 = require("../core/usecases/roles/GetRoleById");
const ListRoles_1 = require("../core/usecases/roles/ListRoles");
const RolesController_1 = require("../presentation/controllers/RolesController");
// Auth usecases...
const LoginByEmail_1 = require("../core/usecases/auth/LoginByEmail");
const LoginByPin_1 = require("../core/usecases/auth/LoginByPin");
const AuthController_1 = require("../presentation/controllers/AuthController");
// JWT service
const JwtService_1 = require("../infra/security/JwtService");
// ----- Catalogo (Categories/Products/Modifiers) -----
const PrismaCategoryRepository_1 = require("../infra/repositories/PrismaCategoryRepository");
const PrismaProductRepository_1 = require("../infra/repositories/PrismaProductRepository");
const PrismaModifierRepository_1 = require("../infra/repositories/PrismaModifierRepository");
const CreateCategory_1 = require("../core/usecases/categories/CreateCategory");
const ListCategories_1 = require("../core/usecases/categories/ListCategories");
const CreateProductSimple_1 = require("../core/usecases/products/CreateProductSimple");
const CreateProductVarianted_1 = require("../core/usecases/products/CreateProductVarianted");
const GetProductDetail_1 = require("../core/usecases/products/GetProductDetail");
const ListProducts_1 = require("../core/usecases/products/ListProducts");
const AttachModifierGroupToProduct_1 = require("../core/usecases/products/AttachModifierGroupToProduct");
const CreateModifierGroupWithOptions_1 = require("../core/usecases/modifiers/CreateModifierGroupWithOptions");
const CategoriesController_1 = require("../presentation/controllers/CategoriesController");
const ProductsController_1 = require("../presentation/controllers/ProductsController");
const ModifiersController_1 = require("../presentation/controllers/ModifiersController");
const UpdateCategory_1 = require("../core/usecases/categories/UpdateCategory");
const DeleteCategory_1 = require("../core/usecases/categories/DeleteCategory");
const UpdateProduct_1 = require("../core/usecases/products/UpdateProduct");
const ReplaceProductVariants_1 = require("../core/usecases/products/ReplaceProductVariants");
const DeleteProduct_1 = require("../core/usecases/products/DeleteProduct");
const UpdateModifierGroup_1 = require("../core/usecases/modifiers/UpdateModifierGroup");
const ReplaceModifierOptions_1 = require("../core/usecases/modifiers/ReplaceModifierOptions");
const DeleteModifierGroup_1 = require("../core/usecases/modifiers/DeleteModifierGroup");
const ListModifierGroups_1 = require("../core/usecases/modifiers/ListModifierGroups");
const GetModifierGroup_1 = require("../core/usecases/modifiers/GetModifierGroup");
const ListModifierGroupsByProduct_1 = require("../core/usecases/modifiers/ListModifierGroupsByProduct");
const ConvertProductToVarianted_1 = require("../core/usecases/products/ConvertProductToVarianted");
const ConvertProductToSimple_1 = require("../core/usecases/products/ConvertProductToSimple");
const ListProductsByModifierGroup_1 = require("../core/usecases/modifiers/ListProductsByModifierGroup");
const DetachModifierGroupFromProduct_1 = require("../core/usecases/products/DetachModifierGroupFromProduct");
const UpdateModifierGroupPosition_1 = require("../core/usecases/products/UpdateModifierGroupPosition");
const ReorderModifierGroups_1 = require("../core/usecases/products/ReorderModifierGroups");
const UpdateModifierOption_1 = require("../core/usecases/modifiers/UpdateModifierOption");
const AttachModifierGroupToVariant_1 = require("../core/usecases/products/AttachModifierGroupToVariant");
const UpdateVariantModifierGroup_1 = require("../core/usecases/products/UpdateVariantModifierGroup");
const DetachModifierGroupFromVariant_1 = require("../core/usecases/products/DetachModifierGroupFromVariant");
const ListVariantModifierGroups_1 = require("../core/usecases/products/ListVariantModifierGroups");
//IMPORTS COMBOSSSS
const CreateProductCombo_1 = require("../core/usecases/products/CreateProductCombo");
const AddComboItems_1 = require("../core/usecases/products/AddComboItems");
const UpdateComboItem_1 = require("../core/usecases/products/UpdateComboItem");
const RemoveComboItem_1 = require("../core/usecases/products/RemoveComboItem");
//IMPORTS DE MENU
const PrismaMenuRepository_1 = require("../infra/repositories/PrismaMenuRepository");
const MenuController_1 = require("../presentation/controllers/MenuController");
const CreateMenu_1 = require("../core/usecases/menu/CreateMenu");
const ListMenus_1 = require("../core/usecases/menu/ListMenus");
const ListArchivedMenus_1 = require("../core/usecases/menu/ListArchivedMenus");
const UpdateMenu_1 = require("../core/usecases/menu/UpdateMenu");
const DeleteMenu_1 = require("../core/usecases/menu/DeleteMenu");
const RestoreMenu_1 = require("../core/usecases/menu/RestoreMenu");
const CreateMenuSection_1 = require("../core/usecases/menu/sections/CreateMenuSection");
const UpdateMenuSection_1 = require("../core/usecases/menu/sections/UpdateMenuSection");
const DeleteMenuSection_1 = require("../core/usecases/menu/sections/DeleteMenuSection");
const DeleteMenuSectionHard_1 = require("../core/usecases/menu/sections/DeleteMenuSectionHard");
const ListMenuSections_1 = require("../core/usecases/menu/sections/ListMenuSections");
const ListArchivedMenuSections_1 = require("../core/usecases/menu/sections/ListArchivedMenuSections");
const RestoreMenuSection_1 = require("../core/usecases/menu/sections/RestoreMenuSection");
const AddMenuItem_1 = require("../core/usecases/menu/items/AddMenuItem");
const UpdateMenuItem_1 = require("../core/usecases/menu/items/UpdateMenuItem");
const RemoveMenuItem_1 = require("../core/usecases/menu/items/RemoveMenuItem");
const RestoreMenuItem_1 = require("../core/usecases/menu/items/RestoreMenuItem");
const DeleteMenuItemHard_1 = require("../core/usecases/menu/items/DeleteMenuItemHard");
const ListMenuItems_1 = require("../core/usecases/menu/items/ListMenuItems");
const ListArchivedMenuItems_1 = require("../core/usecases/menu/items/ListArchivedMenuItems");
const GetMenuPublic_1 = require("../core/usecases/menu/items/GetMenuPublic");
//pedidos
const PrismaOrderRepository_1 = require("../infra/repositories/PrismaOrderRepository");
const OrdersController_1 = require("../presentation/controllers/OrdersController");
const CreateOrder_1 = require("../core/usecases/orders/CreateOrder");
const AddOrderItem_1 = require("../core/usecases/orders/AddOrderItem");
const UpdateOrderItemStatus_1 = require("../core/usecases/orders/UpdateOrderItemStatus");
const AddPayment_1 = require("../core/usecases/orders/AddPayment");
const GetOrderDetail_1 = require("../core/usecases/orders/GetOrderDetail");
const ListKDS_1 = require("../core/usecases/orders/ListKDS");
const UpdateOrderItem_1 = require("../core/usecases/orders/UpdateOrderItem");
const RemoveOrderItem_1 = require("../core/usecases/orders/RemoveOrderItem");
const SplitOrderByItems_1 = require("../core/usecases/orders/SplitOrderByItems");
const UpdateOrderMeta_1 = require("../core/usecases/orders/UpdateOrderMeta");
const UpdateOrderStatus_1 = require("../core/usecases/orders/UpdateOrderStatus");
const ListOrders_1 = require("../core/usecases/orders/ListOrders");
const RefundOrder_1 = require("../core/usecases/orders/RefundOrder");
const AdminAuthService_1 = require("../infra/services/AdminAuthService");
const GetPaymentsReport_1 = require("../core/usecases/reports/GetPaymentsReport");
const ReportsController_1 = require("../presentation/controllers/ReportsController");
const PrismaChannelConfigRepository_1 = require("../infra/repositories/PrismaChannelConfigRepository");
const ListChannelConfigs_1 = require("../core/usecases/channelConfig/ListChannelConfigs");
const SetChannelConfig_1 = require("../core/usecases/channelConfig/SetChannelConfig");
const ChannelConfigController_1 = require("../presentation/controllers/ChannelConfigController");
// Mesas
const PrismaTableRepository_1 = require("../infra/repositories/PrismaTableRepository");
const CreateTable_1 = require("../core/usecases/tables/CreateTable");
const ListTables_1 = require("../core/usecases/tables/ListTables");
const GetTable_1 = require("../core/usecases/tables/GetTable");
const UpdateTable_1 = require("../core/usecases/tables/UpdateTable");
const DeleteTable_1 = require("../core/usecases/tables/DeleteTable");
const TablesController_1 = require("../presentation/controllers/TablesController");
const permRepo = new PrismaPermissionRepository_1.PrismaPermissionRepository();
const getPermsByRole = new GetPermissionsByRole_1.GetPermissionsByRole(permRepo);
const setRolePerms = new SetRolePermissions_1.SetRolePermissions(permRepo);
const userRepo = new PrismaUserRepository_1.PrismaUserRepository();
const roleRepo = new PrismaRoleRepository_1.PrismaRoleRepository();
// repos de productos
const categoryRepo = new PrismaCategoryRepository_1.PrismaCategoryRepository();
const productRepo = new PrismaProductRepository_1.PrismaProductRepository();
const modifierRepo = new PrismaModifierRepository_1.PrismaModifierRepository();
//REPOS DE MENU
const menuRepo = new PrismaMenuRepository_1.PrismaMenuRepository();
// Mesas
const tableRepo = new PrismaTableRepository_1.PrismaTableRepository();
//repos de pedidos
const orderRepo = new PrismaOrderRepository_1.PrismaOrderRepository();
const channelConfigRepo = new PrismaChannelConfigRepository_1.PrismaChannelConfigRepository();
// Users
const usersController = new UsersController_1.UsersController(new ListUsers_1.ListUsers(userRepo), new GetUserById_1.GetUserById(userRepo), new CreateUser_1.CreateUser(userRepo), new UpdateUser_1.UpdateUser(userRepo), new DeleteUser_1.DeleteUser(userRepo));
exports.usersController = usersController;
// Roles
const rolesController = new RolesController_1.RolesController(new ListRoles_1.ListRoles(roleRepo), new GetRoleById_1.GetRoleById(roleRepo), new CreateRole_1.CreateRole(roleRepo), new UpdateRole_1.UpdateRole(roleRepo), new DeleteRole_1.DeleteRole(roleRepo), setRolePerms);
exports.rolesController = rolesController;
// Auth
const jwtService = new JwtService_1.JwtService(); // <-- crea el servicio
const authController = new AuthController_1.AuthController(new LoginByEmail_1.LoginByEmail(userRepo), new LoginByPin_1.LoginByPin(userRepo), new JwtService_1.JwtService(), getPermsByRole // <-- inyéctalo aquí
);
exports.authController = authController;
// usecases
const createCategoryUC = new CreateCategory_1.CreateCategory(categoryRepo);
const listCategoriesUC = new ListCategories_1.ListCategories(categoryRepo);
const updateCategoryUC = new UpdateCategory_1.UpdateCategory(categoryRepo);
const deleteCategoryUC = new DeleteCategory_1.DeleteCategory(categoryRepo);
const detachModGroupUC = new DetachModifierGroupFromProduct_1.DetachModifierGroupFromProduct(productRepo);
const updateModGroupPosUC = new UpdateModifierGroupPosition_1.UpdateModifierGroupPosition(productRepo);
const reorderModGroupsUC = new ReorderModifierGroups_1.ReorderModifierGroups(productRepo); // opcional
const createProductSimpleUC = new CreateProductSimple_1.CreateProductSimple(productRepo);
const createProductVariantedUC = new CreateProductVarianted_1.CreateProductVarianted(productRepo);
const getProductDetailUC = new GetProductDetail_1.GetProductDetail(productRepo);
const listProductsUC = new ListProducts_1.ListProducts(productRepo);
const updateProductUC = new UpdateProduct_1.UpdateProduct(productRepo);
const replaceVariantsUC = new ReplaceProductVariants_1.ReplaceProductVariants(productRepo);
const deleteProductUC = new DeleteProduct_1.DeleteProduct(productRepo);
const attachModifierUC = new AttachModifierGroupToProduct_1.AttachModifierGroupToProduct(productRepo);
const attachVariantModUC = new AttachModifierGroupToVariant_1.AttachModifierGroupToVariant(productRepo);
const updateVariantModUC = new UpdateVariantModifierGroup_1.UpdateVariantModifierGroup(productRepo);
const detachVariantModUC = new DetachModifierGroupFromVariant_1.DetachModifierGroupFromVariant(productRepo);
const listVariantModUC = new ListVariantModifierGroups_1.ListVariantModifierGroups(productRepo);
// NUEVOS
const convertToVariantedUC = new ConvertProductToVarianted_1.ConvertProductToVarianted(productRepo);
const convertToSimpleUC = new ConvertProductToSimple_1.ConvertProductToSimple(productRepo);
const listProductsByGroupUC = new ListProductsByModifierGroup_1.ListProductsByModifierGroup(modifierRepo);
// instancias COMBOSSS
const createProductComboUC = new CreateProductCombo_1.CreateProductCombo(productRepo);
const addComboItemsUC = new AddComboItems_1.AddComboItems(productRepo);
const updateComboItemUC = new UpdateComboItem_1.UpdateComboItem(productRepo);
const removeComboItemUC = new RemoveComboItem_1.RemoveComboItem(productRepo);
const createModifierGroupUC = new CreateModifierGroupWithOptions_1.CreateModifierGroupWithOptions(modifierRepo);
const updateModGroupUC = new UpdateModifierGroup_1.UpdateModifierGroup(modifierRepo);
const replaceModOptionsUC = new ReplaceModifierOptions_1.ReplaceModifierOptions(modifierRepo);
const deleteModGroupUC = new DeleteModifierGroup_1.DeleteModifierGroup(modifierRepo);
const listModGroupsUC = new ListModifierGroups_1.ListModifierGroups(modifierRepo);
const getModGroupUC = new GetModifierGroup_1.GetModifierGroup(modifierRepo);
const listByProductUC = new ListModifierGroupsByProduct_1.ListModifierGroupsByProduct(modifierRepo);
const updateModifierOptionUC = new UpdateModifierOption_1.UpdateModifierOption(modifierRepo);
//instancias de menus 
const createMenuUC = new CreateMenu_1.CreateMenu(menuRepo);
const listMenusUC = new ListMenus_1.ListMenus(menuRepo);
const listArchivedMenusUC = new ListArchivedMenus_1.ListArchivedMenus(menuRepo);
const updateMenuUC = new UpdateMenu_1.UpdateMenu(menuRepo);
const deleteMenuUC = new DeleteMenu_1.DeleteMenu(menuRepo);
const restoreMenuUC = new RestoreMenu_1.RestoreMenu(menuRepo);
const createSecUC = new CreateMenuSection_1.CreateMenuSection(menuRepo);
const updateSecUC = new UpdateMenuSection_1.UpdateMenuSection(menuRepo);
const deleteSecUC = new DeleteMenuSection_1.DeleteMenuSection(menuRepo);
const deleteSecHardUC = new DeleteMenuSectionHard_1.DeleteMenuSectionHard(menuRepo);
const listSectionsUC = new ListMenuSections_1.ListMenuSections(menuRepo);
const listArchivedSectionsUC = new ListArchivedMenuSections_1.ListArchivedMenuSections(menuRepo);
const restoreSectionUC = new RestoreMenuSection_1.RestoreMenuSection(menuRepo);
const addItemUC = new AddMenuItem_1.AddMenuItem(menuRepo);
const updateItemUC = new UpdateMenuItem_1.UpdateMenuItem(menuRepo);
const removeItemUC = new RemoveMenuItem_1.RemoveMenuItem(menuRepo);
const restoreItemUC = new RestoreMenuItem_1.RestoreMenuItem(menuRepo);
const deleteItemHardUC = new DeleteMenuItemHard_1.DeleteMenuItemHard(menuRepo);
const listItemsUC = new ListMenuItems_1.ListMenuItems(menuRepo);
const listArchivedItemsUC = new ListArchivedMenuItems_1.ListArchivedMenuItems(menuRepo);
const getMenuPublicUC = new GetMenuPublic_1.GetMenuPublic(menuRepo);
// usecases de mesas
const createTableUC = new CreateTable_1.CreateTable(tableRepo);
const listTablesUC = new ListTables_1.ListTables(tableRepo);
const getTableUC = new GetTable_1.GetTable(tableRepo);
const updateTableUC = new UpdateTable_1.UpdateTable(tableRepo);
const deleteTableUC = new DeleteTable_1.DeleteTable(tableRepo);
// usecases de pedidos
const createOrderUC = new CreateOrder_1.CreateOrder(orderRepo);
const addOrderItemUC = new AddOrderItem_1.AddOrderItem(orderRepo);
const updateOrderItemStatusUC = new UpdateOrderItemStatus_1.UpdateOrderItemStatus(orderRepo);
const addPaymentUC = new AddPayment_1.AddPayment(orderRepo);
const getOrderDetailUC = new GetOrderDetail_1.GetOrderDetail(orderRepo);
const listKDSUC = new ListKDS_1.ListKDS(orderRepo);
const updateOrderItemUC = new UpdateOrderItem_1.UpdateOrderItem(orderRepo);
const removeOrderItemUC = new RemoveOrderItem_1.RemoveOrderItem(orderRepo);
const splitOrderByItemsUC = new SplitOrderByItems_1.SplitOrderByItems(orderRepo);
const updateOrderMetaUC = new UpdateOrderMeta_1.UpdateOrderMeta(orderRepo);
const updateOrderStatusUC = new UpdateOrderStatus_1.UpdateOrderStatus(orderRepo);
const listOrdersUC = new ListOrders_1.ListOrders(orderRepo);
const getPaymentsReportUC = new GetPaymentsReport_1.GetPaymentsReport(orderRepo);
const refundOrderUC = new RefundOrder_1.RefundOrder(orderRepo);
const adminAuthService = new AdminAuthService_1.AdminAuthService();
const listChannelConfigsUC = new ListChannelConfigs_1.ListChannelConfigs(channelConfigRepo);
const setChannelConfigUC = new SetChannelConfig_1.SetChannelConfig(channelConfigRepo);
// controllers
// controllers (reconstruye con deps nuevas)
exports.categoriesController = new CategoriesController_1.CategoriesController(createCategoryUC, listCategoriesUC, updateCategoryUC, deleteCategoryUC);
exports.productsController = new ProductsController_1.ProductsController(createProductSimpleUC, createProductVariantedUC, getProductDetailUC, listProductsUC, attachModifierUC, // o attachModUC, iguala el nombre con tu var real
updateProductUC, replaceVariantsUC, deleteProductUC, 
// ⬇️ primero las conversiones
convertToVariantedUC, convertToSimpleUC, 
// ⬇️ luego COMBOS
createProductComboUC, addComboItemsUC, updateComboItemUC, removeComboItemUC, detachModGroupUC, updateModGroupPosUC, reorderModGroupsUC, attachVariantModUC, updateVariantModUC, detachVariantModUC, listVariantModUC);
exports.modifiersController = new ModifiersController_1.ModifiersController(createModifierGroupUC, updateModGroupUC, replaceModOptionsUC, deleteModGroupUC, listModGroupsUC, getModGroupUC, listByProductUC, listProductsByGroupUC, updateModifierOptionUC);
exports.tablesController = new TablesController_1.TablesController(createTableUC, listTablesUC, getTableUC, updateTableUC, deleteTableUC);
//menu
exports.menuController = new MenuController_1.MenuController(createMenuUC, listMenusUC, listArchivedMenusUC, updateMenuUC, deleteMenuUC, restoreMenuUC, createSecUC, updateSecUC, deleteSecUC, deleteSecHardUC, restoreSectionUC, listSectionsUC, listArchivedSectionsUC, addItemUC, updateItemUC, removeItemUC, restoreItemUC, deleteItemHardUC, listItemsUC, listArchivedItemsUC, getMenuPublicUC);
exports.ordersController = new OrdersController_1.OrdersController(createOrderUC, addOrderItemUC, updateOrderItemStatusUC, addPaymentUC, getOrderDetailUC, listKDSUC, updateOrderItemUC, removeOrderItemUC, splitOrderByItemsUC, updateOrderMetaUC, updateOrderStatusUC, listOrdersUC, refundOrderUC, adminAuthService);
exports.channelConfigController = new ChannelConfigController_1.ChannelConfigController(listChannelConfigsUC, setChannelConfigUC);
exports.reportsController = new ReportsController_1.ReportsController(getPaymentsReportUC);
//# sourceMappingURL=index.js.map