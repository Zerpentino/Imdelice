using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Imdeliceapp.Generic;



namespace Imdeliceapp.Model
{
    public class MenuModel : BaseBinding
    {
 private List<MenuCLS> _listamenu;

        public List<MenuCLS> listamenu
        {
            get => _listamenu;
            set => SetValue(ref _listamenu, value);
        }



    }
}
