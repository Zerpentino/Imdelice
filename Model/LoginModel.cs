using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Imdeliceapp.Generic;


namespace Imdeliceapp.Model
{

     // Esta clase representa el modelo de datos del Login
    // Hereda de BaseBinding para que pueda notificar a la UI cuando cambien sus valores
    public class LoginModel:BaseBinding
    {

        private bool _iscargando;

        public bool iscargando
        {
            get
            {
                return _iscargando; // Devuelve el valor actual
            }
            set
            {
                SetValue(ref _iscargando, value);// Cambia y notifica
            }
        }


        private string _acIDUsuario;
        public string acIDUsuario
        {
            get => _acIDUsuario;
            set => SetValue(ref _acIDUsuario, value);
        }


        // Campo privado donde se guarda el nombre de usuario
     private string _dvcMail; // <- debe coincidir con el backend
    public string dvcMail
    {
        get => _dvcMail;
        set => SetValue(ref _dvcMail, value);
    }


    private string _dvcContrasenia; // <- debe coincidir con el backend
    public string dvcContrasenia
    {
        get => _dvcContrasenia;
        set => SetValue(ref _dvcContrasenia, value);
    }

    }
}   