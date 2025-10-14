using Imdeliceapp.Generic;
namespace Imdeliceapp.Model
{
    public class SeguroResponse
{
    public string message { get; set; }
    public SeguroData data { get; set; }
    public object error { get; set; }
}

public class SeguroData
{
    public SeguroDTO Seguro { get; set; }
    public AseguradoraDTO AseguradoraVig { get; set; }
    public AseguradoraDTO AseguradoraRen { get; set; }
}

public class SeguroDTO
{
    public string avcPolizaVig { get; set; }
    public DateTime? dtFechaInicioVig { get; set; }
    public DateTime? dtFechaFinVig { get; set; }
    public string dmPrimaVig { get; set; }
}

public class AseguradoraDTO
{
    public string avcNombre { get; set; }
}

}