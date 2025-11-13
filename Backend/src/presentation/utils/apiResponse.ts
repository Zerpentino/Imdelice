import { Response } from "express";

export function success(res: Response, data: any, message = "OK", status = 200) {
  return res.status(status).json({
    error: null,
    data,
    message,
  });
}

export function fail(res: Response, message = "Error", status = 500, details?: any) {
  return res.status(status).json({
    error: { code: status, details },
    data: null,
    message,
  });
}
