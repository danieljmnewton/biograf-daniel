using Microsoft.AspNetCore.Mvc;

namespace Biograf.Api.Common;

public static class ResultatExtensions
{
    /// <summary>Översätter ett tjänsteresultat till ett HTTP-svar med rätt statuskod.</summary>
    public static ActionResult<T> TillSvar<T>(this ControllerBase controller, Resultat<T> resultat)
    {
        if (resultat.Lyckades)
            return resultat.Varde is Inget ? controller.NoContent() : controller.Ok(resultat.Varde);

        var status = resultat.Fel switch
        {
            FelTyp.HittadesInte => StatusCodes.Status404NotFound,
            FelTyp.Konflikt => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return controller.Problem(title: resultat.Meddelande, statusCode: status);
    }
}
