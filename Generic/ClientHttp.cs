using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Imdeliceapp.Generic;
using Microsoft.Maui.Networking;   

using System.Net.Http.Headers;
using Imdeliceapp.Helpers;

using System.Net.Http.Json;

namespace Imdeliceapp.Generic
{
        
    public class ClientHttp
    {
        static bool SinInternet =>
    Connectivity.Current.NetworkAccess != NetworkAccess.Internet;

        static async Task MostrarSinConexionAsync()
        {
            await ErrorHandler.MostrarErrorUsuario(
                "Sin conexión a Internet. Revisa tu red e inténtalo de nuevo.");
        }

        public static async Task<TResponse> Put<TRequest, TResponse>(
            string baseUrl, string path, TRequest data, string token = "")
        {
            if (SinInternet)                    // ① chequeo inmediato
    {
        await MostrarSinConexionAsync();
        return default!;
    }

            using var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });
            // await Application.Current.MainPage.DisplayAlert("JSON enviado", json, "OK");


            var content = new StringContent(json, Encoding.UTF8, "application/json");
            //await Application.Current.MainPage.DisplayAlert("Token enviado", token, "OK");

            var response = await client.PutAsync(baseUrl + path, content);

            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                //await Application.Current.MainPage.DisplayAlert("ERROR RESPUESTA", responseString, "OK");
                await ErrorHandler.MostrarErrorUsuario("No se pudo actualizar la información. Intenta más tarde.");
                return default;
            }

            try
            {
                var deserialized = JsonSerializer.Deserialize<TResponse>(responseString, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });

                return deserialized;
            }
            catch (Exception ex)
            {
                //await Application.Current.MainPage.DisplayAlert("ERROR AL DESERIALIZAR", ex.Message, "OK");
                await ErrorHandler.MostrarErrorTecnico(ex, "Put - Error al deserializar la respuesta");
                return default;
            }
        }

        // Método genérico para obtener una lista de objetos desde una API (GET)

        public static async Task<List<T>> GetAll<T>(string urlbase, string rutaapi, string token = "")
        {
            if (SinInternet)                    // ① chequeo inmediato
    {
        await MostrarSinConexionAsync();
        return default!;
    }

            try
            {
                var cliente = new HttpClient();
                string fullUrl = $"{urlbase.TrimEnd('/')}/{rutaapi.TrimStart('/')}";
                //await Application.Current.MainPage.DisplayAlert("dfd", fullUrl, "OK");


                if (!string.IsNullOrWhiteSpace(token))
                    cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                //await Application.Current.MainPage.DisplayAlert("Petición", fullUrl, "OK");

                string cadena = await cliente.GetStringAsync(fullUrl);

                var respuesta = JsonSerializer.Deserialize<RespuestaApi<T>>(cadena, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });

                return respuesta?.data ?? new List<T>();
            }
            catch (Exception ex)
            {
                await ErrorHandler.MostrarErrorTecnico(ex, "GetAll - Error al obtener datos");

                //await Application.Current.MainPage.DisplayAlert("Error al cargar", $"HTTP Error: {ex.Message}", "OK");
                return new List<T>();
            }
        }
        // Método genérico para obtener un solo objeto desde una API (GET)


        public static async Task<T> Get<T>(string urlbase, string rutaapi, string token = "")
        {
             if (SinInternet)                    // ① chequeo inmediato
    {
        await MostrarSinConexionAsync();
        return default!;
    }
    

            try
            {
                // Verificar que sí haya token
                if (string.IsNullOrEmpty(token))
                {
                    // await Application.Current.MainPage.DisplayAlert("ERROR", "Token no encontrado en SecureStorage", "OK");
                    return (T)Activator.CreateInstance(typeof(T));
                }

                // DEBUG: Mostrar token y URL completa
                string fullUrl = urlbase.TrimEnd('/') + rutaapi;
                //await Application.Current.MainPage.DisplayAlert("URL DEBUG", $"Petición a:\n{fullUrl}\nToken:\n{token}", "OK");

                // Cliente HTTP con validación de certificado
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                var cliente = new HttpClient(handler)
                {
                    BaseAddress = new Uri(urlbase)
                };

                // USAR HEADER DE AUTORIZACIÓN ESTÁNDAR (Bearer token)
                cliente.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // Hacer petición
                var response = await cliente.GetAsync(rutaapi);

                if (!response.IsSuccessStatusCode)
                {
                    // await Application.Current.MainPage.DisplayAlert("ERROR HTTP", $"Response status code: {(int)response.StatusCode} {response.ReasonPhrase}", "OK");
                    return (T)Activator.CreateInstance(typeof(T));
                }

                string cadena = await response.Content.ReadAsStringAsync();

                // DEBUG opcional: ver JSON crudo
                // await Application.Current.MainPage.DisplayAlert("JSON", cadena.Substring(0, Math.Min(300, cadena.Length)), "OK");

                T lista = JsonSerializer.Deserialize<T>(cadena);

                return lista;
            }

            catch (Exception ex)
            {
                await ErrorHandler.MostrarErrorTecnico(ex, "Get - Error de red");


                //await Application.Current.MainPage.DisplayAlert("ERROR EXCEPCIÓN", ex.Message, "OK");
                return (T)Activator.CreateInstance(typeof(T));
            }
        }

        public static async Task<int> GetInt(
        string urlbase, string rutaapi, string token = "")
        {
            if (SinInternet)                    // ① chequeo inmediato
    {
        await MostrarSinConexionAsync();
        return default!;
    }
            try
            {
                var cliente = new HttpClient();  // Crear una nueva instancia de HttpClient
                cliente.BaseAddress = new Uri(urlbase);
                if (token != "") cliente.DefaultRequestHeaders.Add("token", token);
                string cadena = await cliente.GetStringAsync(rutaapi);
                return int.Parse(cadena);
            }
            catch (Exception ex)
            {
                await ErrorHandler.MostrarErrorTecnico(ex, "GetInt - Error inesperado");

                return 0;
            }

        }

        public static async Task<int> Delete(
        string urlbase, string rutaapi, string token = "")
        {
            if (SinInternet)                    // ① chequeo inmediato
                {
                    await MostrarSinConexionAsync();
                    return default!;
                }

            try
            { // un response es la respuesta que te manda el servidor después de que tú le haces una petición (como un GET, POST, DELETE, etc.).
                var cliente = new HttpClient();  // Crear una nueva instancia de HttpClient

                cliente.BaseAddress = new Uri(urlbase);//Establece la URL base del servidor (ej: http://jericalo.somee.com)
                if (token != "") cliente.DefaultRequestHeaders.Add("token", token);

                var response = await cliente.DeleteAsync(rutaapi); // Aquí hace la petición DELETE a la ruta que le pasas
                if (response.IsSuccessStatusCode)
                {
                    string cadena = await response.Content.ReadAsStringAsync();// Lee el contenido que devuelve el servidor (en este caso, un número en texto) osea el que hice en back en el itri priyecto
                    return int.Parse(cadena);// Convierte el texto a un número entero (ej: "1" -> 1)


                }
                else
                {

                    return 0;

                }




            }
            catch (Exception ex)
            {
                await ErrorHandler.MostrarErrorTecnico(ex, "Delete - Error inesperado");

                return 0;
            }



        }

        public static async Task<TResponse> Post<TRequest, TResponse>(
            string urlbase, string rutaapi, TRequest obj, string token = "")
        {
            if (SinInternet)                    // ① chequeo inmediato
                {
                    await MostrarSinConexionAsync();
                    return default!;
                }
            try
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                var cliente = new HttpClient(handler);

                cliente.BaseAddress = new Uri(urlbase);
                if (!string.IsNullOrEmpty(token))
                    cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await cliente.PostAsync(rutaapi, content);

                if (response.IsSuccessStatusCode)
                {
                    string cadena = await response.Content.ReadAsStringAsync();

                    // 🚨 Aquí imprime lo que viene desde tu API
                    //   await Application.Current.MainPage.DisplayAlert("Respuesta RAW", cadena, "OK");

                    return JsonSerializer.Deserialize<TResponse>(cadena);
                }
                else
                {




                    string errorBody = await response.Content.ReadAsStringAsync();
    
                    try
                    {
                        var errorData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(errorBody);
                        if (errorData != null && errorData.ContainsKey("errors"))
                        {
                            var errores = errorData["errors"];
                            var mensajes = errores.SelectMany(e => e.Value).ToList();
                            await ErrorHandler.MostrarErrorTecnico(new Exception(string.Join("\n", mensajes)), "Post - Error al procesar la respuesta");
                            //await Application.Current.MainPage.DisplayAlert("Errores 1", string.Join("\n", mensajes), "OK");

                        }
                        else
                        {
                            await ErrorHandler.MostrarErrorTecnico(new Exception(errorBody), "Post - Error al procesar la respuesta");
                            //await Application.Current.MainPage.DisplayAlert("Error 2", errorBody, "OK");

                        }
                    }
                    catch
                    {
                        await ErrorHandler.MostrarErrorTecnico(new Exception(errorBody), "Post - Error al procesar la respuesta");
                        //await Application.Current.MainPage.DisplayAlert("Error 3", errorBody, "OK");
                    }




                    return default;
                }
            }
            catch (Exception ex)
            {
                await ErrorHandler.MostrarErrorTecnico(ex, "Post - Error inesperado");
                //await Application.Current.MainPage.DisplayAlert("Error RAW 4", ex.Message, "OK");
                return default;
            }
        }

        public static async Task<byte[]> PostFile(string baseUrl, string path, string token, int folioAnexo)
        {
            if (SinInternet)                    // ① chequeo inmediato
                {
                    await MostrarSinConexionAsync();
                    return default!;
                }
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            client.BaseAddress = new Uri(baseUrl);

            var content = new StringContent(folioAnexo.ToString());
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await client.PostAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            return null;
        }



public static async Task<byte[]> PostFile<TRequest>(string urlbase, string rutaapi, TRequest obj, string token = "")
{
    if (SinInternet)                    // ① chequeo inmediato
                {
                    await MostrarSinConexionAsync();
                    return default!;
                }

    try
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                var cliente = new HttpClient(handler)
                {
                    BaseAddress = new Uri(urlbase)
                };

                if (!string.IsNullOrEmpty(token))
                    cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await cliente.PostAsync(rutaapi, content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    await ErrorHandler.MostrarErrorTecnico(new Exception(errorBody), "PostFile - Error al procesar la respuesta");
                    //await Application.Current.MainPage.DisplayAlert("Error", errorBody, "OK");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await ErrorHandler.MostrarErrorTecnico(ex, "PostFile - Error inesperado Raw");
                //await Application.Current.MainPage.DisplayAlert("Error RAW", ex.Message, "OK");
                return null;
            }
}




        public static async Task<List<T>> PostList<T>(
    string urlbase, string rutaapi, T obj, string token = "")
        {
            if (SinInternet)                    // ① chequeo inmediato
                {
                    await MostrarSinConexionAsync();
                    return default!;
                }

            try
            {
                var cliente = new HttpClient();  // Crear una nueva instancia de HttpClient
                cliente.BaseAddress = new Uri(urlbase);
                if (token != "") cliente.DefaultRequestHeaders.Add("token", token);
                var response = await cliente.PostAsJsonAsync<T>(rutaapi, obj);
                if (response.IsSuccessStatusCode)
                {
                    string cadena = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<T>>(cadena);
                }
                else
                {
                    await ErrorHandler.MostrarErrorUsuario("No se pudo completar la operación. Intenta más tarde.");
                    return new List<T>();
                }

            }
            catch (Exception ex)
            {
                await ErrorHandler.MostrarErrorTecnico(ex, "PostList - Error inesperado");

                return new List<T>();
            }

        }


    }

}