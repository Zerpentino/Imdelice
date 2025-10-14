namespace Imdeliceapp.Generic
{
    public class RespuestaApi<T>
    {
        public string message { get; set; }
        public List<T> data { get; set; }
        public object error { get; set; }
    }
    public class ApiErrorDTO
{
    public string message { get; set; }
    public object? data   { get; set; }
    public string error   { get; set; }
}
}
