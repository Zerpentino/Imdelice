using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Imdeliceapp.Generic;

namespace Imdeliceapp.Model
{
    public class RespuestaAutenticacionDTO
    {
        public string token { get; set; }
        public DateTime expiracion { get; set; }
        public UsuarioDTO user { get; set; } // ← Agrega esto
    }

    public class UsuarioDTO
    {
        public int piFolioUsuario { get; set; }
        public string? acIDUsuario { get; set; }
        public string? dvcNombre { get; set; }

        public int? fiFolioArea { get; set; }
        public DateTime? dtVigencia { get; set; }
        public int? iExtension { get; set; }
        public int? dbAgente { get; set; }
        public string? dcUsuarioModif { get; set; }
        public DateTime? dtFechaModif { get; set; }

        public int bCambioContrasena { get; set; }
        public int? fiFolioCliente { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Colonia { get; set; }
        public string? CP { get; set; }
        public string? Ciudad { get; set; }
        public string? Estado { get; set; }
        public string? RFC { get; set; }
        public string? tipoUsuario { get; set; }

    }
    public class LoginResponseDTO
    {
        public string                message { get; set; } = "";
        public LoginDataDTO?         data    { get; set; }
        public object?               error   { get; set; }
    }

    public class LoginDataDTO
    {
        // *** flujo normal ***
        public string?     token      { get; set; }   // presente si NO debe cambiar pwd
        public UsuarioDTO? user       { get; set; }

        // *** primer login / cambio de password ***
        public string?     tempToken  { get; set; }   // presente si backend exige pwd nueva
    }

//public class ApiEnvelopeMods<T>
// {
//     public string? error { get; set; }
//     public T? data { get; set; }
//     public string? message { get; set; }
// }

//     // Opción dentro de un grupo de modificadores
//    public class ModifierOptionDTO
// {
//     public int id { get; set; }
//     public int groupId { get; set; }
//     public string name { get; set; } = "";
//     public int priceExtraCents { get; set; }
//     public bool isDefault { get; set; }
//     public bool isActive { get; set; }
//     public int position { get; set; }
// }

//     // Grupo de modificadores
//     public class ModifierGroupDTO
// {
//     public int id { get; set; }
//     public string name { get; set; } = "";
//     public string? description { get; set; }
//     public int minSelect { get; set; }
//     public int? maxSelect { get; set; }
//     public bool isRequired { get; set; }
//     public bool isActive { get; set; }
//     public int position { get; set; }
//     public int? appliesToCategoryId { get; set; }
//     public List<ModifierOptionDTO> options { get; set; } = new();
// }

// public class ProductGroupLinkDTO
// {
//     public int id { get; set; }         // id del link
//     public int position { get; set; }   // orden del grupo en el producto
//     public ModifierGroupDTO? group { get; set; }
// }

}