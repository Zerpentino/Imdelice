import 'dotenv/config';

import express from 'express';
import cors from 'cors';
import usersRouter from './routes/users.routes';
import { errorHandler } from './presentation/middlewares/errorHandler';
//roles
import rolesRoutes from "./routes/roles.routes";
import authRoutes from './routes/auth.routes';
//Productos
import categoriesRouter from './routes/categories.routes';
import productsRouter from './routes/products.routes';
import modifiersRouter from './routes/modifiers.routes'
import channelConfigRouter from './routes/channelConfig.routes';
import reportsRouter from './routes/reports.routes';

//Menu
import menuRouter from './routes/menu.routes';
//pedido
import ordersRouter from "./routes/orders.routes";
//mesas
import tablesRouter from './routes/tables.routes';


const app = express();
app.use(cors());
app.use(express.json());

app.get('/', (_req, res) => {
  res.send('API OK mandando respuesta');
});

app.use('/api/users', usersRouter);
app.use("/api/roles", rolesRoutes);
app.use('/api/auth', authRoutes);
app.use('/api/categories', categoriesRouter);
app.use('/api/products', productsRouter);
app.use('/api/modifiers', modifiersRouter);
app.use('/api/tables', tablesRouter);
//MENU
app.use('/api/menus', menuRouter);
//pedido
app.use("/api/orders", ordersRouter);
app.use('/api/channel-config', channelConfigRouter);
app.use('/api/reports', reportsRouter);

// Middleware global de errores al final
app.use(errorHandler);

const port = Number(process.env.PORT) || 3000;
const host = process.env.HOST || '0.0.0.0';

app.listen(port, host, () => {
  console.log(`API up on http://${host === '0.0.0.0' ? 'localhost' : host}:${port}`);
});
