namespace Imdeliceapp.Model
{
	public class PersonaCLS
	{
		public int iidpersona { get; set; } = 0;
		public string nombrecompleto { get; set; }="";
		public string correo { get; set; } = "";
		public string fechanacimientocadena { get; set; } = "";
		//Defino
		public string nombre { get; set; } = "";
		public string appaterno { get; set; } = "";
		public string apmaterno { get; set; } = "";
		public DateTime fechanacimiento { get; set; }

		public int iidsexo { get; set; } = 0;

		public string fotocadena { get; set; } = "";

		public string nombrearchivo { get; set; } = "";

		public byte[] archivo { get; set; }

        public string telefono { get; set; } = "";

        public string nombresexo { get; set; } = "";

        public string nombreusuario { get; set; } = "";

        public string nombretipousuario { get; set; } = "";



	}
}