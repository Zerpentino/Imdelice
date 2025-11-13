import { PrismaUserRepository } from '../infra/repositories/PrismaUserRepository';
import { PrismaRoleRepository } from '../infra/repositories/PrismaRoleRepository';
import { PrismaPermissionRepository } from '../infra/repositories/PrismaPermissionRepository'
import { GetPermissionsByRole } from '../core/usecases/roles/GetPermissionsByRole'
import { SetRolePermissions } from '../core/usecases/roles/SetRolePermissions'

// Users usecases...
import { CreateUser } from '../core/usecases/users/CreateUser';
import { UpdateUser } from '../core/usecases/users/UpdateUser';
import { DeleteUser } from '../core/usecases/users/DeleteUser';
import { GetUserById } from '../core/usecases/users/GetUserById';
import { ListUsers } from '../core/usecases/users/ListUsers';
import { UsersController } from '../presentation/controllers/UsersController';

// Roles usecases...
import { CreateRole } from '../core/usecases/roles/CreateRole';
import { UpdateRole } from '../core/usecases/roles/UpdateRole';
import { DeleteRole } from '../core/usecases/roles/DeleteRole';
import { GetRoleById } from '../core/usecases/roles/GetRoleById';
import { ListRoles } from '../core/usecases/roles/ListRoles';
import { RolesController } from '../presentation/controllers/RolesController';

// Auth usecases...
import { LoginByEmail } from '../core/usecases/auth/LoginByEmail';
import { LoginByPin } from '../core/usecases/auth/LoginByPin';
import { AuthController } from '../presentation/controllers/AuthController';

// JWT service
import { JwtService } from '../infra/security/JwtService';

// ----- Catalogo (Categories/Products/Modifiers) -----
import { PrismaCategoryRepository } from '../infra/repositories/PrismaCategoryRepository';
import { PrismaProductRepository }  from '../infra/repositories/PrismaProductRepository';
import { PrismaModifierRepository } from '../infra/repositories/PrismaModifierRepository';

import { CreateCategory } from '../core/usecases/categories/CreateCategory';
import { ListCategories } from '../core/usecases/categories/ListCategories';

import { CreateProductSimple }    from '../core/usecases/products/CreateProductSimple';
import { CreateProductVarianted } from '../core/usecases/products/CreateProductVarianted';
import { GetProductDetail }       from '../core/usecases/products/GetProductDetail';
import { ListProducts }           from '../core/usecases/products/ListProducts';
import { AttachModifierGroupToProduct } from '../core/usecases/products/AttachModifierGroupToProduct';

import { CreateModifierGroupWithOptions } from '../core/usecases/modifiers/CreateModifierGroupWithOptions';

import { CategoriesController } from '../presentation/controllers/CategoriesController';
import { ProductsController }   from '../presentation/controllers/ProductsController';
import { ModifiersController }  from '../presentation/controllers/ModifiersController';


import { UpdateCategory } from '../core/usecases/categories/UpdateCategory';
import { DeleteCategory } from '../core/usecases/categories/DeleteCategory';
import { UpdateProduct } from '../core/usecases/products/UpdateProduct';
import { ReplaceProductVariants } from '../core/usecases/products/ReplaceProductVariants';
import { DeleteProduct } from '../core/usecases/products/DeleteProduct';
import { UpdateModifierGroup } from '../core/usecases/modifiers/UpdateModifierGroup';
import { ReplaceModifierOptions } from '../core/usecases/modifiers/ReplaceModifierOptions';
import { DeleteModifierGroup } from '../core/usecases/modifiers/DeleteModifierGroup';
import { ListModifierGroups } from '../core/usecases/modifiers/ListModifierGroups';
import { GetModifierGroup } from '../core/usecases/modifiers/GetModifierGroup';
import { ListModifierGroupsByProduct } from '../core/usecases/modifiers/ListModifierGroupsByProduct';
import { ConvertProductToVarianted } from '../core/usecases/products/ConvertProductToVarianted';
import { ConvertProductToSimple } from '../core/usecases/products/ConvertProductToSimple';
import { ListProductsByModifierGroup } from '../core/usecases/modifiers/ListProductsByModifierGroup';
import { DetachModifierGroupFromProduct } from '../core/usecases/products/DetachModifierGroupFromProduct';
import { UpdateModifierGroupPosition } from '../core/usecases/products/UpdateModifierGroupPosition';
import { ReorderModifierGroups } from '../core/usecases/products/ReorderModifierGroups';
import { UpdateModifierOption } from '../core/usecases/modifiers/UpdateModifierOption';
import { AttachModifierGroupToVariant } from '../core/usecases/products/AttachModifierGroupToVariant';
import { UpdateVariantModifierGroup } from '../core/usecases/products/UpdateVariantModifierGroup';
import { DetachModifierGroupFromVariant } from '../core/usecases/products/DetachModifierGroupFromVariant';
import { ListVariantModifierGroups } from '../core/usecases/products/ListVariantModifierGroups';


//IMPORTS COMBOSSSS
import { CreateProductCombo } from '../core/usecases/products/CreateProductCombo';
import { AddComboItems } from '../core/usecases/products/AddComboItems';
import { UpdateComboItem } from '../core/usecases/products/UpdateComboItem';
import { RemoveComboItem } from '../core/usecases/products/RemoveComboItem';

//IMPORTS DE MENU
import { PrismaMenuRepository } from '../infra/repositories/PrismaMenuRepository';
import { MenuController } from '../presentation/controllers/MenuController';
import { CreateMenu } from '../core/usecases/menu/CreateMenu';
import { ListMenus } from '../core/usecases/menu/ListMenus';
import { ListArchivedMenus } from '../core/usecases/menu/ListArchivedMenus';
import { UpdateMenu } from '../core/usecases/menu/UpdateMenu';
import { DeleteMenu } from '../core/usecases/menu/DeleteMenu';
import { RestoreMenu } from '../core/usecases/menu/RestoreMenu';
import { CreateMenuSection } from '../core/usecases/menu/sections/CreateMenuSection';
import { UpdateMenuSection } from '../core/usecases/menu/sections/UpdateMenuSection';
import { DeleteMenuSection } from '../core/usecases/menu/sections/DeleteMenuSection';
import { DeleteMenuSectionHard } from '../core/usecases/menu/sections/DeleteMenuSectionHard';
import { ListMenuSections } from '../core/usecases/menu/sections/ListMenuSections';
import { ListArchivedMenuSections } from '../core/usecases/menu/sections/ListArchivedMenuSections';
import { RestoreMenuSection } from '../core/usecases/menu/sections/RestoreMenuSection';
import { AddMenuItem } from '../core/usecases/menu/items/AddMenuItem';
import { UpdateMenuItem } from '../core/usecases/menu/items/UpdateMenuItem';
import { RemoveMenuItem } from '../core/usecases/menu/items/RemoveMenuItem';
import { RestoreMenuItem } from '../core/usecases/menu/items/RestoreMenuItem';
import { DeleteMenuItemHard } from '../core/usecases/menu/items/DeleteMenuItemHard';
import { ListMenuItems } from '../core/usecases/menu/items/ListMenuItems';
import { ListArchivedMenuItems } from '../core/usecases/menu/items/ListArchivedMenuItems';
import { GetMenuPublic } from '../core/usecases/menu/items/GetMenuPublic';

//pedidos
import { PrismaOrderRepository } from "../infra/repositories/PrismaOrderRepository";
import { OrdersController } from "../presentation/controllers/OrdersController";
import { CreateOrder } from "../core/usecases/orders/CreateOrder";
import { AddOrderItem } from "../core/usecases/orders/AddOrderItem";
import { UpdateOrderItemStatus } from "../core/usecases/orders/UpdateOrderItemStatus";
import { AddPayment } from "../core/usecases/orders/AddPayment";
import { GetOrderDetail } from "../core/usecases/orders/GetOrderDetail";
import { ListKDS } from "../core/usecases/orders/ListKDS";
import { UpdateOrderItem } from "../core/usecases/orders/UpdateOrderItem";
import { RemoveOrderItem } from "../core/usecases/orders/RemoveOrderItem";
import { SplitOrderByItems } from "../core/usecases/orders/SplitOrderByItems";
import { UpdateOrderMeta } from "../core/usecases/orders/UpdateOrderMeta";
import { UpdateOrderStatus } from "../core/usecases/orders/UpdateOrderStatus";
import { ListOrders } from "../core/usecases/orders/ListOrders";
import { RefundOrder } from "../core/usecases/orders/RefundOrder";
import { AdminAuthService } from "../infra/services/AdminAuthService";
import { GetPaymentsReport } from "../core/usecases/reports/GetPaymentsReport";
import { ReportsController } from "../presentation/controllers/ReportsController";
import { PrismaChannelConfigRepository } from "../infra/repositories/PrismaChannelConfigRepository";
import { ListChannelConfigs } from "../core/usecases/channelConfig/ListChannelConfigs";
import { SetChannelConfig } from "../core/usecases/channelConfig/SetChannelConfig";
import { ChannelConfigController } from "../presentation/controllers/ChannelConfigController";

// Mesas
import { PrismaTableRepository } from '../infra/repositories/PrismaTableRepository';
import { CreateTable } from '../core/usecases/tables/CreateTable';
import { ListTables } from '../core/usecases/tables/ListTables';
import { GetTable } from '../core/usecases/tables/GetTable';
import { UpdateTable } from '../core/usecases/tables/UpdateTable';
import { DeleteTable } from '../core/usecases/tables/DeleteTable';
import { TablesController } from '../presentation/controllers/TablesController';


const permRepo = new PrismaPermissionRepository()
const getPermsByRole = new GetPermissionsByRole(permRepo)
const setRolePerms   = new SetRolePermissions(permRepo)


const userRepo = new PrismaUserRepository();
const roleRepo = new PrismaRoleRepository();
// repos de productos
const categoryRepo = new PrismaCategoryRepository();
const productRepo  = new PrismaProductRepository();
const modifierRepo = new PrismaModifierRepository();

//REPOS DE MENU
const menuRepo = new PrismaMenuRepository();
// Mesas
const tableRepo = new PrismaTableRepository();
//repos de pedidos
const orderRepo = new PrismaOrderRepository();
const channelConfigRepo = new PrismaChannelConfigRepository();

// Users
const usersController = new UsersController(
  new ListUsers(userRepo),
  new GetUserById(userRepo),
  new CreateUser(userRepo),
  new UpdateUser(userRepo),
  new DeleteUser(userRepo)
);

// Roles
const rolesController = new RolesController(
  new ListRoles(roleRepo),
  new GetRoleById(roleRepo),
  new CreateRole(roleRepo),
  new UpdateRole(roleRepo),
  new DeleteRole(roleRepo),
  setRolePerms
);

// Auth
const jwtService = new JwtService(); // <-- crea el servicio
const authController = new AuthController(
  new LoginByEmail(userRepo),
  new LoginByPin(userRepo),
  new JwtService(), getPermsByRole                  // <-- inyéctalo aquí
);


// usecases
const createCategoryUC = new CreateCategory(categoryRepo);
const listCategoriesUC = new ListCategories(categoryRepo);
const updateCategoryUC = new UpdateCategory(categoryRepo);
const deleteCategoryUC = new DeleteCategory(categoryRepo);

const detachModGroupUC = new DetachModifierGroupFromProduct(productRepo);
const updateModGroupPosUC = new UpdateModifierGroupPosition(productRepo);
const reorderModGroupsUC = new ReorderModifierGroups(productRepo); // opcional


const createProductSimpleUC    = new CreateProductSimple(productRepo);
const createProductVariantedUC = new CreateProductVarianted(productRepo);
const getProductDetailUC       = new GetProductDetail(productRepo);
const listProductsUC           = new ListProducts(productRepo);
const updateProductUC = new UpdateProduct(productRepo);
const replaceVariantsUC = new ReplaceProductVariants(productRepo);
const deleteProductUC = new DeleteProduct(productRepo);
const attachModifierUC         = new AttachModifierGroupToProduct(productRepo);
const attachVariantModUC       = new AttachModifierGroupToVariant(productRepo);
const updateVariantModUC       = new UpdateVariantModifierGroup(productRepo);
const detachVariantModUC       = new DetachModifierGroupFromVariant(productRepo);
const listVariantModUC         = new ListVariantModifierGroups(productRepo);

// NUEVOS
const convertToVariantedUC     = new ConvertProductToVarianted(productRepo);
const convertToSimpleUC        = new ConvertProductToSimple(productRepo);
const listProductsByGroupUC = new ListProductsByModifierGroup(modifierRepo);


// instancias COMBOSSS
const createProductComboUC = new CreateProductCombo(productRepo);
const addComboItemsUC      = new AddComboItems(productRepo);
const updateComboItemUC    = new UpdateComboItem(productRepo);
const removeComboItemUC    = new RemoveComboItem(productRepo);



const createModifierGroupUC = new CreateModifierGroupWithOptions(modifierRepo);

const updateModGroupUC = new UpdateModifierGroup(modifierRepo);
const replaceModOptionsUC = new ReplaceModifierOptions(modifierRepo);
const deleteModGroupUC = new DeleteModifierGroup(modifierRepo);
const listModGroupsUC = new ListModifierGroups(modifierRepo);
const getModGroupUC   = new GetModifierGroup(modifierRepo);
const listByProductUC = new ListModifierGroupsByProduct(modifierRepo);
const updateModifierOptionUC = new UpdateModifierOption(modifierRepo);



//instancias de menus 

const createMenuUC = new CreateMenu(menuRepo);
const listMenusUC  = new ListMenus(menuRepo);
const listArchivedMenusUC = new ListArchivedMenus(menuRepo);
const updateMenuUC = new UpdateMenu(menuRepo);
const deleteMenuUC = new DeleteMenu(menuRepo);
const restoreMenuUC = new RestoreMenu(menuRepo);

const createSecUC = new CreateMenuSection(menuRepo);
const updateSecUC = new UpdateMenuSection(menuRepo);
const deleteSecUC = new DeleteMenuSection(menuRepo);
const deleteSecHardUC = new DeleteMenuSectionHard(menuRepo);
const listSectionsUC = new ListMenuSections(menuRepo);
const listArchivedSectionsUC = new ListArchivedMenuSections(menuRepo);
const restoreSectionUC = new RestoreMenuSection(menuRepo);

const addItemUC    = new AddMenuItem(menuRepo);
const updateItemUC = new UpdateMenuItem(menuRepo);
const removeItemUC = new RemoveMenuItem(menuRepo);
const restoreItemUC = new RestoreMenuItem(menuRepo);
const deleteItemHardUC = new DeleteMenuItemHard(menuRepo);
const listItemsUC = new ListMenuItems(menuRepo);
const listArchivedItemsUC = new ListArchivedMenuItems(menuRepo);

const getMenuPublicUC = new GetMenuPublic(menuRepo);

// usecases de mesas
const createTableUC = new CreateTable(tableRepo);
const listTablesUC = new ListTables(tableRepo);
const getTableUC = new GetTable(tableRepo);
const updateTableUC = new UpdateTable(tableRepo);
const deleteTableUC = new DeleteTable(tableRepo);
// usecases de pedidos
const createOrderUC = new CreateOrder(orderRepo);
const addOrderItemUC = new AddOrderItem(orderRepo);
const updateOrderItemStatusUC = new UpdateOrderItemStatus(orderRepo);
const addPaymentUC = new AddPayment(orderRepo);
const getOrderDetailUC = new GetOrderDetail(orderRepo);
const listKDSUC = new ListKDS(orderRepo);
const updateOrderItemUC = new UpdateOrderItem(orderRepo);
const removeOrderItemUC = new RemoveOrderItem(orderRepo);
const splitOrderByItemsUC = new SplitOrderByItems(orderRepo);
const updateOrderMetaUC = new UpdateOrderMeta(orderRepo);
const updateOrderStatusUC = new UpdateOrderStatus(orderRepo);
const listOrdersUC = new ListOrders(orderRepo);
const getPaymentsReportUC = new GetPaymentsReport(orderRepo);
const refundOrderUC = new RefundOrder(orderRepo);
const adminAuthService = new AdminAuthService();
const listChannelConfigsUC = new ListChannelConfigs(channelConfigRepo);
const setChannelConfigUC = new SetChannelConfig(channelConfigRepo);

// controllers
// controllers (reconstruye con deps nuevas)
export const categoriesController = new CategoriesController(
  createCategoryUC, listCategoriesUC, updateCategoryUC, deleteCategoryUC
);

export const productsController = new ProductsController(
  createProductSimpleUC,
  createProductVariantedUC,
  getProductDetailUC,
  listProductsUC,
  attachModifierUC,         // o attachModUC, iguala el nombre con tu var real
  updateProductUC,
  replaceVariantsUC,
  deleteProductUC,

  // ⬇️ primero las conversiones
  convertToVariantedUC,
  convertToSimpleUC,

  // ⬇️ luego COMBOS
  createProductComboUC,
  addComboItemsUC,
  updateComboItemUC,
  removeComboItemUC,

  detachModGroupUC,
  updateModGroupPosUC,
  reorderModGroupsUC,
  attachVariantModUC,
  updateVariantModUC,
  detachVariantModUC,
  listVariantModUC
);

export const modifiersController = new ModifiersController(
  createModifierGroupUC, updateModGroupUC, replaceModOptionsUC, deleteModGroupUC,
  listModGroupsUC, getModGroupUC, listByProductUC,   listProductsByGroupUC,
    updateModifierOptionUC,      // 👈 AÑADIR ESTE
 // 👈

);

export const tablesController = new TablesController(
  createTableUC,
  listTablesUC,
  getTableUC,
  updateTableUC,
  deleteTableUC
);


//menu
export const menuController = new MenuController(
  createMenuUC, listMenusUC, listArchivedMenusUC, updateMenuUC, deleteMenuUC, restoreMenuUC,
  createSecUC, updateSecUC, deleteSecUC, deleteSecHardUC, restoreSectionUC, listSectionsUC, listArchivedSectionsUC,
  addItemUC, updateItemUC, removeItemUC, restoreItemUC, deleteItemHardUC, listItemsUC, listArchivedItemsUC,
  getMenuPublicUC
);
export const ordersController = new OrdersController(
  createOrderUC, addOrderItemUC, updateOrderItemStatusUC, addPaymentUC, getOrderDetailUC, listKDSUC,
  updateOrderItemUC, removeOrderItemUC, splitOrderByItemsUC, updateOrderMetaUC, updateOrderStatusUC, listOrdersUC,
  refundOrderUC,
  adminAuthService
);
export const channelConfigController = new ChannelConfigController(
  listChannelConfigsUC,
  setChannelConfigUC
);
export const reportsController = new ReportsController(getPaymentsReportUC);
export { usersController, rolesController, authController };
