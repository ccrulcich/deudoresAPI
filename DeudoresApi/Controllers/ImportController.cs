using Microsoft.AspNetCore.Mvc;

namespace DeudoresApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImportController : ControllerBase
    {
        [HttpPost("upload")]
        [RequestSizeLimit(long.MaxValue)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Archivo inválido");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Inputs");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, file.FileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Proccess from diks (streaming)
            int count = 0;

            using var reader = new StreamReader(filePath);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                count++;
            }

            return Ok(new
            {
                message = "Archivo procesado",
                lines = count
            });
        }
    }
}