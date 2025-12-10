using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Options;

namespace Red55.Uma.Authz.Proxy.Models;

[OptionsValidator]
public partial class ValidateAppConfig : IValidateOptions<AppConfig> { }

public class AppConfig
{
    [Required]
    public string UmaServerBaseUrl { get; set; } = string.Empty;
}

