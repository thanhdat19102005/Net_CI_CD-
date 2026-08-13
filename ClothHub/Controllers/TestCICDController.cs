using Microsoft.AspNetCore.Mvc;

namespace ClothHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestCICDController : ControllerBase
    {
        [HttpGet("pingggggg")]
        public IActionResult CheckStatus()
        {
            return Ok(new
            {
                Status = "Success",
                Message = "Tuyệt vời! Jenkins CI/CD Pipeline đang hoạt động hoàn hảo!",
                Version = "V1 - Cập nhật lúc " + DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"),
                Environment = "Production - MonsterASP"
            });
        }
    }
}
