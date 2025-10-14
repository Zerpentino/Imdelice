using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;


// Clase base para ViewModels o modelos con enlace a UI
	// Implementa INotifyPropertyChanged para notificar cambios a la interfaz
namespace Imdeliceapp.Generic
{
	// Clase base para ViewModels o modelos con enlace a UI
	// Implementa INotifyPropertyChanged para notificar cambios a la interfaz
	public class BaseBinding:INotifyPropertyChanged
	{
		// Evento que se lanza cuando una propiedad cambia
		public event PropertyChangedEventHandler PropertyChanged;
// Método que lanza el evento PropertyChanged
		// [CallerMemberName] toma el nombre de la propiedad que lo llamó automáticamente
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			// Si hay suscriptores, lanza el evento con el nombre de la propiedad que cambió
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

// Método genérico para asignar valor a una propiedad y notificar a la UI si cambió
		protected void SetValue<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			// Si el nuevo valor es igual al actual, no hace nada
			if (EqualityComparer<T>.Default.Equals(field, value)) return;
			// Si el valor cambió, se asigna
			field = value;
			// Notifica el cambio a la interfaz
			OnPropertyChanged(propertyName);

		}


	}
}
