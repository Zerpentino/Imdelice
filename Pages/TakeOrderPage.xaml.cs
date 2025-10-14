using System.Collections.ObjectModel;
using Imdeliceapp.Model;

namespace Imdeliceapp.Pages;

public partial class TakeOrderPage : ContentPage
{
    public ObservableCollection<CategoryGroup> Groups { get; } = new();

    // carrito mínimo para la demo de UI
    private readonly List<MenuRow> _cart = new();

    public TakeOrderPage()
    {
        InitializeComponent();
        BindingContext = this;

        // ============ CREPAS/MARQUESITAS — DULCES ============
        Groups.Add(new CategoryGroup {
            Id="dulces", Name="Crepas y Marquesitas — Dulces",
            Items = new ObservableCollection<MenuRow> {
                new(){ Id="cd1", Name="1 ingrediente", Price=55 },
                new(){ Id="cd2", Name="2 ingredientes", Price=59 },
                new(){ Id="cd3", Name="3 ingredientes", Price=64 },
                new(){ Id="cdqb1", Name="Queso Bola + 1 ingrediente", Price=61 },
                new(){ Id="cdqb2", Name="Queso Bola + 2 ingredientes", Price=66 },
                new(){ Id="delice", Name="Delice (Nutella, Phil., Fresa, Hershey's, Azúcar Glass + Kínder Delice)", Price=79 },
                new(){ Id="chocofruit", Name="Chocofruit (Masa choco, Nutella, Fresa, Kiwi, Crema Batida)", Price=79 },
                new(){ Id="frutella", Name="Frutella (Nutella, Philadelphia, Fresa y Plátano)", Price=79 },
            }
        });

        // ============ CREPAS/WAFFLES/MARQUESITAS — SALADAS ============
        Groups.Add(new CategoryGroup {
            Id="saladas", Name="Crepas / Waffles / Marquesitas — Saladas",
            Items = new ObservableCollection<MenuRow> {
                new(){ Id="cs2", Name="2 ingredientes", Price=75 },
                new(){ Id="cs3", Name="3 ingredientes", Price=77 },
                new(){ Id="s_amer", Name="Americana (Mozzarella, Jamón, Philadelphia)", Price=77 },
                new(){ Id="s_creperoni", Name="Creperoni (Mozzarella, Salsa de Pizza, Pepperoni)", Price=77 },
                new(){ Id="s_chori", Name="Choriqueso (Manchego, Chorizo)", Price=75 },
                new(){ Id="s_haw", Name="Hawaiana (Manchego, Jamón, Piña)", Price=77 },
                new(){ Id="s_champi", Name="Champi (Manchego, Champiñones, Jamón)", Price=77 },
                new(){ Id="s_veg", Name="Vegetariana (Salsa pizza, Champiñones, Queso, Morrón)", Price=79 },
                new(){ Id="s_sup", Name="Suprema (Tocino, Chorizo, Jamón, Champiñón, Mozzarella)", Price=85 },
            }
        });

        // ============ BEBIDAS ============
        Groups.Add(new CategoryGroup {
            Id="bebidas", Name="Bebidas (calientes / frías / sodas / tisanas / frappes)",
            Items = new ObservableCollection<MenuRow> {
                new(){ Id="caf_ame", Name="Café Americano", Price=39 },
                new(){ Id="cap_ch", Name="Cappuccino CH", Price=50 },
                new(){ Id="cap_gde", Name="Cappuccino GDE", Price=55 },
                new(){ Id="cap_es_ch", Name="Cappuccino con esencia CH", Price=55 },
                new(){ Id="cap_es_gde", Name="Cappuccino con esencia GDE", Price=59 },
                new(){ Id="late_frio", Name="Late frío", Price=55 },
                new(){ Id="late_es", Name="Late frío con esencia", Price=59 },
                new(){ Id="te_dig", Name="Té digestivo", Price=45 },
                new(){ Id="agua_fresca", Name="Agua fresca (Horchata/Jamaica)", Price=25 },
                // Frappes / Sodas: muestra 1 línea genérica; luego podrás abrir detalle de sabores
                new(){ Id="frap_gde", Name="Frappe GDE (sabores)", Price=65 },
                new(){ Id="soda_ita", Name="Soda italiana (sabores)", Price=65 }, // si tu menú usa otro precio, aquí lo ajustas
            }
        });

        // ============ ALITAS / BONELESS / SNACKS ============
        Groups.Add(new CategoryGroup {
            Id="snacks", Name="Alitas / Boneless / Snacks",
            Items = new ObservableCollection<MenuRow> {
                new(){ Id="al5",  Name="Alitas 5 pzas",  Price=69 },
                new(){ Id="al10", Name="Alitas 10 pzas", Price=129 },
                new(){ Id="al15", Name="Alitas 15 pzas", Price=178 },
                new(){ Id="bon300", Name="Boneless 300 gr", Price=119 },
                new(){ Id="papas_f", Name="Papas a la Francesa", Price=55 },
                new(){ Id="papas_g", Name="Papas Gajo", Price=64 },
                new(){ Id="popcorn", Name="Popcorn Chicken", Price=59 },
                new(){ Id="dedos", Name="Dedos de Queso (7 pzas)", Price=89 },
                new(){ Id="bot_mix", Name="Botana Mixta", Price=99 },
            }
        });

        // ============ BAGUETTES / SÁNDWICH ============
        Groups.Add(new CategoryGroup {
            Id="bag", Name="Baguettes / Sándwich",
            Items = new ObservableCollection<MenuRow> {
                new(){ Id="bag_jam", Name="Baguette Jamón", Price=75 },
                new(){ Id="bag_pan", Name="Baguette Panela", Price=75 },
                new(){ Id="bag_pol", Name="Baguette Pollo", Price=79 },
                new(){ Id="bag_emp", Name="Baguette Pollo Empanizado", Price=79 },
                new(){ Id="bag_haw", Name="Baguette Hawaiano", Price=79 },
                new(){ Id="bag_pep", Name="Baguette Pepperoni", Price=79 },
                new(){ Id="bag_ame", Name="Baguette Americano", Price=79 },
                new(){ Id="bag_chori", Name="Baguette Choriqueso", Price=79 },
                new(){ Id="bag_sup", Name="Baguette Supremo", Price=85 },
                new(){ Id="bag_atun", Name="Baguette Atún", Price=87 },

                new(){ Id="sand_jam", Name="Sándwich Jamón", Price=55 },
                new(){ Id="sand_pol", Name="Sándwich Pollo", Price=65 },
                new(){ Id="sand_emp", Name="Sándwich Pollo Empanizado", Price=65 },
                new(){ Id="sand_atun", Name="Sándwich Atún", Price=69 },
            }
        });

        // ============ ENSALADAS ============
        Groups.Add(new CategoryGroup {
            Id="ens", Name="Ensaladas",
            Items = new ObservableCollection<MenuRow> {
                new(){ Id="ens_m", Name="Ensalada Mediana", Price=129 },
                new(){ Id="ens_g", Name="Ensalada Grande",  Price=169 },
                new(){ Id="ens_d", Name="Ensalada Delice",  Price=199 },
            }
        });

        // ============ MARISCOS ============
        Groups.Add(new CategoryGroup {
            Id="mar", Name="Mariscos",
            Items = new ObservableCollection<MenuRow> {
                new(){ Id="tac_cam_panko", Name="Taco de camarón (al panko/capeado)", Price=39 },
                new(){ Id="tac_pes_panko", Name="Taco de pescado (al panko/capeado)", Price=36 },
                new(){ Id="cev_tost", Name="Ceviche – Tostada", Price=50 },
                new(){ Id="agua_l",  Name="Aguachile – Litro", Price=160 },
                new(){ Id="agua_m",  Name="Aguachile – Medio litro", Price=85 },
                new(){ Id="coc_cam_g", Name="Cóctel Camarón (Grande)", Price=155 },
                new(){ Id="coc_cam_c", Name="Cóctel Camarón (Chico)",  Price=99 },
                new(){ Id="filete_pesc", Name="Filete de pescado empanizado", Price=169 },
                new(){ Id="mojarra", Name="Mojarra frita", Price=129 },
            }
        });

        // ============ COMBOS ============
        Groups.Add(new CategoryGroup {
            Id="combos", Name="Combos",
            Items = new ObservableCollection<MenuRow> {
                new(){ Id="cmb_alit", Name="Combo Alitas (10 pzas + botana mixta)", Price=215 },
                new(){ Id="cmb_fies", Name="Combo Fiesta (alitas/boneless/papas etc.)", Price=399 },
                new(){ Id="cmb_del",  Name="Combo Delice (15 alitas + gajo + boneless)", Price=339 },
                new(){ Id="cmb_ind",  Name="Combo Individual (W/C/M dulce 1–2 ing + frappe GDE)", Price=115 },
                new(){ Id="cmb_par",  Name="Combo Pareja (2 W/C/M dulces + 2 smoothies AGUA)", Price=220 },
                new(){ Id="cmb_sal",  Name="Combo Salada (W/C/M salada 2 ing + frappe GDE)", Price=129 },
            }
        });

        // Abre por defecto la primera categoría
        if (Groups.Count > 0) Groups[0].IsExpanded = true;
    }

    private void ToggleExpand_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is CategoryGroup grp)
        {
            grp.IsExpanded = !grp.IsExpanded;
            var idx = Groups.IndexOf(grp);
            Groups.RemoveAt(idx);
            Groups.Insert(idx, grp); // refresco
        }
    }

    private void AddToCart_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is MenuRow row)
        {
            _cart.Add(row);
            FabCart.Text = $"🧾 Carrito ({_cart.Count})";
        }
    }

    private void OpenCart_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Carrito", $"Tienes {_cart.Count} productos.", "OK");
    }

   private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
{
    var q = (e.NewTextValue ?? "").Trim().ToLowerInvariant();
    foreach (var grp in Groups)
    {
        bool hasMatch = grp.Items.Any(i => i.Name.ToLowerInvariant().Contains(q));
        if (string.IsNullOrEmpty(q)) continue;
        grp.IsExpanded = hasMatch;   // notificará y el Expander se actualizará
    }
}

}
