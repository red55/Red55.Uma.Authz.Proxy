using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Options;

namespace Red55.Uma.Authz.Proxy.Models;

[OptionsValidator]
public partial class ValidateAppConfig : IValidateOptions<AppConfig> { }

public class AppConfig
{
    static readonly Uri EmtpyUri = new ("about:blank", UriKind.Absolute);
    [Required]
    public Uri UmaServerBaseUrl { get; set; } = EmtpyUri;
    public bool InsecureSkipTlsVerify { get; set; } = false;
}

