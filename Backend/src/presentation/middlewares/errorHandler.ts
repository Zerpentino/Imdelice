import { Request, Response, NextFunction } from "express";
import { fail } from "../utils/apiResponse";

export function errorHandler(err: any, _req: Request, res: Response, _next: NextFunction) {
  console.error(err); // log real
  const status = err.status || 500;
  const message = err.message || "Error interno";
  return fail(res, message, status, err.details);
}
