import { Guid } from "guid-typescript";

export class LookupHelper {
  public static createTestGuid(): string {
    return ("7E57" + Guid.raw().substring(4)).toLowerCase();
  }

  public static createTestEmail(): string {
    return `__test_${Math.random().toString(36).substring(2, 15)}@contoso.com`.toLowerCase();
  }

  public static createTestDomainEmail(): string {
    return `__test_${Math.random().toString(36).substring(2, 15)}@ftrdev1.onmicrosoft.com`.toLowerCase();
  }

  public static createTestLookupItem(lookupType: string, active = true): any {
    const id = this.createTestGuid();
    return {
      id,
      name: `test ${lookupType} ${id}`,
      status: active ? "Active" : "Deleted"
    };
  }
}