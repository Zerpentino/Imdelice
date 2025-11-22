import type { RequestHandler } from 'express';

const replacer = (_key: string, value: unknown) => {
  if (Buffer.isBuffer(value)) {
    return `[Buffer:${value.length}]`;
  }
  return value;
};

const formatBody = (body: any) => {
  if (body === undefined || body === null) return '<empty>';
  if (typeof body !== 'object') return String(body);
  if (Buffer.isBuffer(body)) return `[Buffer:${body.length}]`;
  if (!Object.keys(body).length) return '<empty>';
  try {
    return JSON.stringify(body, replacer);
  } catch {
    return '<unserializable>';
  }
};

export const requestLogger: RequestHandler = (req, _res, next) => {
  const bodyPreview = formatBody(req.body);
  console.log(`[HTTP] ${req.method} ${req.originalUrl} body=${bodyPreview}`);
  next();
};
