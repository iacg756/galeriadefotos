using galeriadefotos.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace galeriadefotos.Controllers
{
    [Route("api/fotos")]
    [ApiController]
    public class FotosController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public FotosController()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");


            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // Endpoint 1. Visualización de álbumes

        [HttpGet("album/{albumId}")]
        public async Task<IActionResult> GetFotosPorAlbum(int albumId)
        {
            if (albumId < 1 || albumId > 100)
            {
                return BadRequest(new
                {
                    Error = "El ID del álbum es inválido, debe ser un número entre 1 y 100."
                });
            }

            var response = await _httpClient.GetAsync($"/photos?albumId={albumId}");

            var jsonString = await response.Content.ReadAsStringAsync();
            var fotosOriginales = JsonSerializer.Deserialize<List<Foto>>(jsonString, _jsonOptions);

            if (fotosOriginales == null || fotosOriginales.Count == 0)
            {
                return NotFound(new
                {
                    Mensaje = $"No se encontró ninguna foto para el álbum {albumId}."
                });
            }

            var fotosTransformadas = fotosOriginales.Select(f => new
            {
                id = f.Id,
                titulo = f.Title,
                albumId = f.AlbumId,
                urlCompleta = f.Url,
                miniatura = f.ThumbnailUrl,
                tamanio = "Completa"
            }).ToList();

            return Ok(new
            {
                albumId = albumId,
                totalFotos = fotosTransformadas.Count,
                fotos = fotosTransformadas
            });
            
        }

        // Endpoint 2. Buscador de fotos

        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarFotos([FromQuery] string?  palabra)
        {
            if (string.IsNullOrWhiteSpace(palabra))
            {
                return BadRequest("Ingrese una palabra para buscar.");
            }

            bool tieneLetras = palabra.Any(char.IsLetter);

            if (!tieneLetras)
            {
                return BadRequest(new
                {
                    Mensaje = "La búsqueda debe ser una palabra válida."
                });
            }

            var response = await _httpClient.GetAsync("/photos");
            var jsonString = await response.Content.ReadAsStringAsync();
            var todasLasFotos = JsonSerializer.Deserialize<List<Foto>>(jsonString, _jsonOptions);

            if (todasLasFotos == null)
            {
                return NotFound();
            }

            var palabraMinuscula = palabra.ToLower();
            var fotosFiltradas = todasLasFotos
                .Where(f => f.Title.ToLower().Contains(palabraMinuscula))
                .ToList();

            if (fotosFiltradas.Count == 0)
            {
                return NotFound(new
                {
                    Mensaje = $"Disculpe, no se encontró ninguna foto que contenga la palabra '{palabra}' en su título."
                });
            }

            var resultadoFinal = new
            {
                palabraBuscada = palabra,
                totalEncontradas = fotosFiltradas.Count,
                totalMostradas = fotosFiltradas.Take(20).Count(),
                fotos = fotosFiltradas.Take(20).Select(f => new
                {
                    id = f.Id,
                    titulo = f.Title,
                    tituloDestacado = f.Title.Replace(palabra, $"**{palabra}**", StringComparison.OrdinalIgnoreCase),
                    albumId = f.AlbumId,
                    miniatura = f.ThumbnailUrl
                })
            };

            return Ok(resultadoFinal);
        }

        // Endpoint 3. Foto del día
        [HttpGet("aleatoria")]
        public async Task<IActionResult> GetFotoDelDia()
        {
            var random = new Random();
            Foto fotoEncontrada = null;
            int intentos = 0;

            while (intentos < 3 && fotoEncontrada == null)
            {
                int randomId = random.Next(1, 5001);

                var response = await _httpClient.GetAsync($"/photos/{randomId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    fotoEncontrada = JsonSerializer.Deserialize<Foto>(jsonString, _jsonOptions);
                }

                intentos++;
            }

            if (fotoEncontrada == null)
            {
                return NotFound(new
                {
                    Error = "No se pudo encontrar la foto en este momento, intenta más tarde."
                });
            }

            var resultadoFinal = new
            {
                Mensaje = "Foto del día:",
                Foto = new
                {
                    id = fotoEncontrada.Id,
                    titulo = fotoEncontrada.Title,
                    albumId = fotoEncontrada.AlbumId,
                    urlCompleta = fotoEncontrada.Url,
                    miniatura = fotoEncontrada.ThumbnailUrl
                },

                Sugerencia = $"Tambien puedes ver el album completo: /api/fotos/album/{fotoEncontrada.AlbumId}"
            };

            return Ok(resultadoFinal);
        }

        // Endpoint 4. Resumen del álbum

        [HttpGet("album/{albumId}/resumen")]
        public async Task<IActionResult> GetResumenAlbum(int albumId)
        {
            var response = await _httpClient.GetAsync($"/photos?albumId={albumId}");
            var jsonString = await response.Content.ReadAsStringAsync();
            var fotosDelAlbum = JsonSerializer.Deserialize<List<Foto>>(jsonString, _jsonOptions);

            if (fotosDelAlbum == null || fotosDelAlbum.Count == 0)
            {
                return NotFound(new
                {
                    Mensaje = $"No se encontró el álbum {albumId} o en este momento no tiene fotos."
                });
            }

            var respuestaResumen = new
            {
                albumId = albumId,
                totalFotos = fotosDelAlbum.Count,
                muestras = fotosDelAlbum.Take(5).Select(f => f.ThumbnailUrl).ToList(),
                primeraFoto = fotosDelAlbum.FirstOrDefault()?.Url,
            };

            return Ok(respuestaResumen);
        }
    }
}