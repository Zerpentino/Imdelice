using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Imdeliceapp.Generic;

namespace Imdeliceapp.Model
{
    public class RoleDTO
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
    }

    public class UserDTO
    {
        public int id { get; set; }
        public string? email { get; set; }
        public string? name { get; set; }
        public int roleId { get; set; }
        public RoleDTO? role { get; set; }
    }

    public class ApiEnvelope<T>
    {
        public object? error { get; set; }
        public T? data { get; set; }
        public string? message { get; set; }
    }
}
