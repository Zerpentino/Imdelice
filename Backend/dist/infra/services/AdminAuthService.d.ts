interface VerifyInput {
    adminEmail?: string;
    adminPin?: string;
    password: string;
}
export declare class AdminAuthService {
    verify(input: VerifyInput): Promise<any>;
}
export {};
//# sourceMappingURL=AdminAuthService.d.ts.map