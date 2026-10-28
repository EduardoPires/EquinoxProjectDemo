using Equinox.UI.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Equinox.UI.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Route("sobre")]
        public IActionResult About()
        {
            return View();
        }

        [Route("error/{id:length(3,3)}")]
        public IActionResult Errors(int id)
        {
            var modelErro = new ErrorViewModel();

            if (id == 500)
            {
                modelErro.Message = "Não conseguimos concluir a sua solicitação. Tente de novo em instantes.";
                modelErro.Title = "Algo deu errado";
                modelErro.ErrorCode = id;
            }
            else if (id == 404)
            {
                modelErro.Message = "The page you are looking for does not exist! <br /> If you have any questions please contact our support";
                modelErro.Title = "Oops! Page not found.";
                modelErro.ErrorCode = id;
            }
            else if (id == 403)
            {
                modelErro.Message = "You are not allowed to do this.";
                modelErro.Title = "Access Denied";
                modelErro.ErrorCode = id;
            }
            else
            {
                return StatusCode(500);
            }

            return View("Error", modelErro);
        }
    }
}
