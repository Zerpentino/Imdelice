namespace Imdeliceapp.Services;

public static class Perms
{
    static HashSet<string> _perms = new(StringComparer.OrdinalIgnoreCase);

    public static void Set(IEnumerable<string>? codes)
        => _perms = codes?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();

    public static bool Has(string code) => _perms.Contains(code);
    public static bool Any(params string[] codes) => codes.Any(Has);
    public static bool All(params string[] codes) => codes.All(Has);

    // helpers por módulo (evita strings repetidos en la app)
    public static bool RolesRead    => Has("roles.read");
    public static bool RolesCreate  => Has("roles.create");
    public static bool RolesUpdate  => Has("roles.update");
    public static bool RolesDelete  => Has("roles.delete");

    public static bool UsersRead    => Has("users.read");
    public static bool UsersCreate  => Has("users.create");
    public static bool UsersUpdate  => Has("users.update");
    public static bool UsersDelete => Has("users.delete");

    public static bool CategoriesRead    => Has("categories.read");
    public static bool CategoriesCreate  => Has("categories.create");
    public static bool CategoriesUpdate  => Has("categories.update");
    public static bool CategoriesDelete => Has("categories.delete");

public static bool ModifiersRead    => Has("modifiers.read");
    public static bool ModifiersCreate  => Has("modifiers.create");
    public static bool ModifiersUpdate  => Has("modifiers.update");
    public static bool ModifiersDelete => Has("modifiers.delete");


    public static bool MenusRead    => Has("menu.read");
public static bool MenusCreate  => Has("menu.create");
public static bool MenusUpdate  => Has("menu.update");
public static bool MenusDelete  => Has("menu.delete");

}
