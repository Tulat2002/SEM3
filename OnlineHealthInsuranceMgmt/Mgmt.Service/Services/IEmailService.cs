using Mgmt.Service.Models;

namespace Mgmt.Service.Services;

public interface IEmailService
{
    void SendEmail(Message message);
}