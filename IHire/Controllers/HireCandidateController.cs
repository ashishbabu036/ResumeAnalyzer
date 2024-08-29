using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IHire.Core;
using IHire.API.Helper;

namespace IHire.API.Controllers
{
    [Route("api/hire")]
    [ApiController]
    public class HireCandidateController : ControllerBase
    {
        private IHireAIService _hireAIService;
        public HireCandidateController(IHireAIService hireAIService)
        {
            _hireAIService = hireAIService;
        }

        [HttpPost("resume")]
        public async Task<IActionResult> GetTechnicalSkillSet([FromForm] IFormFile file, [FromHeader] string question = "Fetch Technical Skill Sets from the uploaded file.")
        {
            string path = Directory.GetCurrentDirectory() + "\\FileDownloaded";

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var filePath = System.IO.Path.Combine(
               Directory.GetCurrentDirectory(), "FileDownloaded",
               file.FileName);

            await FileHandler.CopyStream(file.OpenReadStream(), filePath);

            string result = await _hireAIService.ExtractCandidateInfo(file.FileName, question);
            return Ok(result);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            var response = await _hireAIService.UploadFile(file.OpenReadStream());
            return Ok(response.DocumentId);
        }

        [HttpPost("extract")]
        public async Task<IActionResult> ExtractContent([FromBody] string documentId)
        {
            string textContent = await _hireAIService.FetchContentFromResume(documentId);
            return Ok(textContent);
        }

        [HttpPost("skills")]
        public async Task<IActionResult> FetchSkills([FromBody] Chat[] messages)
        {
            string textContent = await _hireAIService.FetchSkills(messages);
            return Ok(textContent);
        }
    }
}
