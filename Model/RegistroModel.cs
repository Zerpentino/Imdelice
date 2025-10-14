using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Imdeliceapp.Generic;

namespace Imdeliceapp.Model
{
    // Esta clase representa el modelo de datos del Registro
    // Hereda de BaseBinding para que pueda notificar a la UI cuando cambien sus valores
    public class RegistroModel : BaseBinding
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

       private int _piFolioUsuario; // <- debe coincidir con el backend
    public int piFolioUsuario
    {
        get => _piFolioUsuario;
        set => SetValue(ref _piFolioUsuario, value);
    }

    private string _dvcContrasenia; // <- debe coincidir con el backend
    public string dvcContrasenia
    {
        get => _dvcContrasenia;
        set => SetValue(ref _dvcContrasenia, value);
    }

    

         private string _confirmarPassword;
    public string ConfirmarPassword
    {
        get => _confirmarPassword;
        set => SetValue(ref _confirmarPassword, value);
    }
    }

}