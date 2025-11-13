// src/infra/security/pincode.ts
import crypto from 'crypto';
import bcrypt from 'bcrypt';

const PEPPER = process.env.PINCODE_PEPPER || 'dev-pepper-change-me';

export async function hashPin(pin: string): Promise<string> {
  return bcrypt.hash(pin, 10);
}

export function digestPin(pin: string): string {
  return crypto.createHmac('sha256', PEPPER).update(pin, 'utf8').digest('hex');
}
