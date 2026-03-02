using galeriadefotos.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PlayStream_Core.Controllers
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

        // 1. Visualización de Álbumes

        [HttpGet("album/{albumId}")]
        public async Task<IActionResult> GetFotosPorAlbum(int albumId)
        {
            if (albumId < 1 || albumId > 100)
            {
                return BadRequest(new
                {
                    error = "El ID del álbum es inválido. Debe ser un número entre 1 y 100."
                });
            }

            var response = await _httpClient.GetAsync($"/photos?albumId={albumId}");

            var jsonString = await response.Content.ReadAsStringAsync();
            var fotosOriginales = JsonSerializer.Deserialize<List<Foto>>(jsonString, _jsonOptions);

            if (fotosOriginales == null || fotosOriginales.Count == 0)
            {
                return NotFound(new
                {
                    mensaje = $"¡Ups! No encontramos ninguna foto para el álbum {albumId}."
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

            var respuestaExitosa = new
            {
                albumId = albumId,
                totalFotos = fotosTransformadas.Count,
                fotos = fotosTransformadas
            };

            return Ok(respuestaExitosa);
        }

        // 2. Buscador de Fotos
        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarFotos([FromQuery] string palabra)
        {
            if (string.IsNullOrWhiteSpace(palabra) || palabra.Length < 3)
            {
                return BadRequest(new
                {
                    error = "Por favor, ingresa una palabra de al menos 3 letras para buscar."
                });
            }

            var response = await _httpClient.GetAsync("/photos");
            var jsonString = await response.Content.ReadAsStringAsync();
            var todasLasFotos = JsonSerializer.Deserialize<List<Foto>>(jsonString, _jsonOptions);

            if (todasLasFotos == null) return NotFound();

            var palabraMinuscula = palabra.ToLower();

            var fotosEncontradas = todasLasFotos
                .Where(f => f.Title.ToLower().Contains(palabraMinuscula))
                .Select(f => new
                {
                    id = f.Id,
                    titulo = f.Title,
                    albumId = f.AlbumId,
                    urlCompleta = f.Url,
                    miniatura = f.ThumbnailUrl
                }).ToList();

            if (fotosEncontradas.Count == 0)
            {
                return NotFound(new
                {
                    mensaje = $"¡Ups! No encontramos ninguna foto que contenga la palabra '{palabra}' en su título."
                });
            }
            return Ok(new
            {
                palabraBuscada = palabra,
                totalEncontradas = fotosEncontradas.Count,
                resultados = fotosEncontradas
            });
        }
        // 3. Foto del Día
        [HttpGet("dia")]
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
                    error = "No se pudo encontrar la foto del día en este momento. Inténtalo más tarde."
                });
            }

            var resultadoFinal = new
            {
                mensaje = "¡Foto del día!",
                foto = new
                {
                    id = fotoEncontrada.Id,
                    titulo = fotoEncontrada.Title,
                    albumId = fotoEncontrada.AlbumId,
                    urlCompleta = fotoEncontrada.Url,
                    miniatura = fotoEncontrada.ThumbnailUrl
                },

                sugerencia = $"/api/fotos/album/{fotoEncontrada.AlbumId}"
            };

            return Ok(resultadoFinal);
        }

        // 4. Resumen del Álbum

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
                    mensaje = $"No se encontró el álbum {albumId} o no tiene fotos actualmente."
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