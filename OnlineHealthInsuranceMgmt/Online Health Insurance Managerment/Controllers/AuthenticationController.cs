using Mgmt.Service.Models;
using Mgmt.Service.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Online_Health_Insurance_Managerment.Models;
using Online_Health_Insurance_Managerment.Models.Authentication.SignUp;

namespace Online_Health_Insurance_Managerment.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IEmailService _emailService;

    public AuthenticationController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IEmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUser registerUser, string role)
    {
        //Check user exist
        var userExist = await _userManager.FindByEmailAsync(registerUser.Email);
        if (userExist != null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, 
                new Response{ Status = "Error", Message = "User already exists!" });
        }
        //Add the user in database
        IdentityUser user = new()
        {
            Email = registerUser.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = registerUser.Username,
        };
        if (await _roleManager.RoleExistsAsync(role))
        {
            var result = await _userManager.CreateAsync(user, registerUser.Password);
            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new Response { Status = "Error", Message = "User failed to create!" });
            }
            //Add role to the user
            await _userManager.AddToRoleAsync(user, role);
            
            //Add token to verify the email
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action(nameof(ConfirmEmail), "Authentication", new { token, email = user.Email }, Request.Scheme);
            var message = new Message(new string[] {user.Email!}, "Confirmation email link", confirmationLink!);
            _emailService.SendEmail(message);
            
            return StatusCode(StatusCodes.Status200OK,
                new Response { Status = "Success", Message = $"User created & Email send to {user.Email} Successfully" });
        }
        else
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new Response { Status = "Error", Message = "This role does not exist!" });
        }
    }
    
    [HttpGet("ConfirmEmail")]
    public async Task<IActionResult> ConfirmEmail(string token, string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return StatusCode(StatusCodes.Status200OK, 
                    new Response { Status = "Success", Message = "Email Verified successfully!" });
            }
        }
        return StatusCode(StatusCodes.Status500InternalServerError,
            new Response { Status = "Error", Message = "This user does not exist!" });
    }

    // [HttpGet("sendmail")]
    // public IActionResult TestEmail()
    // {
    //     var message = new Message(new string[]
    //         { "tulath2109053@fpt.edu.vn" }, "Test", "<h1>Hello this is emal test</h1>");
    //     _emailService.SendEmail(message);
    //     return StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Email Send successfully!" });
    // }
}