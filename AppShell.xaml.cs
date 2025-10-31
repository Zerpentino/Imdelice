// using Imdeliceapp.Pages;
using Microsoft.Maui.Controls;
using Imdeliceapp.Pages;

namespace Imdeliceapp;

public partial class AppShell : Shell
{
	public bool NavegacionHabilitada { get; set; } = true;

	public AppShell()
	{
		InitializeComponent();
		Navigating += AppShell_Navigating; // 👈
		Routing.RegisterRoute(nameof(UsersPage), typeof(UsersPage));
		Routing.RegisterRoute(nameof(UserEditorPage), typeof(UserEditorPage));


		Routing.RegisterRoute(nameof(RolesPage), typeof(RolesPage));
		Routing.RegisterRoute(nameof(RoleEditorPage), typeof(RoleEditorPage));
		Routing.RegisterRoute(nameof(CategoriesPage), typeof(CategoriesPage));
		Routing.RegisterRoute(nameof(CategoryEditorPage), typeof(CategoryEditorPage));
		Routing.RegisterRoute(nameof(ProductsPage), typeof(ProductsPage));
		Routing.RegisterRoute(nameof(ProductEditorPage), typeof(ProductEditorPage));


		Routing.RegisterRoute(nameof(AdminMenuPage), typeof(AdminMenuPage));
		Routing.RegisterRoute(nameof(MenuSectionsPage), typeof(MenuSectionsPage));
		Routing.RegisterRoute(nameof(MenuItemEditorPage), typeof(MenuItemEditorPage));
		Routing.RegisterRoute(nameof(SectionEditorPage), typeof(SectionEditorPage));
		Routing.RegisterRoute(nameof(SectionItemsPage), typeof(SectionItemsPage));
		Routing.RegisterRoute(nameof(ProductPickerPage), typeof(ProductPickerPage));
		Routing.RegisterRoute(nameof(ProductModifiersPage), typeof(ProductModifiersPage));
		Routing.RegisterRoute(nameof(ModifierGroupsPage), typeof(ModifierGroupsPage));
		Routing.RegisterRoute(nameof(GroupEditorPage), typeof(GroupEditorPage));

		// AppShell.xaml.cs
		Routing.RegisterRoute(nameof(GroupLinkedProductsPage), typeof(GroupLinkedProductsPage));
		Routing.RegisterRoute(nameof(AttachGroupToProductPage), typeof(AttachGroupToProductPage));
		Routing.RegisterRoute(nameof(VariantModifierOverridesPage), typeof(VariantModifierOverridesPage));






	}
	private void AppShell_Navigating(object sender, ShellNavigatingEventArgs e)
	{
		if (!NavegacionHabilitada)
		{
			e.Cancel(); // 🔒 Bloquea la navegación
		}
	}
}
