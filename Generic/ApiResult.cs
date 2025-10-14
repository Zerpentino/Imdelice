namespace Imdeliceapp.Generic
{
    public static class ApiResult
    {
        public static string ObtenerCampoJWT(string token, string campo)
        {
            var partes = token.Split('.');
            if (partes.Length < 2) return null;

            var payload = partes[1];
            int mod4 = payload.Length % 4;
            if (mod4 > 0)
                payload += new string('=', 4 - mod4); // padding base64

            var bytes = Convert.FromBase64String(payload);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var doc = System.Text.Json.JsonDocument.Parse(json);

            return doc.RootElement.TryGetProperty(campo, out var valor) ? valor.GetString() : null;
        }
    }
}
